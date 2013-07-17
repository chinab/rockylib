using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics.Contracts;

namespace System.Net
{
    [Serializable]
    public sealed class TransferConfig
    {
        #region Fields
        private const uint MinChunkLength = (uint)(1024 ^ 2 * 32);
        [NonSerialized]
        internal readonly string FilePath;
        #endregion

        #region Properties
        public long FileLength { get; private set; }
        public string Checksum { get; private set; }
        public ushort ChunkCount { get; private set; }
        public string FileName { get; set; }
        public object State { get; set; }
        #endregion

        #region Constructors
        public TransferConfig(string filePath, uint chunkLength = 0)
        {
            Contract.Requires(!string.IsNullOrEmpty(filePath));
            var file = new FileInfo(filePath);
            if (!file.Exists)
            {
                throw new FileNotFoundException(string.Empty, filePath);
            }

            this.FilePath = file.FullName;
            this.FileLength = file.Length;
            using (var stream = file.OpenRead())
            {
                this.Checksum = CryptoManaged.MD5Hash(stream);
            }
            ushort chunkSize = (ushort)(this.FileLength / Math.Max(MinChunkLength, chunkLength));
            this.ChunkCount = chunkSize;
            this.FileName = file.Name;
        }
        #endregion
    }
}