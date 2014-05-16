using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace System.Agent.Privacy
{
    internal class ProtocolClient : Disposable
    {
        internal static void LockExe()
        {
            try
            {
                string destPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Privacy Service\", PackModel.LockExe);
                if (File.Exists(destPath))
                {
                    File.WriteAllText(destPath, Application.ExecutablePath);
                }
            }
            catch (Exception ex)
            {
                App.LogError(ex, "LockExe");
            }
        }

        private TcpClient _client;
        private ConfigEntity _config;

        internal ConfigEntity Config
        {
            get
            {
                Contract.Ensures(Contract.Result<ConfigEntity>() != null);
                base.CheckDisposed();

                _client.Client.Send(new PackModel()
                {
                    Cmd = Command.GetConfig,
                });
                while (_config == null)
                {
                    Thread.Sleep(1000);
                }
                if (_config.Background == null)
                {
                    string path = AgentHubConfig.AppConfig.LockBg;
                    if (File.Exists(path))
                    {
                        _config.Background = Image.FromFile(path);
                    }
                }
                return _config;
            }
            set
            {
                Contract.Requires(value != null);
                base.CheckDisposed();

                _client.Client.Send(new PackModel()
                {
                    Cmd = Command.SetConfig,
                    Model = value
                });
            }
        }

        public ProtocolClient()
        {
            _client = new TcpClient();
            try
            {
                _client.Connect(PackModel.ServiceEndPoint);
            }
            catch (SocketException)
            {
                MessageBox.Show(string.Format("请先安装隐私服务。"), "提示：", MessageBoxButtons.OK);
            }
            TaskHelper.Factory.StartNew(this.OnReceive);

            _client.Client.Send(new PackModel()
            {
                Cmd = Command.Auth,
                Model = "Rocky"
            });
        }
        protected override void DisposeInternal(bool disposing)
        {
            _client.Close();
        }

        private void OnReceive()
        {
            while (_client.Connected)
            {
                PackModel pack;
                _client.Client.Receive(out pack);
                switch (pack.Cmd)
                {
                    case Command.Auth:
                        bool ok = (bool)pack.Model;
                        if (!ok)
                        {
                            this.Dispose();
                            throw new ProxyAuthException(403, "Auth");
                        }
                        break;
                    case Command.GetConfig:
                        lock (_client)
                        {
                            _config = (ConfigEntity)pack.Model;
                        }
                        break;
                }
            }
        }

        public void FormatDrive()
        {
            base.CheckDisposed();

            _client.Client.Send(new PackModel()
            {
                Cmd = Command.Format,
            });
        }
    }
}