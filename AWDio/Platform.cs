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
            new Guid("AAEAC9AC-FC38-4917-AE81-64EADBC79353"),
            "PSX"
        );

        public static Platform Xbox = new(
            "Xbox",
            new Guid("453A2D04-E45F-4BC8-81F0-DF758B01F273"), 
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
