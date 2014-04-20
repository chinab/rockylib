using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace System.AgentHub
{
    public partial class LockForm : Form
    {
        public LockForm()
        {
            InitializeComponent();
        }

        private Point loc = Point.Empty;
        private Form1 f1 = new Form1();

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (this.passwork.Text.Equals("") && this.passwork2.Text.Equals(""))
            {
                this.label5.Visible = true;
                this.label5.Text = "密码不能为空。";
            }
            else if (this.passwork.Text != this.passwork2.Text)
            {
                this.toolTip1.Show("两次设置的密码不一样。", this.passwork, 600);
                this.label5.Visible = false;
            }
            else
            {
                base.Visible = false;
                if (this.cbbox.SelectedItem.ToString().Equals("100%"))
                {
                    this.f1.getinfo(this.passwork.Text, 10.0);
                }
                else
                {
                    this.f1.getinfo(this.passwork.Text, double.Parse(this.cbbox.SelectedItem.ToString().Remove(1)));
                }
                this.f1.Show();
                this.Hide();
            }
        }

        private void passwork2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                this.btnOK_Click(null, null);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            this.cbbox.SelectedItem = "80%";
        }
    }
}