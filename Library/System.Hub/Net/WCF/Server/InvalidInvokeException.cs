using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net.WCF
{
    [Serializable]
    public class InvalidInvokeException : InvalidOperationException
    {
        public InvokeFaultLevel FaultLevel { get; set; }

        public InvalidInvokeException(string message)
            : base(message)
        {
        }
    }
}