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
using System.Threading;

namespace System.Agent.Privacy
{
    partial class PrivacyService : ServiceBase
    {
        #region Fields
        private TcpListener _listener;
        private Process _proc;
        #endregion

        #region Methods
        public PrivacyService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            App.LogInfo("Privacy service start... It's monitor on disk {0}.", PrivacyHelper.Config.Drive);

            _listener = new TcpListener(PackModel.ServiceEndPoint);
            _listener.Start();
            _listener.BeginAcceptTcpClient(this.AcceptTcpClient, null);

            KeepLock();
        }
        private void AcceptTcpClient(IAsyncResult ar)
        {
            if (_listener == null)
            {
                return;
            }
            _listener.BeginAcceptTcpClient(this.AcceptTcpClient, null);

            var client = _listener.EndAcceptTcpClient(ar);
            App.LogDebug("Client: {0}", client.Client.RemoteEndPoint);
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
                            return;
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
                            if (Monitor.TryEnter(_listener))
                            {
                                try
                                {
                                    PrivacyHelper.FormatDrive(PrivacyHelper.Config.Drive);
                                }
                                finally
                                {
                                    Monitor.Exit(_listener);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            App.LogError(ex, "FormatDrive");
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
            App.LogInfo("Privacy service stop...");
        }

        private void KeepLock()
        {
            new JobTimer(state =>
            {
                bool run = _proc == null;
                if (!run)
                {
                    try
                    {
                        Process.GetProcessById(_proc.Id);
                    }
                    catch (ArgumentException)
                    {
                        run = true;
                    }
                }
                if (run)
                {
                    try
                    {
                        string path = File.ReadAllText(App.CombinePath(PackModel.LockExe));
                        var proc = new ProcessStarter(path, "1");
                        _proc = proc.Start();
                        App.LogDebug("ProcessStarter={0}", _proc.Id);
                    }
                    catch (Exception ex)
                    {
                        App.LogError(ex, "KeepLock");
                    }
                }
            }, TimeSpan.FromSeconds(1)).Start();
        }
        #endregion
    }
}