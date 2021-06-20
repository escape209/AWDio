using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AWDio
{
    public class AWD {
        static readonly string testExePath = Path.Combine("vgmstream", "test.exe");

        static readonly int Sec1Tag = 0x809;
        static readonly int soundBankSize = 0x2C;
        static readonly int waveDictSize = 0x30;
        static readonly int waveSize = 0x5C;

        public static AWD Empty { get; } = new AWD();

        public AWD() { }
        public AWD(string name)
        {
            this.Name = name;
        }

        public string Name {
            get { return UID.uniqueName; }
            set { UID.uniqueName = value; }
        }

        string sysName = string.Empty;
        public string SystemName
        {
            get
            {
                if (string.IsNullOrEmpty(sysName))
                {
                    sysName = SystemUuids.GetPlatUuidName(SystemUuid);
                }
                return sysName;
            }
        }

        public UniqueID UID { get; set; } = new UniqueID(0, "Null", string.Empty, 0);

        public Guid SystemUuid { get; set; }

        public uint Unk0 { get; set; }
        public uint Unk1 { get; set; }

        public uint DataSize { get; set; }

        public LinkedList<Wave> WaveList { get; set; }

        public static class SystemUuids
        {
            public static readonly Guid PlayStation = new(-0x55153654, -0x3C8, 0x4917, new byte[] { 0xAE, 0x81, 0x64, 0xEA, 0xDB, 0xC7, 0x93, 0x53 });
            public static readonly Guid Xbox = new(0x453A2D04, -0x1BA1, 0x4BC8, new byte[] { 0x81, 0xF0, 0xDF, 0x75, 0x8B, 0x01, 0xF2, 0x73 });

            public static string GetPlatUuidName(Guid uuid)
            {
                if (uuid == PlayStation)
                {
                    return nameof(PlayStation);
                }
                else if (uuid == Xbox)
                {
                    return nameof(Xbox);
                }
                else
                {
                    return "Unknown";
                }
            }
        }

        public class UniqueID
        {
            public int pUuid;
            public string uniqueName;
            public string copyName;
            public uint flags;

            public UniqueID(int pUuid, string uniqueName, string copyName, uint flags)
            {
                this.pUuid = pUuid;
                this.uniqueName = uniqueName;
                this.copyName = copyName;
                this.flags = flags;
            }

            public override string ToString()
            {
                string ret = uniqueName;
                if (!string.IsNullOrEmpty(copyName))
                {
                    ret += string.Format(" ({0})", copyName);
                }
                return ret;
            }
        }

        public class Wave
        {
            public class Format
            {
                public uint sampleRate;
                public int pDataType;
                public uint length;
                public byte bitDepth;
                public byte noChannels;
                public int pMiscData;
                public uint miscDataSize;
                public byte flags;
                public byte reserved;
            }

            public UniqueID uniqueID;
            public int pWaveDef;
            public Format format;
            public Format targetFormat;
            public uint uncompLength;
            public int pData;
            public int pState;
            public uint flags;
            public int pObj;

            public override string ToString()
            {
                return uniqueID.ToString();
            }
        }

        static Wave.Format GetWaveFormat(int[] data)
        {
            Wave.Format format = new Wave.Format()
            {
                sampleRate = (uint)data[0],
                pDataType = data[1],
                length = (uint)data[2],
                bitDepth = (byte)(0xFF & data[3]),
                noChannels = (byte)((0xFF00 & data[3]) >> 8),
                pMiscData = data[4],
                miscDataSize = (uint)data[5],
                flags = (byte)(0xFF & data[6]),
                reserved = (byte)((0xFF00 & data[6]) >> 8),
            };
            return format;
        }

        public static AWD Deserialize(string inFile) {
            var fs = new FileStream(inFile, FileMode.Open);
            var br = new BinaryReader(fs);

            var ret = AWD.Empty;

            if (fs.Length <= soundBankSize)
            {
                Console.WriteLine(Exception.invalidAwdMessage);
                return ret;
            }

            int[] soundBankDat = new int[soundBankSize / sizeof(int)];
            Buffer.BlockCopy(br.ReadBytes(soundBankSize), 0, soundBankDat, 0, soundBankSize);

            if (soundBankDat[0] != Sec1Tag)
            {
                Console.WriteLine(Exception.invalidAwdMessage);
                return ret;
            }

            ret.Unk0 = (uint)soundBankDat[1];
            ret.Unk1 = (uint)soundBankDat[4];
            ret.DataSize = (uint)soundBankDat[5];

            var sysUuidDat = new byte[16];
            Buffer.BlockCopy(soundBankDat, 6 * sizeof(int), sysUuidDat, 0, 16);
            var sysUuid = new Guid(sysUuidDat);

            if (sysUuid == SystemUuids.PlayStation || sysUuid == SystemUuids.Xbox)
            {
                ret.SystemUuid = sysUuid;
                
            }
            else
            {
                Console.WriteLine(Exception.invalidUuidMessage);
                return AWD.Empty;
            }

            fs.Position = soundBankDat[3];

            if (fs.Length <= soundBankSize + waveDictSize)
            {
                Console.WriteLine(Exception.invalidAwdMessage);
                return AWD.Empty;
            }

            int[] waveDictDat = new int[waveDictSize / sizeof(int)];
            Buffer.BlockCopy(br.ReadBytes(waveDictSize), 0, waveDictDat, 0, waveDictSize);

            ret.UID.pUuid = waveDictDat[0];

            fs.Position = waveDictDat[1];
            ret.UID.uniqueName = br.ReadAscii();

            if (waveDictDat[2] != 0)
            {
                fs.Position = waveDictDat[2];
                ret.UID.copyName = br.ReadAscii();
            }

            ret.UID.flags = (uint)waveDictDat[3];

            Console.WriteLine("Name:    " + ret.Name);
            Console.WriteLine("System:  " + ret.SystemName);

            fs.Position = waveDictDat[4]; // Go to waveListHead.

            ret.WaveList = new LinkedList<Wave>();
            List<long> wavePos = new List<long>();
            int[] linkDat = new int[3];
            while (true)
            {
                // Get link.
                Buffer.BlockCopy(br.ReadBytes(sizeof(int) * linkDat.Length), 0, linkDat, 0, sizeof(int) * linkDat.Length);
                
                if (wavePos.Contains(linkDat[2]) || linkDat[2] == 0)
                {
                    break;
                }

                wavePos.Add(linkDat[2]);
                fs.Position = linkDat[2];

                // Read wave header.
                var waveDat = new int[23];
                Buffer.BlockCopy(br.ReadBytes(waveSize), 0, waveDat, 0, waveSize);

                var wave = new Wave
                {
                    pWaveDef = waveDat[3],
                    format = GetWaveFormat(waveDat[4..11]),
                    targetFormat = GetWaveFormat(waveDat[11..18]),
                    uncompLength = (uint)waveDat[18],
                    pData = waveDat[19],
                    pState = waveDat[20],
                    flags = (uint)waveDat[21],
                    pObj = waveDat[22]
                };

                // Read wave name.
                fs.Position = waveDat[1];
                string name = br.ReadAscii();
                wave.uniqueID = new UniqueID(waveDat[0], name, string.Empty, (uint)waveDat[3]);

                // Read wave copy name.
                if (waveDat[2] != 0)
                {
                    fs.Position = waveDat[2];
                    string copyName = br.ReadAscii();
                    wave.uniqueID.copyName = copyName;
                }
                
                ret.WaveList.AddLast(wave);

                fs.Position = linkDat[1];
            }

            int colWidth = 20;
            Console.WriteLine("\nWaves (Total: {0}):", ret.WaveList.Count);
            Console.WriteLine();
            Console.Write("Name".PadRight(colWidth - 4));
            Console.Write("Frequency (Hz)".PadRight(colWidth - 2));
            Console.Write("Channels".PadRight(colWidth - 8));
            Console.Write("Bit Depth".PadRight(colWidth));
            Console.WriteLine();
            Console.WriteLine("".PadRight(colWidth * 4, '='));

            foreach (var item in ret.WaveList)
            {
                Console.Write(item.uniqueID.ToString().PadRight(colWidth));
                Console.Write(item.format.sampleRate.ToString().PadLeft(5).PadRight(colWidth - 3));
                Console.Write(item.format.noChannels.ToString().PadRight(colWidth - 7));
                Console.Write(item.format.bitDepth.ToString().PadRight(colWidth));
                Console.WriteLine();
            }

            return ret;
        }
    }
}
