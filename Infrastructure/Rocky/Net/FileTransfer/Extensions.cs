using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace Rocky.Net
{
    public static partial class Extensions
    {
        internal static bool IsAllCompleted(this IChunkModel[] instance)
        {
            Contract.Requires(instance != null);

            return instance.All(t => t.IsCompleted);
        }

        internal static void ReportSpeed(this IChunkModel[] instance, TransferProgress progress, ref long lastTransferred)
        {
            Contract.Requires(instance != null);

            long bytesTransferred = 0L, transferLength = 0L;
            for (int i = 0; i < instance.Length; i++)
            {
                long perBytesTransferred, perTransferLength;
                instance[i].ReportProgress(out perBytesTransferred, out perTransferLength);
                bytesTransferred += perBytesTransferred;
                transferLength += perTransferLength;
            }
            progress.OnProgressChanged(bytesTransferred - lastTransferred, bytesTransferred);
            lastTransferred = bytesTransferred;
        }
    }
}