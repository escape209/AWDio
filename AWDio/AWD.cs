using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace AWDio
{
    public class AWD : INotifyPropertyChanged
    {
        const string awdFileExt = ".awd";

        static readonly string testExePath = Path.Combine("vgmstream", "test.exe");

        const int Sec1Tag = 0x809;
        const int pWaveListHead = 0x38;

        const int baseOffset = 0x800; // Container length must be multiple of this.

        static readonly int soundBankSize = 0x2C;
        static readonly int waveDictSize = 0x30;

        static readonly int namePos = 0x5C;

        public static AWD Empty { get; } = new AWD();

        public byte flags;
        public byte flagsAux;
        public int dumpAddr;
        public int waveRamHandle;
        public int waveRamSize;

        public event PropertyChangedEventHandler PropertyChanged;

        public string SystemName => AWDio.SystemUuid.GetSysUuidName(SystemUuid);

        public UniqueID UID { get; set; } = new UniqueID(0, 0, 0);

        Guid systemUuid;
        public Guid SystemUuid
        {
            get { return systemUuid; }
            set {
                if (systemUuid != value)
                {
                    systemUuid = value;
                    RaisePropertyChanged(nameof(SystemName));
                }
            }
        }

        public int Unk0 { get; set; }
        public int Unk1 { get; set; }

        public int pData { get; private set; }

        public uint DataSize { get; set; }

        public LinkedList<Wave> WaveList { get; set; } = new LinkedList<Wave>();

        public string Name { get; set; } = string.Empty;

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

            }
            else if (string.Equals(outPathExt, awdFileExt, StringComparison.OrdinalIgnoreCase))
            {
                bw = new BinaryWriter(fs = File.Create(outPath));

                bw.Write(Sec1Tag);
                bw.Write((ulong)awd.Unk0);
                bw.Write(AWD.soundBankSize);
                bw.Write((ulong)awd.Unk1); 
                bw.Write(awd.SystemUuid.ToByteArray());
                bw.Write(awd.pData);

                bw.Write(awd.UID.pUuid);
                bw.Write(namePos);
                bw.Write(awd.UID.flags);

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
                fs.Seek(0, SeekOrigin.End);

            }

            bw.Close();

            Console.WriteLine("\nAWD saved successfully to {0}.\n", outPath);

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

            ret.SystemUuid = sysUuid;

            fs.Position = soundBankDat[3];

            int[] waveDictDat = new int[waveDictSize / sizeof(int)];
            Buffer.BlockCopy(br.ReadBytes(waveDictSize), 0, waveDictDat, 0, waveDictSize);

            ret.UID.pUuid = waveDictDat[0];

            fs.Position = waveDictDat[1];
            ret.Name = br.ReadAscii();

            ret.UID.flags = (uint)waveDictDat[2];

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
