using System;
using System.Threading.Tasks;

using AwdIO.Rwa;

namespace AwdIO
{
    class Program
    {
        static readonly string usage = "AWDio by escape209\nUsage: AWDio [AWD path]";

        static async Task Main(string[] args)
        {
            Console.WriteLine(usage);
            Console.WriteLine();

            Awd awd = Awd.Empty;

            switch (args.Length)
            {
                case 1:
                    await Awd.DeserializeAsync(args[0], false);
                    break;
                case 2:
                    awd = await Awd.DeserializeAsync(args[0], false);
                    await Awd.SerializeAsync(awd, args[1]);
                    break;
            }
        }
    }
}
