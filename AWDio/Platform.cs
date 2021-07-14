using System;

namespace AWDio
{
    public class Platform
    {
        public string Name { get; set; }
        public Guid Uuid { get; set; }
        public string Codec { get; set; }

        public Platform(string name, Guid uuid, string codec)
        {
            Name = name;
            Uuid = uuid;
            Codec = codec;
        }

        static Platform()
        {
            Platforms = new Platform[]
            {
                PlayStation,
                Xbox
            };
        }

        public static Platform[] Platforms;

        public static Platform PlayStation = new(
            "PlayStation",
            new Guid(new byte[] { 
                0xAC, 0xC9, 0xEA, 0xAA, 
                0x38, 0xFC, 0x17, 0x49, 
                0xAE, 0x81, 0x64, 0xEA, 
                0xDB, 0xC7, 0x93, 0x53 
            }),
            "PSX"
        );

        public static Platform Xbox = new(
            "Xbox",
            new Guid(new byte[] { 
                0x04, 0x2D, 0x3A, 0x45, 
                0x5F, 0xE4, 0xC8, 0x4B, 
                0x81, 0xF0, 0xDF, 0x75,
                0x8B, 0x01, 0xF2, 0x73 
            }), 
            "PCM16LE"
        );

        public static Platform FromUuid(Guid uuid)
        {
            foreach (var plat in Platforms)
            {
                if (uuid == plat.Uuid)
                {
                    return plat;
                }
            }
            return null;
        }

        public static bool IsValid(Guid uuid)
        {
            return FromUuid(uuid) != null;
        }
    }
}
