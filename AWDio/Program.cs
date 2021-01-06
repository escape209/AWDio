using System;
using System.IO;

namespace AWDio {
    class Program {
        static readonly string usage = "AWDio by escape209\nUsage: AWDio [AWD path] [output folder]\n";

        static void Main(string[] args) {
            Console.WriteLine(usage);
            if (args.Length != 2 || !File.Exists(args[0])) {
                return;
            }
            AWD.Extract(args[0], args[1], false);
        }
    }
}
