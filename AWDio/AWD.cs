using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;

using Newtonsoft.Json;

namespace AWDio
{
    public class AWD : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        const string awdFileExt = ".awd";

        [JsonProperty(Order = 0)]
        public string Name { get; set; } = string.Empty;

        const int Sec1Tag = 0x809;
        const int pWaveListHead = 0x38;

        const int baseOffset = 0x800; // Container length must be multiple of this.

        static readonly int soundBankSize = 0x2C;
        static readonly int waveDictSize = 0x30;

        static readonly int namePos = 0x5C;

        public static AWD Empty { get; } = new AWD();

        [JsonProperty(Order = 8)]
        public byte flags;

        [JsonProperty(Order = 2)]
        public byte flagsAux;

        [JsonProperty(Order = 3)]
        public int dumpAddr;

        [JsonProperty(Order = 4)]
        public int waveRamHandle;

        [JsonProperty(Order = 5)]
        public int waveRamSize;

        [JsonProperty(Order = 9)]
        public int Unk0 { get; set; }

        [JsonProperty(Order = 10)]
        public int Unk1 { get; set; }

        [JsonIgnore]
        public int pData { get; private set; }

        [JsonIgnore]
        public uint DataSize { get; set; }

        [JsonProperty(Order = 999)]
        public LinkedList<Wave> WaveList { get; set; } = new LinkedList<Wave>();

        [JsonProperty(Order = 11)]
        public int UuidFlags { get; set; }

        [JsonIgnore]
        public Platform Platform { get; set; }

        [JsonProperty(Order = 6)]
        public int pUuid { get; set; }

        [JsonProperty(Order = 1)]
        public Guid PlatUuid
        {
            get { return Platform.Uuid; }
        }

        static readonly string outMagic = "RwaW";

        /// <summary>
        /// If <paramref name="outPath"/> is a directory, serialize to individual WAV files + header JSON file.
        /// Otherwise, serialize to an AWD file.
        /// </summary>
        /// <param name="awd"></param>
        /// <param name="outPath"></param>
        /// <returns></returns>
        public static int Serialize(AWD awd, string outPath)
        {
            string outPathExt = Path.GetExtension(outPath);
            FileStream fs = null;
            var bw = BinaryWriter.Null;

            // Directory
            if (string.IsNullOrEmpty(outPathExt))
            {
                var txthPath = Path.Combine(outPath, ".txth");
                Directory.CreateDirectory(Path.GetDirectoryName(txthPath));

                File.WriteAllLines(txthPath, new string[] { $"codec = {awd.Platform.Codec}", Vgmstream.txthLines });

                var outTempFiles = new List<string>();

                Console.WriteLine("Exporting audio files...");

                foreach (var wave in awd.WaveList)
                {
                    string outTempFilePath = Path.Combine(outPath, wave.uniqueID.Name + '0'); // Append char to name to circumvent vgmstream L/R pair detection.
                    fs = File.Create(outTempFilePath);
                    bw = new BinaryWriter(fs);
                    bw.Write(Encoding.ASCII.GetBytes(outMagic));
                    bw.Write((int)wave.format.noChannels);
                    bw.Write(wave.format.sampleRate);
                    fs.Seek(0x10, SeekOrigin.Begin);
                    bw.Write(wave.Data);
                    bw.Close();

                    if (!File.Exists(outTempFilePath))
                    {
                        throw new Exception();
                    }

                    outTempFiles.Add(outTempFilePath);
                }

                foreach (var item in outTempFiles)
                {
                    Vgmstream.ConvertToWave(item);
                    File.Delete(item);
                }

                File.Delete(txthPath);

                var jsSettings = new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                string json = JsonConvert.SerializeObject(awd, Formatting.Indented, jsSettings);

                File.WriteAllText(Path.Combine(outPath, awd.Name + ".json"), json);

                Console.WriteLine("AWD extracted successfully.");
            }
            else if (string.Equals(outPathExt, awdFileExt, StringComparison.OrdinalIgnoreCase))
            {
                bw = new BinaryWriter(fs = File.Create(outPath));

                bw.Write(Sec1Tag);
                bw.Write((ulong)awd.Unk0);
                bw.Write(soundBankSize);
                bw.Write((ulong)awd.Unk1);
                bw.Write(awd.PlatUuid.ToByteArray());
                bw.Write(awd.pData);

                bw.Write(awd.pUuid);
                bw.Write(namePos);
                bw.Write(awd.UuidFlags);

                fs.Seek((sizeof(int) * 3) + sizeof(short), SeekOrigin.Current); // waveListHead

                bw.Write(awd.flags);
                bw.Write(awd.flagsAux);
                bw.Write(awd.dumpAddr);
                bw.Write(awd.waveRamHandle);
                bw.Write(awd.waveRamSize);

                fs.Position = namePos;
                bw.Write(Encoding.ASCII.GetBytes(awd.Name + '\0'));

                while (fs.Position % 4 != 0)
                {
                    fs.Position++;
                }

                var pWaves = new List<int>();

                foreach (var wave in awd.WaveList)
                {
                    pWaves.Add((int)fs.Position + Wave.size);
                    wave.Serialize(bw);
                }

                fs.Seek(Utility.RoundUp((int)fs.Length, baseOffset), SeekOrigin.Begin);

                foreach (var wave in awd.WaveList)
                {
                    bw.Write(wave.Data);
                    fs.Seek(Utility.RoundUp((int)fs.Length, 0x10), SeekOrigin.Begin);
                }

                // Write link list nodes.
                fs.Position = pWaves[0];
                bw.Write(pWaveListHead);

                for (int i = 1; i < pWaves.Count; i++)
                {
                    bw.Write(pWaves[i]);
                    bw.Write(pWaves[i - 1] - Wave.size);
                    fs.Position = pWaves[i];
                    bw.Write(pWaves[i - 1]);
                }

                bw.Write(pWaveListHead);
                bw.Write(pWaves[^1] - Wave.size);

                // Write waveListHead.
                fs.Position = pWaveListHead;
                bw.Write(pWaves[^1]);
                bw.Write(pWaves[0]);

                fs.SetLength(Utility.RoundUp((int)fs.Length, baseOffset));

                Console.WriteLine("AWD saved successfully.");
            }

            bw.Close();

            return 0;
        }

