﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json;




namespace AWDio
{
    public class AWD : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        const string awdFileExt = ".awd";

        static readonly string testExePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "vgmstream", 
            "test"
        );

        [JsonProperty(Order = 0)]
        public string Name { get; set; } = string.Empty;

        const int Sec1Tag = 0x809;
        const int pWaveListHead = 0x38;

        const int baseOffset = 0x800; // Container length must be multiple of this.

        static readonly int soundBankSize = 0x2C;
        static readonly int waveDictSize = 0x30;

        static readonly int namePos = 0x5C;

        static string txthLines = "channels = @0x04\nsample_rate = @0x08\nstart_offset = 0x10\ninterleave = 0x1000\nnum_samples = data_size";

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

        

        [JsonIgnore]
        public string SystemName => AWDio.SystemUuid.GetSysUuidName(SystemUUID);

        [JsonProperty(Order = 6)]
        public int pUuid { get; set; }

        Guid system;

        [JsonProperty(Order = 1)]
        public Guid SystemUUID
        {
            get { return system; }
            set {
                if (system != value)
                {
                    system = value;
                    RaisePropertyChanged(nameof(SystemName));
                }
            }
        }

        public string GetEncoderString()
        {
            if (SystemUUID == SystemUuid.PlayStation)
            {
                return "PSX";
            }
            else if (SystemUUID == SystemUuid.Xbox)
            {
                return "PCM16LE";
            }
            return string.Empty;
        }

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

        static readonly string outMagic = "D00D";

        [JsonProperty(Order = 11)]
        public int UuidFlags { get; set; }

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
                var sw = File.CreateText(txthPath);
                sw.WriteLine("codec = " + awd.GetEncoderString());
                sw.WriteLine(txthLines); 
                sw.Close();

                var outTempFiles = new List<string>();

                Console.WriteLine("Exporting audio files...");

                foreach (var wave in awd.WaveList)
                {
                    string outTempFilePath = Path.Combine(outPath, wave.uniqueID.Name + '0'); // Add underscore to name to circumvent vgmstream L/R pair detection.
                    fs = File.Create(outTempFilePath);
                    fs.SetLength(0x10);
                    bw = new BinaryWriter(fs);
                    bw.Write(Encoding.ASCII.GetBytes(outMagic));
                    bw.Write((int)wave.format.noChannels);
                    bw.Write(wave.format.sampleRate);
                    fs.Seek(0, SeekOrigin.End);
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
                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = $"/C {testExePath} -o \"{Path.ChangeExtension(item[0..^1], ".wav")}\" \"{item}\""; // Todo: make this not shit
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
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
                bw.Write(AWD.soundBankSize);
                bw.Write((ulong)awd.Unk1); 
                bw.Write(awd.SystemUUID.ToByteArray());
                bw.Write(awd.pData);

                bw.Write(awd.pUuid);
                bw.Write(namePos);
                bw.Write(awd.UuidFlags);

                fs.Seek(sizeof(int) * 3, SeekOrigin.Current); // waveListHead

                bw.Write((uint)(awd.flags << 16 | awd.flagsAux << 24));
                bw.Write(awd.dumpAddr);
                bw.Write(awd.waveRamHandle);
                bw.Write(awd.waveRamSize);
                
                fs.Position = namePos;
                bw.Write(Encoding.ASCII.GetBytes(awd.Name + '\0'));

                while (++fs.Position % 4 != 0);

                var pWaves = new List<int>();

                foreach (var wave in awd.WaveList)
                {
                    pWaves.Add((int)fs.Position + Wave.size);
                    wave.Serialize(bw);
                }

                fs.SetLength(Utility.RoundUp((int)fs.Length, baseOffset));
                fs.Seek(0, SeekOrigin.End);

                foreach (var wave in awd.WaveList)
                {
                    wave.SerializeAudioData(bw);
                    fs.SetLength(Utility.RoundUp((int)fs.Length, 0x10));
                    fs.Seek(0, SeekOrigin.End);
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

                fs.Position = pWaveListHead;
                bw.Write(pWaves[^1]);
                bw.Write(pWaves[0]);

                fs.SetLength(Utility.RoundUp((int)fs.Length, baseOffset));

                Console.WriteLine("AWD saved successfully.");
            }

            bw.Close();

            return 0;
        }

        public void RaisePropertyChanged([CallerMemberName]string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }

        public static AWD Deserialize(string inPath)
        {
            var fs = new FileStream(inPath, FileMode.Open);
            var br = new BinaryReader(fs);

            if (fs.Length <= soundBankSize)
            {
                Console.WriteLine(Error.invalidAwdMessage);
                return AWD.Empty;
            }

            var sec1Tag = br.ReadInt32();
            fs.Seek(0, SeekOrigin.Begin);

            if (sec1Tag != Sec1Tag)
            {
                Console.WriteLine(Error.invalidAwdMessage);
                return AWD.Empty;
            }

            int[] soundBankDat = new int[soundBankSize / sizeof(int)];
            Buffer.BlockCopy(br.ReadBytes(soundBankSize), 0, soundBankDat, 0, soundBankSize);
            
            var ret = new AWD();
            ret.Unk0 = soundBankDat[1];
            ret.pData = soundBankDat[2];
            ret.Unk1 = soundBankDat[4];
            ret.DataSize = (uint)soundBankDat[5];

            var sysUuidDat = new byte[16];
            Buffer.BlockCopy(soundBankDat, 6 * sizeof(int), sysUuidDat, 0, 16);
            var sysUuid = new Guid(sysUuidDat);

            bool validSysUuid = AWDio.SystemUuid.IsValid(sysUuid);
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

                return AWD.Empty;
            }

            ret.SystemUUID = sysUuid;

            fs.Position = soundBankDat[3];

            int[] waveDictDat = new int[waveDictSize / sizeof(int)];
            Buffer.BlockCopy(br.ReadBytes(waveDictSize), 0, waveDictDat, 0, waveDictSize);

            ret.pUuid = waveDictDat[0];

            fs.Position = waveDictDat[1];
            ret.Name = br.ReadAscii();

            ret.UuidFlags = waveDictDat[2];

            ret.flags = (byte)((0xFF0000 & waveDictDat[6]) >> 16);
            ret.flagsAux = (byte)((0xFF000000 & waveDictDat[6]) >> 24);
            ret.dumpAddr = waveDictDat[7];
            ret.waveRamHandle = waveDictDat[8];
            ret.waveRamSize = waveDictDat[9];

            fs.Position = waveDictDat[4]; // Go to waveListHead.

            var links = new List<int>();
            var datas = new List<int>();
            int link = pWaveListHead;
            fs.Position = link;
            while (!links.Contains(link))
            {
                links.Add(link);
                fs.Seek(sizeof(int), SeekOrigin.Current);

                link = br.ReadInt32();
                int data = br.ReadInt32();

                datas.Add(data);

                fs.Position = link;
            }

            links.Remove(pWaveListHead);
            datas.Remove(0);

            ret.WaveList = new LinkedList<Wave>();

            foreach (var item in datas)
            {
                fs.Position = item;
                ret.WaveList.AddLast(Wave.Deserialize(br, soundBankDat[10]));
            }

            Console.WriteLine("Name:    {0}", ret.Name);
            Console.WriteLine("System:  {0}", ret.SystemName);
            Console.WriteLine();

            int col0Width = 16;
            int col1Width = 8;
            int col2Width = 12;
            int col3Width = 12;
            int col4Width = 16;

            Console.Write("Name".PadRight(col0Width));
            Console.Write("Rate".PadRight(col1Width));
            Console.Write("Channels".PadRight(col2Width));
            Console.Write("Bit Depth".PadRight(col3Width));
            Console.Write("Length".PadRight(col4Width));

            Console.WriteLine();
            Console.WriteLine(new string('=', 64)); // Repeat '=' 64 times.

            foreach (var item in ret.WaveList)
            {
                Console.Write(item.uniqueID.ToString().PadRight(col0Width));
                Console.Write(item.format.sampleRate.ToString().PadRight(col1Width));
                Console.Write(item.format.noChannels.ToString().PadRight(col2Width));
                Console.Write(item.format.bitDepth.ToString().PadRight(col3Width));
                Console.Write(item.Data.Length.ToString("X8"));
                Console.WriteLine();
            }

            Console.WriteLine("\nTotal: {0}\n", ret.WaveList.Count);

            return ret;
        }
    }
}
