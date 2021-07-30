using System;

namespace AwdIO.Rwa
{
    public class Platform
    {
        public string Name { get; set; }
        public Guid Uuid { get; set; }

        public Platform(string name, Guid uuid)
        {
            Name = name;
            Uuid = uuid;
        }

        public static Platform[] Platforms = new Platform[]
        {
            new("PlayStation", new("AAEAC9AC-FC38-4917-AE81-64EADBC79353")),
            new("Xbox",        new("453A2D04-E45F-4BC8-81F0-DF758B01F273"))
        };
    }
}
