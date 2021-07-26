using System;
using System.Linq;

namespace AwdIO
{
    public class Platform
    {
        public string Name { get; set; }
        public Guid Uuid { get; set; }

        public Platform(string name, string uuid)
        {
            Name = name;
            try
            {
                Uuid = new Guid(uuid);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid UUID");
                Uuid = Guid.Empty;
            }
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

        public static readonly Platform PlayStation = new("PlayStation", "AAEAC9AC-FC38-4917-AE81-64EADBC79353");
        public static readonly Platform Xbox        = new("Xbox",        "453A2D04-E45F-4BC8-81F0-DF758B01F273");

        public static Platform FromUuid(Guid uuid)
        {
            return Platforms.SingleOrDefault(s => s.Uuid == uuid);
        }
        
        public static Platform FromName(string name)
        {
            return Platforms.SingleOrDefault(s => s.Name == name);
        }

        public static bool IsValid(Guid uuid)
        {
            return FromUuid(uuid) != null;
        }
    }
}
