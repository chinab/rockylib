using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Forms;
using SharpCompress.Archive;
using SharpCompress.Common;

namespace System.Agent
{
    public partial class MainForm : Form, IFormEntry
    {
        public static bool Confirm(string content, string title = "确认操作")
        {
            return MessageBox.Show(content, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        }

        #region Fields
        internal const ushort AssistPort = 1078, TransferPort = 1079;
        private NamedPipeServerStream _pipeServer;
        private FileTransfer _trans;
        private Process _proxifierProc;
        #endregion

        #region Properties
        public bool CanClose { get; set; }
        #endregion

        #region Load
        public MainForm()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _pipeServer = new NamedPipeServerStream(SecurityPolicy.PipeName, PipeDirection.InOut);
            TaskHelper.Factory.StartNew(this.Callback);

            lb_user.SelectedIndexChanged += lb_user_SelectedIndexChanged;
            button1.Click += button1_Click;
            textBox2.Click += textBox2_Click;
            button2.Click += button2_Click;

            Threading.Thread.Sleep(10000);
            System.Agent.Remote.MonitorChannel.Server(AssistPort);
            System.Agent.Privacy.ProtocolClient.LockExe();
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.Cancel = !this.CanClose)
            {
                this.HideForm();
            }
            else
            {
                if (_proxifierProc != null && !_proxifierProc.HasExited)
                {
                    _proxifierProc.Kill();
                    _proxifierProc.Dispose();
                }
            }
            base.OnFormClosing(e);
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            var tip = new ToolTip()
            {
                AutomaticDelay = 5000,
                AutoPopDelay = 50000,
                InitialDelay = 100,
                ReshowDelay = 500,
            };
            tip.SetToolTip(tb_destIpe, "暂只支持局域网IP");
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var form = new System.Agent.Remote.MonitorClient();
            form.Show();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            var form = new System.Agent.Privacy.LockScreen();
            form.Show();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            var form = new System.Agent.Encode.EncodeForm();
            form.Show();
        }
        #endregion

        #region Methods
        private void ShowForm(bool showBar = false)
        {
            pb_b.Value = 0;
            pb_b.Visible = showBar;
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.Focus();
        }

        private void AppendLog(string format, params object[] args)
        {
            tb_msg.AppendText(DateTime.Now.ToString("HH:mm:ss "));
            tb_msg.AppendText(string.Format(format, args));
            tb_msg.AppendText(Environment.NewLine);
        }

