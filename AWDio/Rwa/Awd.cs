using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace AwdIO.Rwa
{
    public class Awd : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        const string awdFileExt = ".awd";

        const int baseOffset = 0x800; // Container length must be multiple of this.
        static readonly int soundBankSize = 0x2C;
        static readonly int waveDictSize = 0x30;

        const int Sec1Tag = 0x809;
        const int pWaveListHead = 0x38;
        static readonly int namePos = 0x5C;

        public static Awd Empty { get; } = new Awd();

        [JsonProperty(Order = 0)]
        public string Name { get; set; } = string.Empty;

        [JsonProperty(Order = 1)]
        public Platform Platform { get; set; }

        [JsonProperty(Order = 2)]
        public int UuidFlags { get; set; }

        [JsonProperty(Order = 3)]
        public byte flags;

        [JsonProperty(Order = 4)]
        public byte flagsAux;

        [JsonProperty(Order = 5)]
        public int dumpAddr;

        [JsonProperty(Order = 6)]
        public int waveRamHandle;

        [JsonProperty(Order = 8)]
        public int Unk0 { get; set; }

        [JsonProperty(Order = 9)]
        public int Unk1 { get; set; }

        [JsonIgnore]
        public int pData { get; private set; }

        [JsonProperty(Order = 10)]
        public LinkedList<RwaWave> WaveList { get; set; } = new LinkedList<RwaWave>();

        public int pUuid { get; set; }

        static int SerializeFile(Awd awd, string outPath)
        {
            FileStream fs = File.Create(outPath);
            var bw = new BinaryWriter(fs);

            bw.Write(Sec1Tag);
            bw.Write(awd.Unk0);
            bw.Write(0);
            bw.Write(soundBankSize);
            bw.Write(awd.Unk1);
            bw.Write(0);
            bw.Write(awd.Platform.Uuid.ToByteArray());
            bw.Write(awd.pData);
            bw.Write(awd.pUuid);
            bw.Write(namePos);
            bw.Write(awd.UuidFlags);

            fs.Seek((sizeof(int) * 3) + sizeof(short), SeekOrigin.Current); // waveListHead

            bw.Write(awd.flags);
            bw.Write(awd.flagsAux);
            bw.Write(awd.dumpAddr);
            bw.Write(awd.waveRamHandle);

            fs.Position = namePos;
            bw.Write(Encoding.ASCII.GetBytes(awd.Name + '\0'));

            while (fs.Position % 4 != 0)
            {
                fs.Position++;
            }

            var pLinks = new List<int>();

            foreach (var wave in awd.WaveList)
            {
                pLinks.Add((int)fs.Position + RwaWave.size);
                wave.Serialize(bw, awd.pData);
            }

            awd.pData = (int)fs.Seek(Utility.RoundUp((int)fs.Length, baseOffset), SeekOrigin.Begin);
            var pDatas = new List<int>();

            foreach (var wave in awd.WaveList)
            {
                pDatas.Add((int)fs.Position - awd.pData);
                bw.Write(wave.Data);
                fs.Seek(Utility.RoundUp((int)fs.Length, 0x10), SeekOrigin.Begin);
            }

            for (int i = 0; i < pLinks.Count; i++)
            {
                fs.Position = pLinks[i] - 0x10; // data
                bw.Write(pDatas[i]);
            }
            
            fs.Position = 0x08;
            bw.Write(awd.pData);

            fs.Position = 0x28;
            bw.Write(awd.pData);

            fs.Position = soundBankSize;
            bw.Write(awd.pUuid);

            // Write link list nodes.
            fs.Position = pLinks[0];
            bw.Write(pWaveListHead);

            for (int i = 1; i < pLinks.Count; i++)
            {
                bw.Write(pLinks[i]);
                bw.Write(pLinks[i - 1] - RwaWave.size);
                fs.Position = pLinks[i];
                bw.Write(pLinks[i - 1]);
            }

            bw.Write(pWaveListHead);
            bw.Write(pLinks[^1] - RwaWave.size);

            // Write waveListHead.
            fs.Position = pWaveListHead;
            bw.Write(pLinks[^1]);
            bw.Write(pLinks[0]);

            fs.SetLength(Utility.RoundUp((int)fs.Length, baseOffset));

            fs.Position = 0x14;
            bw.Write((int)fs.Length);

            bw.Close();
            return 0;
        }

        static async Task<int> SerializeFolder(Awd awd, string outPath)
        {
            if (Directory.Exists(outPath))
            {
                bool? response = null;
                while (response == null)
                {
                    Console.WriteLine("Specified output directory already exists. Would you like to overwrite it? (Y/N)");
                    response = Utility.ConsoleReadBool(Console.ReadLine());
                }
                if (response != true)
                {
                    return 1;
                }
                else
                {
                    Directory.Delete(outPath, true);
                }
            }

            Console.WriteLine("Exporting audio files...");
            Directory.CreateDirectory(outPath);

            await Task.WhenAll
            (
                awd.WaveList.Select
                (
                    w =>
                    {
                        var dataPath = Path.Combine(outPath, Path.ChangeExtension(w.uniqueID.Name, ".bin"));
                        File.WriteAllBytes(dataPath, w.Data);
                        return Task.CompletedTask;
                    }
                )
            );

            var jsSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            string json = JsonConvert.SerializeObject(awd, Formatting.Indented, jsSettings);

            File.WriteAllText(Path.Combine(outPath, awd.Name + ".json"), json);

            return 0;
        }

        /// <summary>
        /// If <paramref name="outPath"/> is a directory, serialize to individual WAV files + header JSON file.
        /// Otherwise, serialize to an AWD file.
        /// </summary>
        /// <param name="awd"></param>
        /// <param name="outPath"></param>
        /// <returns></returns>
        public static async Task<int> SerializeAsync(Awd awd, string outPath)
        {
            string outPathExt = Path.GetExtension(outPath);

            int ret = 0;
            // Directory
            if (string.IsNullOrEmpty(outPathExt))
            {
                ret = await SerializeFolder(awd, outPath);
            }
            else if (string.Equals(outPathExt, awdFileExt, StringComparison.OrdinalIgnoreCase))
            {
                ret = SerializeFile(awd, outPath);
            }
            else
            {
                ret = -1;
            }

            return ret;
        }

        static Awd DeserializeFile(string inPath)
        {
            var fs = File.OpenRead(inPath);
            var br = new BinaryReader(fs);

            var awd = Empty;

            bool validLen = fs.Length > soundBankSize + waveDictSize;
            if (!validLen)
            {
                Console.WriteLine(Error.invalidAwdMessage);
                return awd;
            }

            // Magic num check.
            if (br.ReadInt32() != Sec1Tag)
            {
                Console.WriteLine(Error.invalidAwdMessage);
                return awd;
            }

            awd.Unk0 = br.ReadInt32();
            awd.pData = br.ReadInt32();
            var sbd3 = br.ReadInt32();
            awd.Unk1 = br.ReadInt32();

            fs.Seek(sizeof(int), SeekOrigin.Current);

            var platUuidDat = br.ReadBytes(16);
            var platUuid = new Guid(platUuidDat);

            Platform plat = null;

            foreach (var p in Platform.Platforms)
            {
                if (p.Uuid == platUuid)
                {
                    plat = p;
                    break;
                }
            }

            if (plat == null)
            {
                Console.WriteLine(Error.invalidUuidMessage);
                return awd;
            }

            awd.Platform = plat;

            fs.Position = sbd3;

            awd.pUuid = br.ReadInt32();
            var pName = br.ReadInt32();
            awd.UuidFlags = br.ReadInt32();

            fs.Seek((sizeof(int) * 3) + sizeof(short), SeekOrigin.Current);

            awd.flags = br.ReadByte();
            awd.flagsAux = br.ReadByte();
            awd.dumpAddr = br.ReadInt32();
            awd.waveRamHandle = br.ReadInt32();

            fs.Position = pName;
            awd.Name = br.ReadAscii();

            awd.WaveList = new LinkedList<RwaWave>();

            fs.Position = pWaveListHead + sizeof(int);

            var next = br.ReadInt32();
            while (next != pWaveListHead)
            {
                fs.Position = next + sizeof(int);

                next = br.ReadInt32();
                int data = br.ReadInt32();

                fs.Position = data;
                awd.WaveList.AddLast(RwaWave.Deserialize(br, awd.pData));
            }

            br.Close();

            return awd;
        }

        static async Task<Awd> DeserializeFolder(string inPath)
        {
            var files = Directory.GetFiles(inPath);
            int iJson = Array.FindIndex(files, f => Path.GetExtension(f).Equals(".json", StringComparison.OrdinalIgnoreCase));

            var jsonStr = File.ReadAllText(files[iJson]);
            var awd = JsonConvert.DeserializeObject<Awd>(jsonStr);

            try
            {
                await Task.WhenAll
                (
                    awd.WaveList.Select
                    (
                        w =>
                        {
                            var dataPath = Path.Combine(inPath, Path.ChangeExtension(w.uniqueID.Name, ".bin"));
                            if (!files.Any(p => Path.GetFileNameWithoutExtension(p).Equals(w.uniqueID.Name)))
                            {
                                throw new FileNotFoundException($"Data file for WaveList entry \"{w.uniqueID.Name}\" could not be found", dataPath);
                            }
                            w.Data = File.ReadAllBytes(dataPath);
                            return Task.CompletedTask;
                        }
                    )
                );
            }
            catch (FileNotFoundException)
            {
                throw;
            }

            return awd;
        }

        public static async Task<Awd> DeserializeAsync(string inPath, bool silent)
        {
            var awd = Empty;
            if (Directory.Exists(inPath))
            {
                awd = await DeserializeFolder(inPath);
            }
            else if (File.Exists(inPath))
            {
                awd = DeserializeFile(inPath);
            }
            else
            {
                return Empty;
            }

            if (!silent)
            {
                PrintProperties(awd);
            }
            
            return awd;
        }

        public static void PrintProperties(Awd awd)
        {
            var colWidths = new int[] { 14, 7, 7, 7, 7 };

            Console.WriteLine("Name:    {0}\nSystem:  {1}\n", awd.Name, awd.Platform.Name);
            Console.WriteLine("              Rate          Bit    Duration");
            Console.Write("Name".PadRight(colWidths[0] ));
            Console.Write("(Hz)".PadRight(colWidths[1]));
            Console.Write("Chan.".PadRight(colWidths[2]));
            Console.Write("Depth".PadRight(colWidths[3]));
            Console.WriteLine("(mm:ss.fff)".PadRight(colWidths[4]));

            Console.WriteLine(new string('=', 48)); // Repeat '=' 48 times.

            foreach (var item in awd.WaveList)
            {
                Console.Write(item.uniqueID.ToString().PadRight(colWidths[0]));
                Console.Write(item.format.sampleRate.ToString().PadRight(colWidths[1]));
                Console.Write(item.format.noChannels.ToString().PadRight(colWidths[2]));
                Console.Write(item.format.bitDepth.ToString().PadRight(colWidths[3]));
                Console.Write(item.format.Duration.ToString("mm\\:ss\\.fff"));
                Console.WriteLine();
            }

            Console.WriteLine("\nTotal: {0}", awd.WaveList.Count);
        }

        public void RaisePropertyChanged([CallerMemberName] string propName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }
}
