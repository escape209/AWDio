using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AWDio {
    public class AWD {
        static readonly string testExePath = Path.Combine("vgmstream", "test.exe");

        static readonly int Sec1Tag = 0x809;
        static readonly byte[] psUuid = new byte[] { 0xAC, 0xC9, 0xEA, 0xAA, 0x38, 0xFC, 0x17, 0x49, 0xAE, 0x81, 0x64, 0xEA, 0xDB, 0xC7, 0x93, 0x53 };
        static readonly byte[] xbUuid = new byte[] { 0x04, 0x2D, 0x3A, 0x45, 0x5F, 0xE4, 0xC8, 0x4B, 0x81, 0xF0, 0xDF, 0x75, 0x8B, 0x01, 0xF2, 0x73 };

        public static int Extract(string inFile, string outDir, bool convert) {
            var fs = new FileStream(inFile, FileMode.Open);
            var br = new BinaryReader(fs);
            if (br.ReadInt32() != Sec1Tag) {
                return 1;
            }
            int unk0 = br.ReadInt32();
            int ctrSize = br.ReadInt32();
            br.BaseStream.Position = 0;
            byte[] buf = br.ReadBytes(ctrSize);
            var platUuid = buf.AsSpan(0x18, 0x10).ToArray();
            int pName = BitConverter.ToInt32(buf, 0x30);
            int nameLen = (Array.IndexOf(buf, (byte)0x00, pName, 12) - pName) + 1;
            string nameStr = Encoding.ASCII.GetString(buf, pName, nameLen);
            int dataPos = BitConverter.ToInt32(buf, 0x28);
            Directory.CreateDirectory(outDir);
            int pos = (int)(br.BaseStream.Position = pName + (int)Math.Ceiling((float)((nameStr.Length + 3) / 4)) * 4);
            string txthPath = Path.Combine(outDir, ".txth");
            File.CreateText(txthPath).Close();
            string codecStr = "codec = " + (Enumerable.SequenceEqual(platUuid, psUuid) ? "PSX" : "PCM16LE");
            File.WriteAllLines(txthPath, new string[] { codecStr, "channels = @0x1D$1", "sample_rate = @0x10", "start_offset = 0x100", "interleave = 0x1000", "num_samples = data_size" });
            var tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            while (pos < dataPos) {
                int trkUuidPos = BitConverter.ToInt32(buf, pos);
                if (trkUuidPos <= 0 || trkUuidPos > br.BaseStream.Length) {
                    break;
                }
                int trkNamePos = BitConverter.ToInt32(buf, pos + 4);
                int trkNameLen = (Array.IndexOf(buf, (byte)0x00, trkNamePos, 16) - trkNamePos) + 1;
                byte[] trkHdrBuf = buf.AsSpan(pos, ((trkNamePos + trkNameLen) - pos) + 0x10).ToArray();
                string trkNameStr = Encoding.ASCII.GetString(buf, trkNamePos, trkNameLen).Trim('\0');
                int len = BitConverter.ToInt32(buf, pos + 0x34);
                int dat = BitConverter.ToInt32(buf, pos + 0x4C) + dataPos;
                br.BaseStream.Position = dat;
                string outTrkPath = Path.Combine(outDir, trkNameStr) + ".wav";
                var trkFs = File.Create(Path.Combine(outDir, Path.GetRandomFileName()));
                string trkFsName = trkFs.Name;
                trkFs.Write(trkHdrBuf);
                trkFs.SetLength(0x100);
                trkFs.Position = trkFs.Length;
                trkFs.Write(br.ReadBytes(len), 0, len);
                trkFs.Close();
                var pStartInfo = new ProcessStartInfo(testExePath, string.Join(' ', "-o", condPathQuote(outTrkPath), condPathQuote(trkFsName))) { RedirectStandardOutput = true };
                var test = Process.Start(pStartInfo);
                test.WaitForExit();
                File.Delete(trkFsName);
                pos = trkUuidPos + 0x10;
            }
            File.Delete(txthPath);
            Console.WriteLine("Internal name          " + nameStr);
            Console.Write("Platform               ");
            IStructuralEquatable se = platUuid;
            if (se.Equals(psUuid, StructuralComparisons.StructuralEqualityComparer)) {
                Console.WriteLine("PlayStation 2");
            } else if (se.Equals(xbUuid, StructuralComparisons.StructuralEqualityComparer)) {
                Console.WriteLine("Xbox");
            } else {
                Console.WriteLine("Unknown\nCould not determine target platform of AWD.");
                return 2;
            }

            return 0;
        }

        // Add quotes to path if it doesn't have them already.
        static string condPathQuote(string path) {
            if (path[0] == '"') {
                return path;
            }
            return '"' + path + '"';
        }
    }
}
