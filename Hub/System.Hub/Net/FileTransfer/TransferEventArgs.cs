using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net
{
    public sealed class TransferEventArgs : EventArgs
    {
        public TransferConfig Config { get; private set; }
        public TransferProgress Progress { get; set; }
        public bool Cancel { get; set; }

        public TransferEventArgs(TransferConfig config)
        {
            this.Config = config;
        }
    }
}