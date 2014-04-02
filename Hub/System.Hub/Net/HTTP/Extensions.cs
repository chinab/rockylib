using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

namespace System.Net
{
    public static partial class Extensions
    {
        public static Encoding GetContentEncoding(this HttpWebResponse instance)
        {
            Contract.Requires(instance != null);

            return string.IsNullOrEmpty(instance.CharacterSet) ? Encoding.UTF8 : Encoding.GetEncoding(instance.CharacterSet);
        }

        public static string GetResponseText(this HttpWebResponse instance, Encoding contentEncoding = null)
        {
            Contract.Requires(instance != null);

            var resStream = instance.GetResponseStream();
            if (instance.ContentEncoding.Equals("gzip", StringComparison.OrdinalIgnoreCase))
            {
                var mem = new MemoryStream();
                using (var gzipStream = new GZipStream(resStream, CompressionMode.Decompress, true))
                {
                    gzipStream.FixedCopyTo(mem);
                }
                mem.Position = 0L;
                resStream = mem;
            }
            using (var reader = new StreamReader(resStream, contentEncoding ?? instance.GetContentEncoding()))
            {
                return reader.ReadToEnd();
            }
        }
    }
}