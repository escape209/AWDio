using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AWDio
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
    }
}
