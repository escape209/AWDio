using System;
using System.IO;

namespace AWDio
{
    public class Wave
    {
        public static readonly int size = 0x5C;

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

            public static Format GetWaveFormat(int[] data)
            {
                Format format = new()
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

        public static Wave ReadWave(BinaryReader br)
        {
            // Read wave header.
            var waveDat = new int[23];
            Buffer.BlockCopy(br.ReadBytes(Wave.size), 0, waveDat, 0, Wave.size);

            var retPos = br.BaseStream.Position;

            var wave = new Wave
            {
                pWaveDef = waveDat[3],
                format = Wave.Format.GetWaveFormat(waveDat[4..11]),
                targetFormat = Wave.Format.GetWaveFormat(waveDat[11..18]),
                uncompLength = (uint)waveDat[18],
                pData = waveDat[19],
                pState = waveDat[20],
                flags = (uint)waveDat[21],
                pObj = waveDat[22]
            };

            // Read wave name.
            br.BaseStream.Position = waveDat[1];
            string name = br.ReadAscii();
            wave.uniqueID = new UniqueID(waveDat[0], name, string.Empty, (uint)waveDat[3]);

            // Read wave copy name.
            if (waveDat[2] != 0)
            {
                br.BaseStream.Position = waveDat[2];
                string copyName = br.ReadAscii();
                wave.uniqueID.copyName = copyName;
            }

            br.BaseStream.Position = retPos;

            return wave;
        }

        public override string ToString()
        {
            return uniqueID.ToString();
        }
    }
}
