using System;
using System.Runtime.Remoting;
using System.IO;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace System.Net
{
    public static class MonitorChannel
    {
        private const string Name = "MonitorService";

        public static void Server(ushort listenPort)
        {
            var channel = new TcpServerChannel("MonitorServer", listenPort);
            ChannelServices.RegisterChannel(channel, true);
            RemotingConfiguration.RegisterWellKnownServiceType(typeof(MonitorProxy), Name, WellKnownObjectMode.SingleCall);
        }

        public static MonitorProxy Client(EndPoint endPoint)
        {
            var channel = new TcpClientChannel("MonitorClient", new BinaryClientFormatterSinkProvider());
            try
            {
                ChannelServices.RegisterChannel(channel, true);
            }
            catch (RemotingException ex)
            {
                App.LogError(ex, "MonitorChannel Client");
            }
            return (MonitorProxy)Activator.GetObject(typeof(MonitorProxy), string.Format("tcp://{0}/{1}", endPoint, Name));
        }
    }
}