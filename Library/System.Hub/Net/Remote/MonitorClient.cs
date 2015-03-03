using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace System.Net
{
    public partial class MonitorClient : Form
    {
        #region Fields
        private volatile bool _initialized;
        private System.Threading.Tasks.Task _task;
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
            tip.SetToolTip(textBox1, "LAN support only");
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
                        var ipe = SocketHelper.ParseEndPoint(textBox1.Text);
                        this.monitorUserControl1.Initialize(ipe);
                        _initialized = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        App.LogError(ex, "MonitorClient");
                    }
                    Cursor = Cursors.Arrow;
                }
                if (_task == null || _task.IsCompleted)
                {
                    _task = TaskHelper.Factory.StartNew(() =>
                    {
                        while (_initialized)
                        {
                            this.monitorUserControl1.UpdateDisplay();
                            Thread.Sleep(200);
                        }
                    });
                }
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