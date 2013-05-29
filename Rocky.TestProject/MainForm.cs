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
using Rocky.Net;
using SharpCompress.Archive;
using SharpCompress.Common;

namespace Rocky.TestProject
{
    public partial class MainForm : Form
    {
        public static bool Confirm(string content, string title = "确认操作")
        {
            return MessageBox.Show(content, title, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK;
        }

        #region Fields
        internal const ushort TransferPort = 1079;
        private NamedPipeServerStream _pipeServer;
        private FileTransfer _trans;
        private Process _proxifierProc;
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

            this.Disposed += MainForm_Disposed;
            lb_user.SelectedIndexChanged += lb_user_SelectedIndexChanged;
            button1.Click += button1_Click;
            textBox2.Click += textBox2_Click;
            button2.Click += button2_Click;
        }

        void MainForm_Disposed(object sender, EventArgs e)
        {
            if (_proxifierProc != null && !_proxifierProc.HasExited)
            {
                _proxifierProc.Kill();
                _proxifierProc.Dispose();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.Cancel = !this.IsDisposed)
            {
                this.HideForm();
            }
            base.OnFormClosing(e);
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

                        break;
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
            _deviceIdentitys = CloudAgentApp.Instance.GetDeviceIdentity().ToArray();
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
            //if (item.Item2 == CloudAgentApp.Instance.FirstClient.ClientID)
            //{
            //    this.AppendLog("请选择非当前设备的其它设备ID");
            //    return;
            //}
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
                    client = CloudAgentApp.Instance.CreateTunnelClient(port, runMode, directTo, item.Item2);
                    this.AppendLog("反向隧道 {0}:{1} ReverseTo={2}\t开启...", port, runMode, item.Item1);
                }
                else
                {
                    directTo = Net.SocketHelper.ParseEndPoint(comboBox1.Text.Replace("DirectTo=", string.Empty));
                    client = CloudAgentApp.Instance.CreateTunnelClient(port, runMode, directTo, item.Item2);
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

        #region File
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
                //var form = new TransferForm();
                //form.Start(_trans);
                //form.Show();
            }
            else
            {
                e.Cancel = true;
            }
        }
        void _trans_Completed(object sender, TransferEventArgs e)
        {
            string path = Path.Combine(_trans.DirectoryPath, e.Config.Checksum + Path.GetExtension(e.Config.FileName));
            Runtime.LogInfo(path);
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
            string zipPath = Runtime.CombinePath("Proxifier PE.7z"), exePath = Runtime.CombinePath("Proxifier.exe");
            if (Runtime.CreateFileFromResource("Rocky.TestProject.Resource.Proxifier PE.7z", zipPath) || !File.Exists(exePath))
            {
                var archive = ArchiveFactory.Open(zipPath);
                int i = 0, count = archive.Entries.Count();
                string destPath = Runtime.CombinePath(string.Empty);
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
        #endregion
    }
}