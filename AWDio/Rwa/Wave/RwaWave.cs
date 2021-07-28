using System;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace AwdIO.Rwa
{
    public class RwaWave
    {
        public const int size = 0x5C;

        [JsonProperty(Order = 0)]
        public RwaUniqueID uniqueID = new();

        [JsonProperty(Order = 2)]
        public int pWaveDef;

        [JsonProperty(Order = 3)]
        public int pState;

        [JsonProperty(Order = 4)]
        public uint flags;

        byte[] data = Array.Empty<byte>();
        [JsonIgnore]
        public byte[] Data
        {
            get { return data; }
            set { 
                data = value;
                format.length = value.Length;
            }
        } 

        [JsonIgnore]
        public int pData;

        [JsonIgnore]
        public int pObj;

        [JsonProperty(Order = 5)]
        public RwaFormat format = new();

        public int Serialize(BinaryWriter bw, int pData)
        {
            // Skip uniqueID.
            int ppUniqueID = (int)bw.BaseStream.Position;
            bw.BaseStream.Seek(sizeof(int) * 3, SeekOrigin.Current);

            bw.Write(pWaveDef);

            // Write format and targetFormat.
            for (int i = 0; i < 2; i++)
            {
                format.Serialize(bw);
            }

            bw.Write(0); // Skip data.
            int ppData = (int)bw.BaseStream.Position;
            bw.Write(int.MaxValue);
            bw.Write(pState);
            bw.Write(flags);
            bw.Write(pObj);

            // AWD will write the link.
            bw.BaseStream.Seek(sizeof(int) * 3, SeekOrigin.Current);

            // UniqueID data.
            int pName = (int)bw.BaseStream.Position;
            bw.Write(Encoding.ASCII.GetBytes(uniqueID.Name + '\0'));

            while (bw.BaseStream.Position % 4 != 0)
            {
                bw.BaseStream.Position++;
            }

            int pUuid = (int)bw.BaseStream.Position;
            bw.Write(uniqueID.Uuid.ToByteArray());

            bw.BaseStream.Position = ppUniqueID;
            bw.Write(pUuid);
            bw.Write(pName);

            bw.BaseStream.Position = ppData;
            bw.Write(pData);

            bw.BaseStream.Position = pUuid + 0x10;

            return 0; 
        }

        public static RwaWave Deserialize(BinaryReader br, long dataOffset)
        {
            // Read raw wave header.
            var wave = new RwaWave();

            wave.uniqueID = new RwaUniqueID();
            wave.uniqueID.pUuid = br.ReadInt32();
            wave.uniqueID.pName = br.ReadInt32();
            wave.uniqueID.flags = br.ReadUInt32();

            wave.pWaveDef = br.ReadInt32();

            wave.format = RwaFormat.GetWaveFormat(br);
            br.BaseStream.Seek(RwaFormat.size + sizeof(int), SeekOrigin.Current); // Skip targetFormat and uncompLen.

            wave.pData = br.ReadInt32();
            wave.pState = br.ReadInt32();
            wave.flags = br.ReadUInt32();
            wave.pObj = br.ReadInt32();

            // Read audio data.
            br.BaseStream.Position = wave.pData + dataOffset;
            wave.Data = br.ReadBytes(wave.format.length);

            // Read wave name.
            br.BaseStream.Position = wave.uniqueID.pName;
            wave.uniqueID.Name = br.ReadAscii();

            // Read UUID.
            br.BaseStream.Position = wave.uniqueID.pUuid;
            wave.uniqueID.Uuid = new Guid(br.ReadBytes(16));

            return wave;
        }

        public override string ToString()
        {
            return uniqueID.ToString();
        }
    }
}
