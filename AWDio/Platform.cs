using System;

namespace AWDio
{
    public class Platform
    {
        public Guid Uuid { get; set; }
        public string Codec { get; set; }

        public Platform(Guid uuid, string codec)
        {
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
            new Guid(-0x55153654, -0x3C8, 0x4917, new byte[] { 0xAE, 0x81, 0x64, 0xEA, 0xDB, 0xC7, 0x93, 0x53 }),
            "PSX"
        );

        public static Platform Xbox = new(
            new Guid(-0x55153654, -0x3C8, 0x4917, new byte[] { 0xAE, 0x81, 0x64, 0xEA, 0xDB, 0xC7, 0x93, 0x53 }), 
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
