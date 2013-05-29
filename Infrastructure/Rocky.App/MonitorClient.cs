using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Rocky.Net;

namespace Rocky.App
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
        #endregion

        #region 监控
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (((CheckBox)sender).Checked)
            {
                if (!_initialized)
                {
                    Cursor = Cursors.WaitCursor;
                    try
                    {
                        var ipe = SocketHelper.ParseEndPoint(this.textBox1.Text.Trim());
                        this.monitorUserControl1.Initialize(ipe);
                        _initialized = true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Runtime.LogError(ex, "MonitorClient");
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