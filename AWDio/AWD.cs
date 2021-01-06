using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AWDio {
    public class AWD {
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
            int llPos = pName + (int)Math.Ceiling((float)((nameStr.Length + 3) / 4)) * 4;
            int dataPos = BitConverter.ToInt32(buf, 0x28);
            Directory.CreateDirectory(outDir);
            br.BaseStream.Position = llPos;
            int pos = llPos;
            string txthPath = Path.Combine(outDir, ".txth");
            if (File.Exists(txthPath)) {
                File.Delete(txthPath);
            }
            File.CreateText(txthPath).Close();
            var txthSb = new StringBuilder();
            if (Enumerable.SequenceEqual(platUuid, psUuid)) {
                txthSb.AppendLine("codec = PSX");
            } else {
                txthSb.AppendLine("codec = PCM16LE");
            }
            txthSb.AppendJoin(Environment.NewLine, "channels = @0x1D$1", "sample_rate = @0x10", "start_offset = 0x100", "interleave = 0x1000", "num_samples = data_size");
            File.WriteAllLines(txthPath, new string[] { txthSb.ToString() });
            while (pos < dataPos) {
                int trkUuidPos = BitConverter.ToInt32(buf, pos);
                if (trkUuidPos == 0) {
                    break;
                }
                int trkNamePos = BitConverter.ToInt32(buf, pos + 4);
                int trkNameLen = (Array.IndexOf(buf, (byte)0x00, trkNamePos, 16) - trkNamePos) + 1;
                byte[] trkHdrBuf = buf.AsSpan(pos, ((trkNamePos + trkNameLen) - pos) + 0x10).ToArray();
                string trkNameStr = Encoding.ASCII.GetString(buf, trkNamePos, trkNameLen).Trim('\0');
                int len = BitConverter.ToInt32(buf, pos + 0x34);
                int dat = BitConverter.ToInt32(buf, pos + 0x4C) + dataPos;
                br.BaseStream.Position = dat;
                string outTrkPath = Path.Combine(outDir, trkNameStr) + ".bin";
                var trkFs = new FileStream(Path.GetTempFileName(), FileMode.OpenOrCreate);
                trkFs.Write(trkHdrBuf);
                trkFs.SetLength(0x100);
                trkFs.Position = trkFs.Length;
                trkFs.Write(br.ReadBytes(len), 0, len);
                File.Copy(trkFs.Name, outTrkPath, true);
                trkFs.Close();
                pos = trkUuidPos + 0x10;
            }
            Console.WriteLine("Internal name          " + nameStr);
            IStructuralEquatable se = platUuid;
            Console.Write("Platform               ");
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
    }
}
