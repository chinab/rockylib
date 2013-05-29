using System;
using System.Runtime.Remoting;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Net;

namespace Rocky.Net
{
    public static class MonitorChannel
    {
        private const string Name = "MonitorService";

        public static void Server(ushort listenPort)
        {
            var channel = new TcpChannel(listenPort);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(Monitor), Name, WellKnownObjectMode.SingleCall);
        }

        public static Monitor Client(EndPoint endPoint)
        {
            var channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            return (Monitor)Activator.GetObject(typeof(Monitor), string.Format("tcp://{0}/{1}", endPoint, Name));
        }
    }
}