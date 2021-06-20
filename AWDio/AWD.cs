using System;
using System.Collections.Generic;
using System.IO;

namespace AWDio
{
    public class AWD
    {
        static readonly string testExePath = Path.Combine("vgmstream", "test.exe");

        static readonly int Sec1Tag = 0x809;

        static readonly int soundBankSize = 0x2C;
        static readonly int waveDictSize = 0x30;

        public static AWD Empty { get; } = new AWD();

        public AWD() { }
        public AWD(string name)
        {
            this.Name = name;
        }

        public string Name
        {
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
                    sysName = AWDio.SystemUuid.GetPlatUuidName(SystemUuid);
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

        public static AWD Deserialize(string inPath)
        {
            var fs = new FileStream(inPath, FileMode.Open);
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

            if (sysUuid == AWDio.SystemUuid.PlayStation || sysUuid == AWDio.SystemUuid.Xbox)
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

            Console.WriteLine("Name:    {0}", ret.Name);
            Console.WriteLine("System:  {0}", ret.SystemName);

            fs.Position = waveDictDat[4]; // Go to waveListHead.

            ret.WaveList = new LinkedList<Wave>();
            var wavePos = new List<long>();
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

                var wave = Wave.ReadWave(br);

                ret.WaveList.AddLast(wave);

                fs.Position = linkDat[1];
            }

            Console.WriteLine("\nWaves (Total: {0}):\n", ret.WaveList.Count);
            Console.WriteLine("Name             Rate   Channels  Bit Depth   Length");
            Console.WriteLine(new string('=', 64)); // Repeat '=' 64 times.

            foreach (var item in ret.WaveList)
            {
                Console.WriteLine(
                    $"{item.uniqueID,-16} {item.format.sampleRate,5}      {item.format.noChannels}        {item.format.bitDepth,2}      {item.format.length:X8}"
                );
            }

            return ret;
        }
    }
}
