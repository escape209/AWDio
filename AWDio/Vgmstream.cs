using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace AWDio
{
    public static class Vgmstream
    {
        public static readonly string testExePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "vgmstream",
            "test"
        );

        public static void ConvertToWave(string inPath)
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
            process.WaitForExit();
            File.Delete(inPath);
        }

        public static string[] txthLines = new string[] {
            "channels = @0x04",
            "sample_rate = @0x08",
            "start_offset = 0x10",
            "interleave = 0x1000",
            "num_samples = data_size"
        };
    }
}
