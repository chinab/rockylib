using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace System.Net
{
    internal sealed class Socks5Request : ISocksRFC
    {
        #region NestedTypes
        /// <summary>
        /// Defines the SOCKS5 authentication phase
        /// </summary>
        internal enum Socks5Phase
        {
            spGreeting,
            spAuthenticating,
            spConnecting
        }
        #endregion

        #region Properties
        public Socks5Phase Phase { get; private set; }
        public NetworkCredential Credential { get; private set; }
        public Socks5Command Command { get; private set; }
        public EndPoint EndPoint { get; private set; }
        #endregion

        #region Methods
        public byte[] ToPackets()
        {
            var bPack = new List<byte>();
            switch (this.Phase)
            {
                case Socks5Phase.spGreeting:
                    if (this.Credential == null)
                    {
                        //匿名代理
                        bPack.Add(5);
                        bPack.Add(1);
                        bPack.Add(0);
                        this.Phase = Socks5Phase.spConnecting;
                    }
                    else
                    {
                        //匿名或用户名密码代理
                        bPack.Add(5);
                        bPack.Add(2);
                        bPack.Add(0);
                        bPack.Add(2);
                        this.Phase = Socks5Phase.spAuthenticating;
                    }
                    break;
                case Socks5Phase.spAuthenticating:
                    if (this.Credential == null)
                    {
                        throw new ProxyAuthException(403, "Socks5 Server request a credential");
                    }

                    bPack.Add(1);

                    bPack.Add(Convert.ToByte(this.Credential.UserName.Length));
                    bPack.AddRange(Encoding.ASCII.GetBytes(this.Credential.UserName));

                    bPack.Add(Convert.ToByte(this.Credential.Password.Length));
                    bPack.AddRange(Encoding.ASCII.GetBytes(this.Credential.Password));
                    this.Phase = Socks5Phase.spConnecting;
                    break;
                case Socks5Phase.spConnecting:
                    bPack.Add(5);
                    bPack.Add((byte)this.Command);
                    bPack.Add(0);
                    bPack.Add((byte)((this.EndPoint is DnsEndPoint) ? 3 : 1));

                    Socks4Request.PackIn(bPack, this.EndPoint);
                    break;
            }
            return bPack.ToArray();
        }

        public void ParsePack(byte[] bPack)
        {
            switch (this.Phase)
            {
                case Socks5Phase.spGreeting:
                    {
                        if (bPack[0] != 5)
                        {
                            throw new ProxyAuthException(403, "Only Socks5 request are supported");
                        }

                        //要求匿名代理
                        if (bPack[1] == 1 && bPack[2] == 0)
                        {
                            this.Credential = null;
                            this.Phase = Socks5Phase.spConnecting;
                        }
                        //要求匿名或用户名密码代理
                        else
                        {
                            this.Credential = new NetworkCredential();
                            this.Phase = Socks5Phase.spAuthenticating;
                        }
                    }
                    break;
                case Socks5Phase.spAuthenticating:
                    {
                        if (bPack[0] != 1)
                        {
                            throw new ProxyAuthException(403, "Only Socks5 request are supported");
                        }
                        if (this.Credential == null)
                        {
                            throw new ProxyAuthException(403, "Socks5Phase.spAuthenticating");
                        }

                        int length = Convert.ToInt32(bPack[1]);
                        this.Credential.UserName = Encoding.ASCII.GetString(bPack, 2, length);

                        int offset = 2 + length;
                        length = Convert.ToInt32(bPack[offset]);
                        this.Credential.Password = Encoding.ASCII.GetString(bPack, offset + 1, length);
                        this.Phase = Socks5Phase.spConnecting;
                    }
                    break;
                case Socks5Phase.spConnecting:
                    {
                        if (bPack[0] != 5 || bPack[2] != 0)
                        {
                            throw new ProxyAuthException(403, "Only Socks5 request are supported");
                        }

                        this.Command = (Socks5Command)bPack[1];

                        int offset = 4;
                        if (bPack[3] == 3)
                        {
                            DnsEndPoint de;
                            Socks4Request.PackOut(bPack, ref offset, out de);
                            this.EndPoint = de;
                        }
                        else
                        {
                            IPEndPoint ipe;
                            Socks4Request.PackOut(bPack, ref offset, out ipe);
                            this.EndPoint = ipe;
                        }
                    }
                    break;
            }
        }
        #endregion
    }

    internal enum Socks5Command
    {
        TcpConnect = 0x01,
        TcpBind = 0x02,
        UdpAssociate = 0x03
    }
}