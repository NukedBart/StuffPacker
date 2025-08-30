using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Packer.Features
{
    public static class StuffPacker
    {
        public static void Pack(string outputPath, ObservableCollection<KeyValuePair<string, string>> gFiles)
        {
            using var fs = new FileStream(outputPath + "res.stuff", FileMode.Create, FileAccess.Write);
            using var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: true);

            var files = new List<(string virtualPath, byte[] data)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in gFiles)
            {
                var vpath = kv.Key?.Trim();
                var rpath = kv.Value?.Trim();

                if (string.IsNullOrEmpty(vpath))
                    throw new ArgumentException("VirtualPath must not be empty.");

                if (string.IsNullOrEmpty(rpath))
                    throw new ArgumentException($"RealPath for \"{vpath}\" must not be empty.");

                vpath = vpath.Replace('/', '\\');

                if (!seen.Add(vpath))
                    throw new InvalidOperationException($"Duplicate VirtualPath detected: \"{vpath}\"");

                rpath = Path.GetFullPath(rpath);
                if (!File.Exists(rpath))
                    throw new FileNotFoundException($"Real file does not exist: {rpath}", rpath);

                var fi = new FileInfo(rpath);

                if (fi.Length > uint.MaxValue)
                    throw new OverflowException($"File too large (>4GB): {rpath}");

                var nameBytes = Encoding.UTF8.GetBytes(vpath);
                if ((uint)nameBytes.Length > uint.MaxValue)
                    throw new OverflowException($"VirtualPath too long: {vpath}");

                // Pre-checks done, read file data
                byte[] data = File.ReadAllBytes(rpath);

                files.Add((vpath, data));
            }

            var revisions = GenerateRevisions(files);
            files.Add(("revisions.txt", Encoding.ASCII.GetBytes(revisions)));

            // Header Segment
            bw.Write((uint)files.Count);
            foreach (var f in files)
            {
                var nameBytes = Encoding.ASCII.GetBytes(f.virtualPath);

                bw.Write((uint)f.data.Length);                 // 文件大小
                bw.Write((uint)nameBytes.Length);        // 路径长度（含 '\0'）
                bw.Write(nameBytes);
                bw.Write((byte)0);                             // null 结尾
            }

            // Data Segment
            foreach (var f in files)
            {
                bw.Write(f.data);
            }

            // Signature Padding
            // There is 0x00000000 betweeen the data segment and the signature segment.
            bw.Write(0); // 4 字节 0

            // EmbedFs Signature
            bw.Write(Encoding.ASCII.GetBytes("EmbedFs 1.0"));
            bw.Write((byte)0);
        }

        private static string GenerateRevisions(List<(string virtualPath, byte[] data)> files)
        {
            var sb = new StringBuilder();
            using var sha1 = SHA1.Create();

            foreach (var f in files.Where(x => x.virtualPath != "revisions.txt"))
            {
                string hash = BitConverter.ToString(sha1.ComputeHash(f.data)).Replace("-", "").ToLower();
                sb.AppendLine($"{f.virtualPath},{hash}");
            }

            return sb.ToString();
        }
    }

}
