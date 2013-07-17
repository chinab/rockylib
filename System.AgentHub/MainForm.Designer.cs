namespace System.AgentHub
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.tb_msg = new System.Windows.Forms.TextBox();
            this.lb_user = new System.Windows.Forms.ListBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.tb_destIpe = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.labRunPort = new System.Windows.Forms.Label();
            this.labRunMode = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.button2 = new System.Windows.Forms.Button();
            this.labID = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.pb_b = new System.Windows.Forms.ToolStripProgressBar();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tb_msg
            // 
            this.tb_msg.Location = new System.Drawing.Point(140, 250);
            this.tb_msg.Multiline = true;
            this.tb_msg.Name = "tb_msg";
            this.tb_msg.ReadOnly = true;
            this.tb_msg.Size = new System.Drawing.Size(432, 126);
            this.tb_msg.TabIndex = 0;
            this.tb_msg.TabStop = false;
            // 
            // lb_user
            // 
            this.lb_user.FormattingEnabled = true;
            this.lb_user.ItemHeight = 12;
            this.lb_user.Location = new System.Drawing.Point(14, 12);
            this.lb_user.Name = "lb_user";
            this.lb_user.Size = new System.Drawing.Size(120, 364);
            this.lb_user.TabIndex = 2;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.tb_destIpe);
            this.groupBox1.Controls.Add(this.textBox2);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.textBox1);
            this.groupBox1.Controls.Add(this.labRunPort);
            this.groupBox1.Controls.Add(this.labRunMode);
            this.groupBox1.Controls.Add(this.comboBox1);
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.labID);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Location = new System.Drawing.Point(141, 13);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(431, 231);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "操作";
            // 
            // tb_destIpe
            // 
            this.tb_destIpe.Location = new System.Drawing.Point(80, 194);
            this.tb_destIpe.Name = "tb_destIpe";
            this.tb_destIpe.Size = new System.Drawing.Size(100, 21);
            this.tb_destIpe.TabIndex = 10;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(80, 163);
            this.textBox2.Name = "textBox2";
            this.textBox2.ReadOnly = true;
            this.textBox2.Size = new System.Drawing.Size(190, 21);
            this.textBox2.TabIndex = 9;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 197);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 8;
            this.label2.Text = "远程地址: ";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 166);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 12);
            this.label1.TabIndex = 7;
            this.label1.Text = "保存目录: ";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(80, 45);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 21);
            this.textBox1.TabIndex = 6;
            this.textBox1.Text = "1090";
            // 
            // labRunPort
            // 
            this.labRunPort.AutoSize = true;
            this.labRunPort.Location = new System.Drawing.Point(9, 48);
            this.labRunPort.Name = "labRunPort";
            this.labRunPort.Size = new System.Drawing.Size(65, 12);
            this.labRunPort.TabIndex = 5;
            this.labRunPort.Text = "运行端口: ";
            // 
            // labRunMode
            // 
            this.labRunMode.AutoSize = true;
            this.labRunMode.Location = new System.Drawing.Point(9, 85);
            this.labRunMode.Name = "labRunMode";
            this.labRunMode.Size = new System.Drawing.Size(65, 12);
            this.labRunMode.TabIndex = 4;
            this.labRunMode.Text = "运行模式: ";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Items.AddRange(new object[] {
            "Socks4",
            "Socks5",
            "Http",
            "DirectTo=-请填写-"});
            this.comboBox1.Location = new System.Drawing.Point(80, 82);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(150, 20);
            this.comboBox1.TabIndex = 3;
            this.comboBox1.Text = "-请选择-";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(195, 192);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "文件传输";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // labID
            // 
            this.labID.AutoSize = true;
            this.labID.Location = new System.Drawing.Point(21, 21);
            this.labID.Name = "labID";
            this.labID.Size = new System.Drawing.Size(101, 12);
            this.labID.TabIndex = 1;
            this.labID.Tag = "设备ID: {0}";
            this.labID.Text = "设备ID: -请选择-";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(174, 117);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "创建隧道";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.pb_b});
            this.statusStrip1.Location = new System.Drawing.Point(0, 390);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(584, 22);
            this.statusStrip1.TabIndex = 4;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(32, 17);
            this.toolStripStatusLabel1.Text = "就绪";
            // 
            // pb_b
            // 
            this.pb_b.Name = "pb_b";
            this.pb_b.Size = new System.Drawing.Size(100, 16);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            this.openFileDialog1.Title = "请选择一个文件";
            // 
            // folderBrowserDialog1
            // 
            this.folderBrowserDialog1.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 412);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.lb_user);
            this.Controls.Add(this.tb_msg);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AgentHub";
            this.WindowState = System.Windows.Forms.FormWindowState.Minimized;
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tb_msg;
        private System.Windows.Forms.ListBox lb_user;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label labID;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label labRunMode;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripProgressBar pb_b;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.Label labRunPort;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox tb_destIpe;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    }
}