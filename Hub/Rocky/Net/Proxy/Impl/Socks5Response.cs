using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    internal sealed class Socks5Response : ISocksRFC
    {
        #region NestedTypes
        public enum ResponseStatus
        {
            [Description("Success")]
            Success = 0x00,

            [Description("General failure")]
            Failure = 0x01,

            [Description("Connection not allowed by ruleset")]
            ConnectionNotAllowed = 0x02,

            [Description("Network unreachable")]
            NetworkUnreachable = 0x03,

            [Description("Host unreachable")]
            HostUnreachable = 0x04,

            [Description("Connection refused by destination host")]
            ConnectionRefused = 0x05,

            [Description("TTL expired")]
            TTLExpired = 0x06,

            [Description("Command not supported / protocol error")]
            ProtocolError = 0x07,

            [Description("Address type not supported")]
            AddressNotSupported = 0x08,
        }
        #endregion

        #region Properties
        public Socks5Request.Socks5Phase Phase { get; private set; }
        public bool Anonymous { get; set; }
        public ResponseStatus Status { get; set; }
        public IPEndPoint RemoteEndPoint { get; set; }
        #endregion

        #region Methods
        public byte[] ToPackets()
        {
            var bPack = new List<byte>();
            switch (this.Phase)
            {
                case Socks5Request.Socks5Phase.spGreeting:
                    if (this.Anonymous)
                    {
                        bPack.Add(5);
                        bPack.Add(0);
                        this.Phase = Socks5Request.Socks5Phase.spConnecting;
                    }
                    else
                    {
                        bPack.Add(5);
                        bPack.Add(2);
                        this.Phase = Socks5Request.Socks5Phase.spAuthenticating;
                    }
                    break;
                case Socks5Request.Socks5Phase.spAuthenticating:
                    if (this.Status != ResponseStatus.Success)
                    {
                        throw new ProxyAuthException(403, "Authentication failure, connection must be closed");
                    }

                    bPack.Add(1);
                    bPack.Add((byte)this.Status);
                    this.Phase = Socks5Request.Socks5Phase.spConnecting;
                    break;
                case Socks5Request.Socks5Phase.spConnecting:
                    bPack.Add(5);
                    bPack.Add(0);
                    bPack.Add(0);
                    bPack.Add(1);

                    Socks4Request.PackIn(bPack, this.RemoteEndPoint);
                    break;
            }
            return bPack.ToArray();
        }

        public void ParsePack(byte[] bPack)
        {
            switch (this.Phase)
            {
                case Socks5Request.Socks5Phase.spGreeting:
                    if (bPack[0] != 5)
                    {
                        throw new ProxyAuthException(403, "Only Socks5 response are supported");
                    }
                    if (bPack[1] == 0xFF)
                    {
                        throw new ProxyAuthException(0xFF, "Authentication method not supported");
                    }

                    if (this.Anonymous = bPack[1] == 0)
                    {
                        this.Phase = Socks5Request.Socks5Phase.spConnecting;
                    }
                    else
                    {
                        this.Phase = Socks5Request.Socks5Phase.spAuthenticating;
                    }
                    break;
                case Socks5Request.Socks5Phase.spAuthenticating:
                    if (bPack[0] != 1)
                    {
                        throw new ProxyAuthException(403, "Only Socks5 response are supported");
                    }

                    this.Phase = Socks5Request.Socks5Phase.spConnecting;

                    this.Status = (ResponseStatus)bPack[1];
                    if (this.Status != ResponseStatus.Success)
                    {
                        throw new ProxyAuthException((int)this.Status, this.Status.ToDescription());
                    }
                    break;
                case Socks5Request.Socks5Phase.spConnecting:
                    if (bPack[0] != 5)
                    {
                        throw new ProxyAuthException(403, "Only Socks5 response are supported");
                    }

                    int offset = 4;
                    IPEndPoint ipe;
                    Socks4Request.PackOut(bPack, ref offset, out ipe);
                    this.RemoteEndPoint = ipe;
                    break;
            }
        }
        #endregion
    }
}