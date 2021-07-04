using System;
using System.IO;

namespace AWDio
{
    public class Wave
    {
        public const int size = 0x5C;

        public class Format
        {
            public uint sampleRate;
            public int pDataType;
            // public int length; // length moved to Wave.
            public byte bitDepth;
            public byte noChannels;
            public int pMiscData;
            public uint miscDataSize;
            public byte flags;
            public byte reserved;

            public int Serialize(BinaryWriter bw, int length)
            {
                bw.Write(sampleRate);
                bw.Write(pDataType); 
                bw.Write(length);
                bw.Write((uint)(bitDepth | noChannels << 8));
                bw.Write(pMiscData);
                bw.Write(miscDataSize);
                bw.Write((uint)(flags | reserved << 8));

                return 0;
            }

            public static Format GetWaveFormat(int[] data)
            {
                Format format = new()
                {
                    sampleRate = (uint)data[0],
                    pDataType = data[1],
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

        public byte[] Data { get; set; } = Array.Empty<byte>();

        public int pState;
        public uint flags;
        public int pObj;

        public int SerializeAudioData(BinaryWriter bw)
        {
            bw.Write(Data);
            return 0;
        }


        public int Serialize(BinaryWriter bw)
        {
            // Skip uniqueID.
            int ppUniqueID = (int)bw.BaseStream.Position;
            bw.BaseStream.Seek(sizeof(int) * 3, SeekOrigin.Current);

            bw.Write(pWaveDef);

            // Write format and targetFormat.
            //int pFormat = (int)bw.BaseStream.Position;
            format.Serialize(bw, Data.Length);
            //int pTargetFormat = (int)bw.BaseStream.Position;
            targetFormat.Serialize(bw, Data.Length);

            bw.Write(uncompLength); // Skip data.
            int ppData = (int)bw.BaseStream.Position;
            bw.Write(int.MaxValue);
            bw.Write(pState);
            bw.Write(flags);
            bw.Write(pObj);

            // AWD will write the link.
            bw.BaseStream.Seek(sizeof(int) * 3, SeekOrigin.Current);

            // UniqueID data.
            int pName = (int)bw.BaseStream.Position;
            bw.Write(System.Text.Encoding.ASCII.GetBytes(uniqueID.Name + '\0'));

            while (++bw.BaseStream.Position % 4 != 0);

            int pUuid = (int)bw.BaseStream.Position;
            bw.Write(uniqueID.Uuid.ToByteArray());

            bw.BaseStream.Position = ppUniqueID;
            bw.Write(pUuid);
            bw.Write(pName);

            bw.BaseStream.Position = ppData;
            bw.Write(pData);
            bw.BaseStream.Seek(0, SeekOrigin.End);
            //bw.Write(Data);
            

            bw.BaseStream.Position = pUuid + 0x10;




            

            return 0; 
        }

        public static Wave Deserialize(BinaryReader br, long dataOffset)
        {
            // Read raw wave header.
            var waveDat = new int[Wave.size / sizeof(int)];
            var waveDatBytes = br.ReadBytes(size);
            Buffer.BlockCopy(waveDatBytes, 0, waveDat, 0, size);

            var wave = new Wave
            {
                pWaveDef = waveDat[3],
                format = Format.GetWaveFormat(waveDat[4..11]),
                targetFormat = Format.GetWaveFormat(waveDat[11..18]),
                uncompLength = (uint)waveDat[18],
                pData = waveDat[19],
                pState = waveDat[20],
                flags = (uint)waveDat[21],
                pObj = waveDat[22]
            };

            // Read audio data.
            br.BaseStream.Position = wave.pData + dataOffset;
            wave.Data = br.ReadBytes(waveDat[6]);

            wave.uniqueID = new UniqueID();

            // Read wave name.
            wave.uniqueID.pName = (int)(br.BaseStream.Position = waveDat[1]);
            wave.uniqueID.Name = br.ReadAscii();

            // Read UUID.
            wave.uniqueID.pUuid = (int)(br.BaseStream.Position = waveDat[0]);
            wave.uniqueID.Uuid = new Guid(br.ReadBytes(16));

            wave.uniqueID.flags = (uint)waveDat[2];

            return wave;
        }

        public override string ToString()
        {
            return uniqueID.ToString();
        }
    }
}
