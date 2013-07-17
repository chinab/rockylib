using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Diagnostics;

namespace System.Net
{
    public class PingClient
    {
        #region Fields
        private Lazy<Stopwatch> _watcher;
        private long[] _milliseconds;
        #endregion

        #region Properties
        public int SendCount
        {
            get { return _milliseconds.Length; }
            set
            {
                Contract.Requires(value > 0);

                if (_milliseconds == null)
                {
                    _milliseconds = new long[value];
                }
                else
                {
                    Array.Resize(ref _milliseconds, value);
                }
            }
        }
        public int PerSleep { get; set; }
        #endregion

        #region Constructors
        public PingClient(int sendCount = 4, int perSleep = 200)
        {
            _watcher = new Lazy<Stopwatch>();
            this.SendCount = sendCount;
            this.PerSleep = perSleep;
        }
        #endregion

        #region Methods
        private void Sleep()
        {
            int perSleep = this.PerSleep;
            if (perSleep > 0)
            {
                System.Threading.Thread.Sleep(perSleep);
            }
        }

        public PingResult Send(string sAddrOrIPe)
        {
            IPAddress addr;
            if (IPAddress.TryParse(sAddrOrIPe, out addr))
            {
                return Send(addr);
            }
            var ipe = SocketHelper.ParseEndPoint(sAddrOrIPe);
            return Send(ipe);
        }

        public PingResult Send(IPAddress addr, int timeout = 3000)
        {
            using (var client = new Ping())
            {
                for (int i = 0; i < _milliseconds.Length; i++)
                {
                    var reply = client.Send(addr, timeout);
                    if (reply.Status == IPStatus.Success)
                    {
                        _milliseconds[i] = reply.RoundtripTime;
                    }
                    else
                    {
                        _milliseconds[i] = 0L;
                    }
                    Sleep();
                }
            }
            return new PingResult(_milliseconds);
        }

        public PingResult Send(IPEndPoint ipe, int timeout = 3000, Func<bool> environment = null)
        {
            for (int i = 0; i < _milliseconds.Length; i++)
            {
                var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                sock.Blocking = false;
                try
                {
                    _watcher.Value.Restart();
                    sock.Connect(ipe);
                }
                catch (SocketException ex)
                {
                    // 10035 == WSAEWOULDBLOCK
                    if (ex.NativeErrorCode != 10035)
                    {
                        throw;
                    }

                    // Wait until connected or timeout.
                    // SelectWrite: returns true, if processing a Connect, and the connection has succeeded.
                    if (sock.Poll((timeout * 1000), SelectMode.SelectWrite))
                    {
                        if (environment != null && !environment())
                        {
                            goto loss;
                        }

                        _watcher.Value.Stop();
                        _milliseconds[i] = _watcher.Value.ElapsedMilliseconds;
                        continue;
                    }
                loss:
                    _milliseconds[i] = 0L;
                }
                finally
                {
                    sock.Dispose();
                    Sleep();
                }
            }
            return new PingResult(_milliseconds);
        }
        #endregion
    }

    #region PingResult
    public struct PingResult
    {
        public static bool operator ==(PingResult t1, PingResult t2)
        {
            return t1.LossPercentage == t2.LossPercentage && t1.AverageValue == t2.AverageValue;
        }
        public static bool operator !=(PingResult t1, PingResult t2)
        {
            return t1.LossPercentage != t2.LossPercentage && t1.AverageValue != t2.AverageValue;
        }
        public static bool operator >(PingResult t1, PingResult t2)
        {
            return t1.LossPercentage > t2.LossPercentage && t1.AverageValue > t2.AverageValue;
        }
        public static bool operator >=(PingResult t1, PingResult t2)
        {
            return t1.LossPercentage >= t2.LossPercentage && t1.AverageValue >= t2.AverageValue;
        }
        public static bool operator <(PingResult t1, PingResult t2)
        {
            return t1.LossPercentage < t2.LossPercentage && t1.AverageValue < t2.AverageValue;
        }
        public static bool operator <=(PingResult t1, PingResult t2)
        {
            return t1.LossPercentage <= t2.LossPercentage && t1.AverageValue <= t2.AverageValue;
        }

        public int LossPercentage { get; set; }
        public double? AverageValue { get; set; }

        public PingResult(long[] milliseconds)
            : this()
        {
            int lossCount = milliseconds.Where(t => t < 1L).Count();
            if (lossCount > 0)
            {
                this.LossPercentage = (lossCount / milliseconds.Length) * 100;
            }
            if (lossCount < milliseconds.Length)
            {
                this.AverageValue = milliseconds.Where(t => t > 0L).Average();
            }
        }

        public bool Equals(PingResult obj)
        {
            return this == obj;
        }
        public override bool Equals(object obj)
        {
            if (obj is PingResult)
            {
                return this.Equals((PingResult)obj);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    #endregion
}