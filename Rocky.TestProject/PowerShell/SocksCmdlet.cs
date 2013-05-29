using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Rocky.Net;

namespace Rocky.TestProject
{
    [Cmdlet("Connect", "Socks")]
    public class SocksConnectCommand : PSCmdlet
    {
        [Alias("R")]
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        public string RemoteEndPoint { get; set; }

        [Alias("PT")]
        [Parameter(Mandatory = false, Position = 2, ValueFromPipeline = true)]
        public ProtocolType Protocol { get; set; }

        protected override void ProcessRecord()
        {
            IPEndPoint remoteIpe = null;
            try
            {
                remoteIpe = SocketHelper.ParseEndPoint(this.RemoteEndPoint);
                base.WriteVerbose(string.Format("Connect {0}...", remoteIpe));
                var result = new SocksConnectResult();
                result.Protocol = this.Protocol;
                if (this.Protocol == ProtocolType.Udp)
                {
                    var client = new UdpClient();
                    client.Connect(remoteIpe);
                    byte[] data = Encoding.UTF8.GetBytes("Hello world!");
                    client.Send(data, data.Length);
                    var asyncResult = client.BeginReceive(ar =>
                    {
                        var remoteIpe2 = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                        byte[] data2 = client.EndReceive(ar, ref remoteIpe2);
                        result.Success = data.SequenceEqual(data2);
                    }, null);
                    asyncResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(10D));
                    result.Success = false;
                    result.LocalEndPoint = client.Client.LocalEndPoint;
                    result.RemoteEndPoint = client.Client.RemoteEndPoint;
                    client.Close();
                }
                else
                {
                    var client = new TcpClient();
                    try
                    {
                        client.Connect(remoteIpe);
                        result.Success = true;
                    }
                    catch (SocketException)
                    {
                        result.Success = false;
                    }
                    result.LocalEndPoint = client.Client.LocalEndPoint;
                    result.RemoteEndPoint = client.Client.RemoteEndPoint;
                    client.Close();
                }
                base.WriteObject(result);
            }
            catch (Exception ex)
            {
                base.ThrowTerminatingError(new ErrorRecord(ex, "Connect", ErrorCategory.NotSpecified, remoteIpe));
            }
            base.ProcessRecord();
        }
    }
    public class SocksConnectResult
    {
        public ProtocolType Protocol { get; set; }
        public EndPoint LocalEndPoint { get; set; }
        public EndPoint RemoteEndPoint { get; set; }
        public bool Success { get; set; }

        public override string ToString()
        {
            return string.Format("{0} {1} Connect {2} {3}", this.Protocol, this.LocalEndPoint, this.RemoteEndPoint, this.Success);
        }
    }

    [Cmdlet("Listen", "Socks")]
    public class SocksListenCommand : PSCmdlet
    {
        [Alias("P")]
        [Parameter(Mandatory = true, Position = 1, ValueFromPipeline = true)]
        public ushort Port { get; set; }

        [Alias("PT")]
        [Parameter(Mandatory = false, Position = 2, ValueFromPipeline = true)]
        public ProtocolType Protocol { get; set; }

        protected override void ProcessRecord()
        {
            if (this.Protocol == ProtocolType.Udp)
            {
                var listener = new UdpClient(this.Port);
                var remoteIpe = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                base.WriteVerbose("Listen start...");
                TaskHelper.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        byte[] data = listener.Receive(ref remoteIpe);
                        listener.Send(data, data.Length, remoteIpe);
                        base.WriteObject(string.Format("SocksListen: {0} Connected", remoteIpe));
                    }
                });
            }
            else
            {
                var listener = new TcpListener(IPAddress.Any, this.Port);
                listener.Start();
                base.WriteVerbose("Listen start...");
                TaskHelper.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        var client = listener.AcceptTcpClient();
                        base.WriteObject(string.Format("SocksListen: {0} Connected", client.Client.RemoteEndPoint));
                        client.Close();
                    }
                });
            }
            base.ProcessRecord();
        }
    }
}