using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace System.Agent.Privacy
{
    [Serializable]
    public sealed class PackModel
    {
        [NonSerialized]
        public static readonly IPEndPoint ServiceEndPoint;

        static PackModel()
        {
            ServiceEndPoint = new IPEndPoint(IPAddress.Loopback, 53);
        }

        public Command Cmd { get; set; }
        public object Model { get; set; }
    }
}