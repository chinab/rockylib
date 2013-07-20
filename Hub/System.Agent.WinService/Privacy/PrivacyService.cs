using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;

namespace System.Agent.Privacy
{
    partial class PrivacyService : ServiceBase
    {
        #region Static
        internal static PrivacyConfigEntity Config { get; private set; }

        static PrivacyService()
        {
            var disk = from t in DriveInfo.GetDrives()
                       where t.DriveType == DriveType.Ram || t.DriveType == DriveType.Removable
                       orderby t.Name ascending
                       select t;
            char diskName = disk.Last().Name[0];
            Config = new PrivacyConfigEntity()
            {
                Password = "Rocky123",
                Opacity = 100,
                Drive = diskName
            };
            Hub.LogDebug("PrivacyService's monitor on disk {0}.", diskName);
        }

        public static void FormatDrive()
        {
            try
            {
                PrivacyHelper.FormatDrive(Config.Drive);
            }
            catch (Exception ex)
            {
                Hub.LogError(ex, "FormatDrive");
            }
        }
        #endregion

        #region Fields
        private TcpListener _listener;
        private JobTimer _job;
        #endregion

        #region Methods
        public PrivacyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            ShowLock();

            _listener = new TcpListener(IPAddress.Any, 53);
            _listener.Start();
            _listener.BeginAcceptTcpClient(this.AcceptTcpClient, null);

            _job = new JobTimer(state =>
            {
                var idle = PrivacyHelper.GetIdleTime();
                if (idle.TotalSeconds >= 30D)
                {
                    ShowLock();
                }
            }, TimeSpan.FromSeconds(1));
            _job.Start();
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
                    ShowLock();
                    result = true;
                    break;
                case Cmd.Format:
                    FormatDrive();
                    break;
            }
            stream.WriteByte(Convert.ToByte(result));
            client.Close();
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            _job.Stop();
            _listener.Stop();
            _listener = null;
        }

        private void ShowLock()
        {
            var locker = new LockScreen();
            locker.Show();
        }
        #endregion
    }
}