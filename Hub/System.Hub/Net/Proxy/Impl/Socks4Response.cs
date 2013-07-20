using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal sealed class Socks4Response : ISocksRFC
    {
        #region NestedTypes
        public enum ResponseStatus
        {
            [Description("Request granted")]
            RequestGranted = 0x5a,

            [Description("Request rejected or failed")]
            RequestRejected = 0x5b,

            [Description("Client is not running identd")]
            NotRunningIdentd = 0x5c,

            [Description("Client's identd could not confirm the user ID string in the request")]
            IdentdCouldNotConfirm = 0x5d,
        }
        #endregion

        #region Properties
        public ResponseStatus Status { get; set; }
        public IPEndPoint RemoteEndPoint { get; private set; }
        #endregion

        #region Constructors
        public Socks4Response(IPEndPoint remoteEndPoint = null)
        {
            if (remoteEndPoint == null)
            {
                remoteEndPoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            }

            this.RemoteEndPoint = remoteEndPoint;
        }
        #endregion

        #region Methods
        public byte[] ToPackets()
        {
            var bPack = new List<byte>();
            bPack.Add(0x00);

            bPack.Add((byte)this.Status);

            Socks4Request.PackIn(bPack, this.RemoteEndPoint, false);

            return bPack.ToArray();
        }

        public void ParsePack(byte[] bPack)
        {
            if (bPack[0] != 0x00)
            {
                throw new ProxyAuthException(403, "Only Socks4/4a response are supported");
            }

            var response = this;
            response.Status = (ResponseStatus)bPack[1];

            int offset = 2;
            IPEndPoint ipe;
            Socks4Request.PackOut(bPack, ref offset, out ipe, false);
            response.RemoteEndPoint = ipe;

            if (response.Status != Socks4Response.ResponseStatus.RequestGranted)
            {
                throw new ProxyAuthException((int)response.Status, response.Status.ToDescription());
            }
        }
        #endregion
    }
}