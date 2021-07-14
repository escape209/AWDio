using System.IO;

using Newtonsoft.Json;

namespace AWDio.Wave
{
    public class Format
    {
        public static readonly int size = 0x1C;

        [JsonIgnore]
        public uint sampleRate;

        [JsonIgnore]
        public int dataType;

        // public int length; // length moved to Wave.

        [JsonIgnore]
        public byte bitDepth;

        [JsonIgnore]
        public byte noChannels;

        [JsonIgnore]
        public int pMiscData;

        [JsonIgnore]
        public uint miscDataSize;

        [JsonIgnore]
        public byte flags;

        [JsonIgnore]
        public byte reserved;

        public int Serialize(BinaryWriter bw, int length)
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

        public static Format GetWaveFormat(BinaryReader br)
        {
            Format format = new();
            format.sampleRate = br.ReadUInt32();
            format.dataType = br.ReadInt32();
            br.BaseStream.Seek(sizeof(int), SeekOrigin.Current);

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
