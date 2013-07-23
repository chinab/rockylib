using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;

namespace System.Agent.Privacy
{
    partial class PrivacyService : ServiceBase
    {
        #region Fields
        private TcpListener _listener;
        #endregion

        #region Methods
        public PrivacyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            var config = PrivacyHelper.Config;
            Hub.LogInfo("Privacy service start... It's monitor on disk {0}.", config.Drive);
            ShowLock();

            _listener = new TcpListener(PackModel.ServiceEndPoint);
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
            TaskHelper.Factory.StartNew(this.OnReceive, client);
        }
        private void OnReceive(object state)
        {
            var client = (TcpClient)state;
            while (client.Connected)
            {
                PackModel pack;
                client.Client.Receive(out pack);
                switch (pack.Cmd)
                {
                    case Command.Auth:
                        bool ok = (string)pack.Model == "Rocky";
                        pack.Model = ok;
                        client.Client.Send(pack);
                        if (!ok)
                        {
                            client.Close();
                        }
                        break;
                    case Command.GetConfig:
                        pack.Model = PrivacyHelper.Config;
                        client.Client.Send(pack);
                        break;
                    case Command.SetConfig:
                        PrivacyHelper.Config = (ConfigEntity)pack.Model;
                        break;
                    case Command.Format:
                        try
                        {
                            var config = PrivacyHelper.Config;
							if(	System.Threading.Monitor.TryEnter(config))
							{
								try
							{
                            	PrivacyHelper.FormatDrive(config.Drive);
							}
							finally{
								System.Threading.Monitor.Exit(config);
							}
							}
						}
                        catch (Exception ex)
                        {
                            Hub.LogError(ex, "FormatDrive");
                        }
                        break;
				}
            }
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            _listener.Stop();
            _listener = null;
            Hub.LogInfo("Privacy service stop...");
        }

        private void ShowLock()
        {
            TaskHelper.Factory.StartNew(() =>
            {
                try
                {
                    string path = @"D:\Projects\GitLib\Hub\System.Agent\bin\Debug\Agent.exe";
                    //string path = Hub.CombinePath("Agent.exe");
                    var proc = new ProcessStarter(path, "test0", "1");
                    var p = proc.Start();
                    Hub.LogDebug("ProcessStarter={0}", p.Id);
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