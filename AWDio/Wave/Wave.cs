﻿using System;
using System.IO;
using System.Text;

using Newtonsoft.Json;

namespace AWDio.Wave
{
    public class Wave
    {
        public const int size = 0x5C;

        public UniqueID uniqueID;

        [JsonProperty(Order = 0)]
        public string Name
        {
            get { return uniqueID.Name; }
            set
            {
                if (Encoding.UTF8.GetByteCount(value) != value.Length)
                {
                    uniqueID.Name = value;
                }
            }
        }

        [JsonProperty(Order = 1)]
        public uint uncompLength;

        [JsonProperty(Order = 2)]
        public int pWaveDef;

        [JsonProperty(Order = 3)]
        public int pState;

        [JsonProperty(Order = 4)]
        public uint flags;

        [JsonProperty(Order = 12)]
        public int FormatDataType { 
            get { return format.dataType; }
            set { format.dataType = value; }
        }

        [JsonIgnore]
        public Format format;

        [JsonIgnore] 
        public byte[] Data { get; set; } = Array.Empty<byte>();

        [JsonIgnore]
        public int pData;

        [JsonIgnore]
        public int pObj;

        public int Serialize(BinaryWriter bw)
        {
            // Skip uniqueID.
            int ppUniqueID = (int)bw.BaseStream.Position;
            bw.BaseStream.Seek(sizeof(int) * 3, SeekOrigin.Current);

            bw.Write(pWaveDef);

            // Write format and targetFormat.
            for (int i = 0; i < 2; i++)
            {
                format.Serialize(bw, Data.Length);
            }

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

        public static Wave Deserialize(BinaryReader br, long dataOffset)
        {
            // Read raw wave header.
            var wave = new Wave();

            wave.uniqueID = new UniqueID();
            wave.uniqueID.pUuid = br.ReadInt32();
            wave.uniqueID.pName = br.ReadInt32();
            wave.uniqueID.flags = br.ReadUInt32();

            wave.pWaveDef = br.ReadInt32();

            br.BaseStream.Seek(sizeof(int) * 2, SeekOrigin.Current);
            int length = br.ReadInt32();
            br.BaseStream.Seek(-(sizeof(int) * 3), SeekOrigin.Current);

            wave.format = Format.GetWaveFormat(br);
            br.BaseStream.Seek(Format.size, SeekOrigin.Current); // Skip targetFormat.

            wave.uncompLength = br.ReadUInt32();
            wave.pData = br.ReadInt32();
            wave.pState = br.ReadInt32();
            wave.flags = br.ReadUInt32();
            wave.pObj = br.ReadInt32();

            // Read audio data.
            br.BaseStream.Position = wave.pData + dataOffset;
            wave.Data = br.ReadBytes(length);

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
