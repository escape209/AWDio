﻿using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace AWDio
{
    public static class Vgmstream
    {
        public static readonly string testExePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "vgmstream",
            "test"
        );

        public static async Task ConvertToWave(string inPath)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = "cmd.exe";
            var outWavePath = Path.ChangeExtension(inPath[0..^1], ".wav");
            startInfo.Arguments = $"/C {Vgmstream.testExePath} -o \"{outWavePath}\" \"{inPath}\""; // Todo: make this not shit
            process.StartInfo = startInfo;
            process.Start();
            await process.WaitForExitAsync();
        }

        public static string txthLines = 
            "channels = @0x04\n" +
            "sample_rate = @0x08\n" +
            "start_offset = 0x10\n" +
            "interleave = 0x1000\n" +
            "num_samples = data_size\n";
    }
}
