using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Rocky;
using Rocky.Data;

namespace Rocky.App
{
    public partial class ConnectionStringForm : Form
    {
        private ConnectionStringBuilder builder;

        public ConnectionStringForm()
        {
            InitializeComponent();
        }

        private void ConnectionStringForm_Load(object sender, EventArgs e)
        {
            builder = new ConnectionStringBuilder();
        }

        private void textBox1_Enter(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                textBox1.Text = Clipboard.GetText();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            builder.ConnectionString = textBox1.Text;
            builder.CryptoKeys = new string[] { CryptoManaged.MD5Hash("Rocky.TBox"), CryptoManaged.MD5Hash(CryptoManaged.NewSalt) };
            textBox2.Text = builder.ToString();
        }
    }
}