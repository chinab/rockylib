using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Text;

namespace Rocky.Net
{
    /// <summary>
    /// http://en.wikipedia.org/wiki/SOCKS
    /// </summary>
    internal sealed class Socks4Request : ISocksRFC
    {
        #region Static
        internal static void PackIn(List<byte> bPack, EndPoint endPoint, bool socks5 = true)
        {
            Contract.Requires(bPack != null & endPoint != null);

            var de = endPoint as DnsEndPoint;
            if (de != null)
            {
                if (socks5)
                {
                    bPack.Add(Convert.ToByte(de.Host.Length));
                    bPack.AddRange(Encoding.ASCII.GetBytes(de.Host));
                    PackIn(bPack, de.Port);
                }
                else
                {
                    PackIn(bPack, de.Port);
                    bPack.Add(Convert.ToByte(de.Host.Length));
                    bPack.AddRange(Encoding.ASCII.GetBytes(de.Host));
                }
                return;
            }

            var ipe = (IPEndPoint)endPoint;
            if (socks5)
            {
                bPack.AddRange(ipe.Address.GetAddressBytes());
                PackIn(bPack, ipe.Port);
            }
            else
            {
                PackIn(bPack, ipe.Port);
                bPack.AddRange(ipe.Address.GetAddressBytes());
            }
        }

        internal static void PackOut(byte[] bPack, ref int offset, out DnsEndPoint de, bool socks5 = true)
        {
            Contract.Requires(!bPack.IsNullOrEmpty());

            string host;
            int port;
            if (socks5)
            {
                int length = Convert.ToInt32(bPack[offset++]);
                host = Encoding.ASCII.GetString(bPack, offset, length);
                offset += length;
                PackOut(bPack, ref offset, out port);
            }
            else
            {
                PackOut(bPack, ref offset, out port);
                int length = Convert.ToInt32(bPack[offset++]);
                host = Encoding.ASCII.GetString(bPack, offset, length);
                offset += length;
            }
            de = new DnsEndPoint(host, port);
        }
        internal static void PackOut(byte[] bPack, ref int offset, out IPEndPoint ipe, bool socks5 = true)
        {
            Contract.Requires(!bPack.IsNullOrEmpty());

            IPAddress ip;
            int port;
            if (socks5)
            {
                byte[] addr = new byte[4];
                Array.ConstrainedCopy(bPack, offset, addr, 0, addr.Length);
                ip = new IPAddress(addr);
                offset += addr.Length;
                PackOut(bPack, ref offset, out port);
            }
            else
            {
                PackOut(bPack, ref offset, out port);
                byte[] addr = new byte[4];
                Array.ConstrainedCopy(bPack, offset, addr, 0, addr.Length);
                ip = new IPAddress(addr);
                offset += addr.Length;
            }
            ipe = new IPEndPoint(ip, port);
        }

        private static void PackIn(List<byte> bPack, int port)
        {
            bPack.Add(Convert.ToByte((port & 0xFF00) >> 8));
            bPack.Add(Convert.ToByte(port & 0xFF));
        }
        private static void PackOut(byte[] bPack, ref int offset, out int port)
        {
            port = (bPack[offset++] << 8) + bPack[offset++];
        }

        private static string ReadNullTerminatedString(byte[] array, int startIndex, out int endIndex)
        {
            endIndex = startIndex;
            for (int i = startIndex; i < array.Length; i++)
            {
                if (array[i] == 0x0)
                {
                    endIndex = i;
                    return Encoding.ASCII.GetString(array, startIndex, i - startIndex);
                }
            }
            return string.Empty;
        }
        #endregion

        #region Properties
        public SocksProxyType ProxyType { get; private set; }
        public Socks4Command Command { get; private set; }
        public int RemotePort { get; private set; }
        public IPAddress RemoteIP { get; private set; }
        public string UserID { get; private set; }
        public string RemoteHost { get; private set; }
        #endregion

        #region Constructors
        public Socks4Request()
        {

        }
        public Socks4Request(Socks4Command cmd, IPEndPoint remoteEndPoint, string userID = null)
            : this(cmd, remoteEndPoint.Port, remoteEndPoint.Address, userID ?? string.Empty, string.Empty)
        {

        }
        public Socks4Request(Socks4Command cmd, DnsEndPoint remoteEndPoint, string userID = null)
            : this(cmd, remoteEndPoint.Port, new IPAddress(new byte[] { 0, 0, 0, 1 }), userID ?? string.Empty, remoteEndPoint.Host)
        {

        }
        private Socks4Request(Socks4Command cmd, int remotePort, IPAddress remoteHostIP, string userID, string remoteHost)
        {
            this.ProxyType = string.IsNullOrEmpty(remoteHost) ? SocksProxyType.Socks4 : SocksProxyType.Socks4a;
            this.Command = cmd;
            this.RemotePort = remotePort;
            this.RemoteIP = remoteHostIP;
            this.UserID = userID;
            this.RemoteHost = remoteHost;
        }
        #endregion

        #region Methods
        public byte[] ToPackets()
        {
            if (this.RemoteIP == null)
            {
                throw new ProxyAuthException(403, "Request remoteIP");
            }

            var bPack = new List<byte>();
            bPack.Add(0x04);
            bPack.Add((byte)this.Command);

            PackIn(bPack, new IPEndPoint(this.RemoteIP, this.RemotePort), false);

            if (this.UserID.Length > 0)
            {
                bPack.AddRange(Encoding.ASCII.GetBytes(this.UserID));
            }
            bPack.Add(0x00);

            if (this.ProxyType == SocksProxyType.Socks4a)
            {
                bPack.AddRange(Encoding.ASCII.GetBytes(this.RemoteHost));
                bPack.Add(0x00);
            }

            return bPack.ToArray();
        }

        public void ParsePack(byte[] bPack)
        {
            if (bPack[0] != 0x04 || bPack[1] != 0x01 || bPack[1] != 0x02)
            {
                throw new ProxyAuthException(403, "Only Socks4/4a request are supported");
            }

            var request = this;
            request.ProxyType = SocksProxyType.Socks4;

            request.Command = (Socks4Command)bPack[1];

            int offset = 2;
            IPEndPoint ipe;
            PackOut(bPack, ref offset, out ipe, false);
            request.RemotePort = ipe.Port;
            request.RemoteIP = ipe.Address;

            int userIDEndIndex;
            request.UserID = ReadNullTerminatedString(bPack, 8, out userIDEndIndex);

            byte[] ipAddr = request.RemoteIP.GetAddressBytes();
            if (ipAddr[0] == 0 && ipAddr[1] == 0 && ipAddr[2] == 0 && ipAddr[3] != 0) // Socks 4a
            {
                request.ProxyType = SocksProxyType.Socks4a;
                int dummy;
                request.RemoteHost = ReadNullTerminatedString(bPack, userIDEndIndex + 1, out dummy);
            }
        }
        #endregion
    }

    internal enum Socks4Command
    {
        Connect = 0x01,
        Bind = 0x02
    }
}