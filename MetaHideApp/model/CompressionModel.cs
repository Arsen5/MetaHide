using System;
using System.IO;
using System.IO.Compression;

namespace MetaHide.model
{
    public class CompressionModel
    {
        public byte[] Compress(byte[] data, int thresholdKB = 1)
        {
            if (data.Length <= thresholdKB * 1024)
                return data;

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return memoryStream.ToArray();
            }
        }

        public byte[] Decompress(byte[] compressedData)
        {
            using (var compressedStream = new MemoryStream(compressedData))
            using (var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                gzipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        public double GetCompressionEfficiency(byte[] original, byte[] compressed)
        {
            if (original.Length == 0) return 0;
            return (1 - (double)compressed.Length / original.Length) * 100;
        }

        public bool ShouldCompress(byte[] data, int thresholdKB)
        {
            return data.Length > thresholdKB * 1024;
        }
    }
}