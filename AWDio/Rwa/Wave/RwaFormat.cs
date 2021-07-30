using System;
using System.IO;

using Newtonsoft.Json;

namespace AwdIO.Rwa
{
    public class RwaFormat
    {
        public static readonly int size = 0x1C;

        public uint sampleRate;
        public int dataType;

        [JsonIgnore]
        public int length;

        [JsonIgnore]
        public TimeSpan Duration
        {
            get 
            {
                var dur = (float)(length * bitDepth) / (sampleRate / 1000);
                return TimeSpan.FromMilliseconds(dur); 
            }
        }

        public byte bitDepth;
        public byte noChannels;
        public int pMiscData;
        public uint miscDataSize;
        public byte flags;
        public byte reserved;

        public int Serialize(BinaryWriter bw)
        {
            bw.Write(sampleRate);
            bw.Write(dataType);
            bw.Write(length);
            bw.Write((uint)(bitDepth | noChannels << 8));
            bw.Write(pMiscData);
            bw.Write(miscDataSize);
            bw.Write((uint)(flags | reserved << 8));

            return 0;
        }

        public static RwaFormat GetWaveFormat(BinaryReader br)
        {
            RwaFormat format = new();
            format.sampleRate = br.ReadUInt32();
            format.dataType = br.ReadInt32();
            format.length = br.ReadInt32();
            format.bitDepth = br.ReadByte();
            format.noChannels = br.ReadByte();
            br.BaseStream.Seek(sizeof(short), SeekOrigin.Current);

            format.pMiscData = br.ReadInt32();
            format.miscDataSize = br.ReadUInt32();

            format.flags = br.ReadByte();
            format.reserved = br.ReadByte();
            br.BaseStream.Seek(sizeof(short), SeekOrigin.Current);

            return format;
        }
    }
}
