using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AwdIO
{
    public static class Utility
    {
        public static int RoundUp(int value, int mul)
        {
            int round = (int)(Math.Ceiling(value / (double)mul) * (double)mul);
            return round;
        }

        public static string ReadAscii(this BinaryReader br)
        {
            long start = br.BaseStream.Position;
            int len = 0;
            while (br.ReadByte() != 0)
            {
                len++;
            }
            br.BaseStream.Position = start;
            var bytes = br.ReadBytes(len);
            return Encoding.ASCII.GetString(bytes);
        }

        public static Task DeleteFileAsync(string path)
        {
            return Task.Run(() => File.Delete(path));
        }

        public static bool IsNullOrZero(int? val)
        {
            return val == 0 || val == null;
        }

        public static bool? ConsoleReadBool(string input)
        {
            if (input.Equals("N", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else if (input.Equals("Y", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else
            {
                return null;
            }
        }
    }
}