        public static AWD Deserialize(string inPath)
        {
            if (Directory.Exists(inPath))
            {
                var ret = new AWD();

                var files = Directory.GetFiles(inPath);

                var jsonFiles = files.Where(p => Path.GetExtension(p) == ".json").ToArray();

                if (jsonFiles.Length != 1)
                {
                    Console.WriteLine("Could not find JSON configuration file.");
                    return AWD.Empty;
                }

                string jsonPath = jsonFiles[0];
                var jsonIn = File.ReadAllText(jsonPath);
                var objThing = JsonConvert.DeserializeObject<AWD>(jsonIn);

                return objThing;
            }
            else if (string.Equals(Path.GetExtension(inPath), awdFileExt, StringComparison.OrdinalIgnoreCase))
            {
                var fs = new FileStream(inPath, FileMode.Open);
                var br = new BinaryReader(fs);

                if (fs.Length <= soundBankSize)
                {
                    Console.WriteLine(Error.invalidAwdMessage);
                    return Empty;
                }

                var sec1Tag = br.ReadInt32();

                if (sec1Tag != Sec1Tag)
                {
                    Console.WriteLine(Error.invalidAwdMessage);
                    return Empty;
                }

                var ret = new AWD();
                ret.Unk0 = br.ReadInt32();
                ret.pData = br.ReadInt32();
                var sbd3 = br.ReadInt32();
                ret.Unk1 = br.ReadInt32();
                ret.DataSize = br.ReadUInt32();

                var sysUuidDat = br.ReadBytes(16);
                var sysUuid = new Guid(sysUuidDat);

                bool validSysUuid = Platform.IsValid(sysUuid);
                bool validLen = fs.Length > soundBankSize + waveDictSize;

                // If either check fails.
                if (validSysUuid ^ validLen)
                {
                    if (!validSysUuid)
                    {
                        Console.WriteLine(Error.invalidUuidMessage);
                    }

                    if (!validLen)
                    {
                        Console.WriteLine(Error.invalidAwdMessage);
                    }

                    return Empty;
                }

                ret.Platform = Platform.FromUuid(sysUuid);

                fs.Position = sbd3;

                ret.pUuid = br.ReadInt32();
                var pName = br.ReadInt32();
                ret.UuidFlags = br.ReadInt32();

                fs.Seek((sizeof(int) * 3) + sizeof(short), SeekOrigin.Current);

                ret.flags = br.ReadByte();
                ret.flagsAux = br.ReadByte();
                ret.dumpAddr = br.ReadInt32();
                ret.waveRamHandle = br.ReadInt32();
                ret.waveRamSize = br.ReadInt32();

                fs.Position = pName;
                ret.Name = br.ReadAscii();

                ret.WaveList = new LinkedList<Wave>();

                fs.Position = pWaveListHead + sizeof(int);

                var next = br.ReadInt32();
                while (next != pWaveListHead)
                {
                    fs.Position = next + sizeof(int);

                    next = br.ReadInt32();
                    int data = br.ReadInt32();

                    fs.Position = data;
                    ret.WaveList.AddLast(Wave.Deserialize(br, ret.pData));
                }

                int[] colWidths = new int[] { 16, 8, 12, 12, 16 };

                Console.WriteLine("Name:    {0}\nSystem:  {1}\n", ret.Name, ret.Platform.Name);
                Console.Write("Name".PadRight(colWidths[0]));
                Console.Write("Rate".PadRight(colWidths[1]));
                Console.Write("Channels".PadRight(colWidths[2]));
                Console.Write("Bit Depth".PadRight(colWidths[3]));
                Console.Write("Length".PadRight(colWidths[4]));

                Console.WriteLine();
                Console.WriteLine(new string('=', 64)); // Repeat '=' 64 times.

                foreach (var item in ret.WaveList)
                {
                    Console.Write(item.uniqueID.ToString().PadRight(colWidths[0]));
                    Console.Write(item.format.sampleRate.ToString().PadRight(colWidths[1]));
                    Console.Write(item.format.noChannels.ToString().PadRight(colWidths[2]));
                    Console.Write(item.format.bitDepth.ToString().PadRight(colWidths[3]));
                    Console.Write(item.Data.Length.ToString("X8"));
                    Console.WriteLine();
                }

                Console.WriteLine("\nTotal: {0}\n", ret.WaveList.Count);

                fs.Close();

                return ret;
            }
            else
            {
                return AWD.Empty;
            }
        }

        public void RaisePropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
