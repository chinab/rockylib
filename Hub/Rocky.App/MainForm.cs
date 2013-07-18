using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Rocky.App
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            var win = new MonitorClient();
            win.Show();
        }

        private void 加密ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var win = new ConnectionStringForm();
            win.Show();
        }

        private void 编码EToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var win = new FileEncodeForm();
            win.Show();
        }
    }
}