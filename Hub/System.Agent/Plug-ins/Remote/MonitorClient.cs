using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Net;

namespace System.Agent.Remote
{
    public partial class MonitorClient : Form
    {
        #region Fields
        private volatile bool _initialized;
        #endregion

        #region Constructors
        public MonitorClient()
        {
            InitializeComponent();
            Application.Idle += Application_Idle;
        }
        void Application_Idle(object sender, EventArgs e)
        {
            this.checkBox1.Enabled = this.textBox1.Text.Trim().Length > 0;
            this.checkBox2.Enabled = this.checkBox1.Enabled;
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
            tip.SetToolTip(textBox1, "暂只支持局域网IP");
        }
        #endregion

        #region Methods
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                if (!_initialized)
                {
                    Cursor = Cursors.WaitCursor;
                    try
                    {
                        string sIpe = string.Format("{0}:{1}", textBox1.Text, MainForm.AssistPort);
                        var ipe = SocketHelper.ParseEndPoint(sIpe);
                        this.monitorUserControl1.Initialize(ipe);
                        _initialized = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Hub.LogError(ex, "MonitorClient");
                    }
                    Cursor = Cursors.Arrow;
                }
                TaskHelper.Factory.StartNew(() =>
                {
                    while (_initialized)
                    {
                        this.monitorUserControl1.UpdateDisplay();
                        Thread.Sleep(200);
                    }
                });
            }
            else
            {
                _initialized = false;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.monitorUserControl1.DoControl = ((CheckBox)sender).Checked;
        }
        #endregion
    }
}