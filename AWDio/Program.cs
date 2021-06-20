using System;
using System.IO;

namespace AWDio
{
    class Program
    {
        static readonly string usage = "AWDio by escape209\nUsage: AWDio [AWD path]";

        static void Main(string[] args)
        {
            Console.WriteLine(usage);
            if (args.Length != 1 || !File.Exists(args[0]))
            {
                return;
            }
            Console.WriteLine();
            AWD.Deserialize(args[0]);
        }
    }
}
