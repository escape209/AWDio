using System;
using System.Numerics;

namespace AWDio
{
    public static class SystemUuid
    {
        public static readonly Guid PlayStation = new(-0x55153654, -0x3C8, 0x4917, new byte[] { 0xAE, 0x81, 0x64, 0xEA, 0xDB, 0xC7, 0x93, 0x53 });
        public static readonly Guid Xbox = new(0x453A2D04, -0x1BA1, 0x4BC8, new byte[] { 0x81, 0xF0, 0xDF, 0x75, 0x8B, 0x01, 0xF2, 0x73 });

        public static bool IsValid(Guid uuid)
        {
            return uuid == PlayStation || uuid == Xbox;
        }

        public static string GetSysUuidName(Guid uuid)
        {
            if (uuid == PlayStation)
            {
                return nameof(PlayStation);
            }
            else if (uuid == Xbox)
            {
                return nameof(Xbox);
            }

            return "Unknown";
        }
    }
}
