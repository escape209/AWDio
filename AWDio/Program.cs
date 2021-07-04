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
            Console.WriteLine();
            switch (args.Length)
            {
                case 1:
                    AWD.Deserialize(args[0]);
                    break;
                case 2:
                    var awd = AWD.Deserialize(args[0]);
                    if (awd != AWD.Empty)
                    {
                        int ret = AWD.Serialize(awd, args[1]);
                    }
                    break;
                default:
                    return;
            }
        }
    }
}
