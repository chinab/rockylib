using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net
{
    [Flags]
    public enum TunnelCommand
    {
        None = 0,
        xInject = 1 << 0,
        KeepAlive = 1 << 1,
        Receive = 1 << 2,
        Send = 1 << 3,
        DeviceIdentity = 1 << 4,
        UdpSend = 1 << 5,
        UdpReceive = 1 << 6,
    }
}