        private void HideForm()
        {
            //this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }

        private void Callback()
        {
            while (_pipeServer != null)
            {
                _pipeServer.WaitForConnection();
                try
                {
                    int cmd = _pipeServer.ReadByte();
                    switch (cmd)
                    {
                        case 0:
                            this.ShowList();
                            break;
                        case 1:
                            this.RunProxifier();
                            break;
                        case 2:
                            this.RunPrivacyService();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Hub.LogError(ex, "PipeServer");
                }
                _pipeServer.WaitForPipeDrain();
                if (_pipeServer.IsConnected)
                {
                    _pipeServer.Disconnect();
                }
            }
        }
        #endregion

        #region ShowList
        private Tuple<string, Guid>[] _deviceIdentitys;

        private void ShowList()
        {
            _deviceIdentitys = AgentApp.Instance.GetDeviceIdentity().ToArray();
            lb_user.Items.Clear();
            lb_user.Items.Add("-我的设备-");
            for (int i = 0; i < _deviceIdentitys.Length; )
            {
                var device = _deviceIdentitys[i];
                int j = device.Item1.IndexOf(@"\");
                lb_user.Items.Add(string.Format(" {0}. {1}", ++i, device.Item1.Remove(j)));
            }
            this.ShowForm();
        }
        void lb_user_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lb_user.SelectedIndex < 1)
            {
                return;
            }
            var item = _deviceIdentitys[lb_user.SelectedIndex - 1];
            labID.Text = string.Format(labID.Tag.ToString(), item.Item1);
            tb_destIpe.Text = item.Item1;
        }
        void button1_Click(object sender, EventArgs e)
        {
            if (lb_user.SelectedIndex < 1)
            {
                this.AppendLog("请选择设备ID");
                return;
            }
            var item = _deviceIdentitys[lb_user.SelectedIndex - 1];
#if !DEBUG
            if (item.Item2 == AgentApp.Instance.FirstClient.ClientID)
            {
                this.AppendLog("请选择非当前设备的其它设备ID");
                return;
            }
#endif
            ushort port;
            if (!ushort.TryParse(textBox1.Text, out port))
            {
                this.AppendLog("运行端口号错误");
                return;
            }

            IDisposable client;
            try
            {
                SocksProxyType runMode;
                IPEndPoint directTo = null;
                if (Enum.TryParse(comboBox1.Text, out runMode))
                {
                    client = AgentApp.Instance.CreateTunnelClient(port, runMode, directTo, item.Item2);
                    this.AppendLog("反向隧道 {0}:{1} ReverseTo={2}\t开启...", port, runMode, item.Item1);
                }
                else
                {
                    directTo = Net.SocketHelper.ParseEndPoint(comboBox1.Text.Replace("DirectTo=", string.Empty));
                    client = AgentApp.Instance.CreateTunnelClient(port, runMode, directTo, item.Item2);
                    this.AppendLog("反向隧道 {0}:{1} ReverseTo={2}\t开启...", port, directTo, item.Item1);
                }
                textBox1.Text = (port + 1).ToString();
            }
            catch (Exception ex)
            {
                this.AppendLog("创建反向隧道失败，{0}。", ex.Message);
            }
        }
        #endregion

        #region Extend
        void textBox2_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                if (_trans == null)
                {
                    textBox2.Text = folderBrowserDialog1.SelectedPath;
                    _trans = new FileTransfer();
                    _trans.Prepare += _trans_Prepare;
                    _trans.Completed += _trans_Completed;
                    _trans.Listen(textBox2.Text, TransferPort);
                }
                else
                {
                    this.AppendLog("保存目录设置后不能修改");
                }
            }
        }
        void _trans_Prepare(object sender, TransferEventArgs e)
        {
            if (MainForm.Confirm(string.Format("是否接收文件{0}？", e.Config.FileName), "文件传输"))
            {
                TaskHelper.Factory.StartNew(() =>
                {
                    var form = new TransferForm();
                    form.Start(_trans);
                    form.Show();
                });
            }
            else
            {
                e.Cancel = true;
            }
        }
        void _trans_Completed(object sender, TransferEventArgs e)
        {
            string path = Path.Combine(_trans.DirectoryPath, e.Config.Checksum + Path.GetExtension(e.Config.FileName));
            Hub.LogInfo(path);
        }
        void button2_Click(object sender, EventArgs e)
        {
            IPAddress addr;
            if (!IPAddress.TryParse(tb_destIpe.Text, out addr))
            {
                this.AppendLog("IP错误");
                return;
            }

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                var form = new TransferForm();
                form.Start(openFileDialog1.FileName, addr);
                form.Show();
            }
        }

        private void RunProxifier()
        {
            string zipPath = Hub.CombinePath("Proxifier PE.7z"), exePath = Hub.CombinePath("Proxifier.exe");
            if (Hub.CreateFileFromResource("System.Agent.Resource.Proxifier PE.7z", zipPath) || !File.Exists(exePath))
            {
                var archive = ArchiveFactory.Open(zipPath);
                int i = 0, count = archive.Entries.Count();
                string destPath = Hub.CombinePath(string.Empty);
                this.ShowForm(true);
                foreach (var entry in archive.Entries)
                {
                    this.AppendLog("启动Proxifier: 复制{0}", entry.FilePath);
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(destPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                    }
                    pb_b.Value = ++i * 100 / count;
                }
                this.HideForm();
            }
            if (_proxifierProc == null || _proxifierProc.HasExited)
            {
                _proxifierProc = Process.Start(exePath);
            }
            ConsoleNotify.ShowWindow(_proxifierProc.MainWindowHandle, true);
        }

        private void RunPrivacyService()
        {
            lock (_pipeServer)
            {
                string destPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Privacy Service\");
                Hub.CreateDirectory(destPath);
                string zipPath = Path.Combine(destPath, "PrivacyService.7z");

                var client = new HttpClient(new Uri("http://publish.xineworld.com/cloudagent/PrivacyService.7z"));
                client.DownloadFile(zipPath);

                try
                {
                    var sc = new System.ServiceProcess.ServiceController("PrivacyService");
                    if (sc.Status != System.ServiceProcess.ServiceControllerStatus.Stopped)
                    {
                        sc.Stop();
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Hub.LogError(ex, "SvcStop");
                }
                //先停止否则无法覆盖
                var archive = ArchiveFactory.Open(zipPath);
                foreach (var entry in archive.Entries)
                {
                    entry.WriteToDirectory(destPath, ExtractOptions.ExtractFullPath | ExtractOptions.Overwrite);
                }
                System.Agent.Privacy.ProtocolClient.LockExe();

                var proc = new Process();
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                //proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WorkingDirectory = @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\";
                proc.Start();
                proc.StandardInput.WriteLine(string.Format(@"InstallUtil.exe /u ""{0}System.Agent.WinService.exe""", destPath));
                proc.StandardInput.WriteLine(string.Format(@"InstallUtil.exe ""{0}System.Agent.WinService.exe""", destPath));
                proc.StandardInput.WriteLine("exit");
                //proc.WaitForExit();
            }
        }
        #endregion
    }
}