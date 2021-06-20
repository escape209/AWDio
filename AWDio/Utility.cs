using System.IO;
using System.Text;

namespace AWDio
{
    static class Utility
    {
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
    }
}
