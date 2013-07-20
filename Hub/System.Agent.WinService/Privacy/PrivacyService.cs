using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;

namespace System.Agent.Privacy
{
    partial class PrivacyService : ServiceBase
    {
        #region Static
        private const uint SHFMT_ID_DEFAULT = 0xFFFF, SHFMT_OPT_FULL = 0x0001;
        [DllImport("shell32.dll")]
        private static extern int SHFormatDrive(IntPtr hWnd, uint drive, uint fmtID, uint Options);

        private static readonly string[] drive = new string[] 
        {
            "A:",
            "B:",
            "C:",
            "D:",
            "E:",
            "F:",
            "G:",
            "H:",
            "I:",
            "J:",
            "K:",
            "L:",
            "M:",
            "N:",
            "O:",
            "P:",
            "Q:",
            "R:",
            "S:",
            "T:",
            "U:",
            "V:",
            "W:",
            "X:",
            "Y:",
            "Z:"
        };

        internal static PrivacyConfigEntity Config { get; private set; }

        static PrivacyService()
        {
            Config = new PrivacyConfigEntity()
            {
                Password = "Rocky123",
                Opacity = 100,

            };
        }

        public static void FormatDrive(IntPtr handle)
        {
            //SHFormatDrive(handle, 0, SHFMT_ID_DEFAULT, SHFMT_OPT_FULL);
            string[] str = Environment.GetLogicalDrives();
            Hub.LogDebug(string.Join(",", str));
        }
        #endregion

        private TcpListener _listener;

        public PrivacyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            _listener = new TcpListener(IPAddress.Any, 53);
            _listener.Start();
            _listener.BeginAcceptTcpClient(this.AcceptTcpClient, null);
        }
        private void AcceptTcpClient(IAsyncResult ar)
        {
            if (_listener == null)
            {
                return;
            }
            _listener.BeginAcceptTcpClient(this.AcceptTcpClient, null);

            var client = _listener.EndAcceptTcpClient(ar);
            bool result = false;
            var stream = client.GetStream();
            switch ((Cmd)stream.ReadByte())
            {
                case Cmd.Lock:
                    Config = (PrivacyConfigEntity)Serializer.Deserialize(stream);

                    break;
                case Cmd.Format:

                    break;
            }
            stream.WriteByte(Convert.ToByte(result));
            client.Close();
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            _listener.Stop();
            _listener = null;
        }
    }
}