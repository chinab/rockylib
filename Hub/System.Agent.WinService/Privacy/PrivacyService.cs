using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace System.Agent.Privacy
{
    partial class PrivacyService : ServiceBase
    {
        #region Static
        internal static PrivacyConfigEntity Config { get; private set; }

        static PrivacyService()
        {
            var disk = from t in DriveInfo.GetDrives()
                       where t.DriveType == DriveType.Fixed || t.DriveType == DriveType.Removable
                       orderby t.Name ascending
                       select t;
            char diskName = disk.Last().Name[0];
            Config = new PrivacyConfigEntity()
            {
                Password = "123456",
                Opacity = 100,
                Drive = diskName
            };
            Hub.LogInfo("PrivacyService's monitor on disk {0}.", diskName);
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
            Hub.LogInfo("Service Start...");
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
            //_job.Start();
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
            Hub.LogInfo("Service Stop...");
        }

        private void ShowLock()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new LockScreen());
            TaskHelper.Factory.StartNew(() =>
            {
                try
                {
                    string path = @"D:\Projects\GitLib\Hub\System.Agent\bin\Debug\Agent.exe";
                    var p2 = NativeMethods.CreateProcessAsUser(path, "");
                    //p.WaitForExit();

                    var p = new ProcessStarter("test0", path);
                    p.Run();

                    MessageBox.Show("p ShowLock");
                }
                catch (Exception ex)
                {
                    Hub.LogError(ex, "ShowLock");
                }
            });
        }
        #endregion
    }
}