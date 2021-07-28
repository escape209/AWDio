using System;
using System.Threading.Tasks;

using AwdIO.Rwa;

namespace AwdIO
{
    class Program
    {
        static readonly string usage = "AwdIO by escape209\nUsage: AwdIO [infile | indir] [outfile | outdir]\n";

        static async Task Main(string[] args)
        {
            Console.WriteLine(usage);

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
