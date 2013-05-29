using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Rocky.Net
{
    [ContractClass(typeof(IChunkModelContract))]
    public interface IChunkModel
    {
        bool IsCompleted { get; }
        void Initialize(string filePath, long position, long length, long bytesTransferred = 0L, long transferLength = -1L);
        void ReportProgress(out long bytesTransferred, out long transferLength);
        void Run();
    }

    [ContractClassFor(typeof(IChunkModel))]
    internal abstract class IChunkModelContract : IChunkModel
    {
        bool IChunkModel.IsCompleted
        {
            get { return default(bool); }
        }

        void IChunkModel.Initialize(string filePath, long position, long length, long bytesTransferred, long transferLength)
        {
            Contract.Requires(!string.IsNullOrEmpty(filePath));
            Contract.Requires(position >= 0L && length > 0L);
        }

        void IChunkModel.ReportProgress(out long bytesTransferred, out long transferLength)
        {
            transferLength = bytesTransferred = default(long);
        }

        void IChunkModel.Run()
        {

        }
    }
}