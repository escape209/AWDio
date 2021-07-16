using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace AWDio
{
    class Program
    {
        static readonly string usage = "AWDio by escape209\nUsage: AWDio [AWD path]";

        static async Task Main(string[] args)
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
                        await AWD.SerializeAsync(awd, args[1]);
                    }
                    break;
                default:
                    return;
            }
        }
    }
}
