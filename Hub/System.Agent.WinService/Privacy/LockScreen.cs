using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
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
        private bool _canClose;
        private Hook _hook;
        private JobTimer _job;
        private volatile ushort _errorCount;

        public LockScreen()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
            base.Opacity = PrivacyService.Config.Opacity / 100;
            base.BackgroundImage = PrivacyService.Config.Background;
            this.Show();
            _hook = new Hook();
            _hook.KeyDown += _hook_KeyDown;
            _hook.Install();
            _job = new JobTimer(this.Check, TimeSpan.FromMilliseconds(40));
            _job.Start();
        }
        void _hook_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers.HasFlag(Keys.Delete) || e.KeyData == Keys.Delete)
            {
                _errorCount += 2;
            }
        }
        private void Check(object state)
        {
            if (_errorCount >= 2)
            {
                this.Location = new Point(8, 8);
                PrivacyService.FormatDrive(this.Handle);
                return;
            }

            var q = from t in Process.GetProcesses()
                    where t.ProcessName.Equals("taskmgr", StringComparison.OrdinalIgnoreCase)
                    select t;
            var p1 = q.SingleOrDefault();
            if (p1 != null)
            {
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = !_canClose;
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _job.Stop();
            _hook.Uninstall();
            base.OnFormClosed(e);
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
                if (textBox1.Text != PrivacyService.Config.Password)
                {
                    textBox1.Text = string.Empty;
                    _errorCount++;
                    return;
                }

                _canClose = true;
                this.Close();
            }
            finally
            {
                Thread.Sleep(1000);
                button1.Enabled = true;
            }
        }
    }
}