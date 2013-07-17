namespace Rocky.App
{
    partial class FileEncodeForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonRun = new System.Windows.Forms.Button();
            this.buttonBrowser = new System.Windows.Forms.Button();
            this.cmbSourceEncode = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbTargetEncode = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.btnRemove = new System.Windows.Forms.Button();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.btnSelectFiles = new System.Windows.Forms.Button();
            this.listSelectedFiles = new System.Windows.Forms.ListBox();
            this.textBox_fileFilter = new System.Windows.Forms.TextBox();
            this.button_Clipboard = new System.Windows.Forms.Button();
            this.chkIsBackup = new System.Windows.Forms.CheckBox();
            this.chkUnknownEncoding = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // buttonRun
            // 
            this.buttonRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRun.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.buttonRun.Location = new System.Drawing.Point(474, 293);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(44, 23);
            this.buttonRun.TabIndex = 10;
            this.buttonRun.Text = "转换";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
            // 
            // buttonBrowser
            // 
            this.buttonBrowser.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowser.Location = new System.Drawing.Point(420, 51);
            this.buttonBrowser.Name = "buttonBrowser";
            this.buttonBrowser.Size = new System.Drawing.Size(96, 23);
            this.buttonBrowser.TabIndex = 9;
            this.buttonBrowser.Text = "按目录选文件";
            this.buttonBrowser.UseVisualStyleBackColor = true;
            this.buttonBrowser.Click += new System.EventHandler(this.buttonBrowser_Click);
            // 
            // cmbSourceEncode
            // 
            this.cmbSourceEncode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbSourceEncode.AutoCompleteCustomSource.AddRange(new string[] {
            "UTF-8",
            "UTF-7",
            "Unicode",
            "ASCII",
            "GB2312(简体中文)",
            "BIG5 (繁体中文)"});
            this.cmbSourceEncode.FormattingEnabled = true;
            this.cmbSourceEncode.Items.AddRange(new object[] {
            "UTF-8",
            "UTF-7",
            "Unicode",
            "ASCII",
            "GB2312(简体中文)",
            "BIG5 (繁体中文)"});
            this.cmbSourceEncode.Location = new System.Drawing.Point(420, 217);
            this.cmbSourceEncode.Name = "cmbSourceEncode";
            this.cmbSourceEncode.Size = new System.Drawing.Size(96, 20);
            this.cmbSourceEncode.TabIndex = 11;
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(419, 200);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 12);
            this.label1.TabIndex = 12;
            this.label1.Text = "原文件编码";
            // 
            // cmbTargetEncode
            // 
            this.cmbTargetEncode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbTargetEncode.AutoCompleteCustomSource.AddRange(new string[] {
            "UTF-8",
            "UTF-7",
            "Unicode",
            "ASCII",
            "GB2312(简体中文)",
            "BIG5 (繁体中文)"});
            this.cmbTargetEncode.FormattingEnabled = true;
            this.cmbTargetEncode.Items.AddRange(new object[] {
            "UTF-8",
            "UTF-7",
            "Unicode",
            "ASCII",
            "GB2312(简体中文)",
            "BIG5 (繁体中文)"});
            this.cmbTargetEncode.Location = new System.Drawing.Point(420, 264);
            this.cmbTargetEncode.Name = "cmbTargetEncode";
            this.cmbTargetEncode.Size = new System.Drawing.Size(97, 20);
            this.cmbTargetEncode.TabIndex = 11;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(419, 247);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(65, 12);
            this.label2.TabIndex = 12;
            this.label2.Text = "转换后编码";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 12);
            this.label3.TabIndex = 12;
            this.label3.Text = "文件过滤字符串";
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRemove.Location = new System.Drawing.Point(421, 141);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(96, 23);
            this.btnRemove.TabIndex = 17;
            this.btnRemove.Text = "从列表中移除";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // txtResult
            // 
            this.txtResult.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtResult.Location = new System.Drawing.Point(15, 322);
            this.txtResult.Multiline = true;
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.txtResult.Size = new System.Drawing.Size(502, 88);
            this.txtResult.TabIndex = 16;
            this.txtResult.WordWrap = false;
            // 
            // btnSelectFiles
            // 
            this.btnSelectFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSelectFiles.Location = new System.Drawing.Point(421, 80);
            this.btnSelectFiles.Name = "btnSelectFiles";
            this.btnSelectFiles.Size = new System.Drawing.Size(96, 23);
            this.btnSelectFiles.TabIndex = 15;
            this.btnSelectFiles.Text = "多选文件";
            this.btnSelectFiles.UseVisualStyleBackColor = true;
            this.btnSelectFiles.Click += new System.EventHandler(this.btnSelectFiles_Click);
            // 
            // listSelectedFiles
            // 
            this.listSelectedFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listSelectedFiles.FormattingEnabled = true;
            this.listSelectedFiles.HorizontalScrollbar = true;
            this.listSelectedFiles.ItemHeight = 12;
            this.listSelectedFiles.Location = new System.Drawing.Point(15, 48);
            this.listSelectedFiles.Name = "listSelectedFiles";
            this.listSelectedFiles.Size = new System.Drawing.Size(388, 268);
            this.listSelectedFiles.TabIndex = 13;
            // 
            // textBox_fileFilter
            // 
            this.textBox_fileFilter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_fileFilter.Location = new System.Drawing.Point(107, 7);
            this.textBox_fileFilter.Name = "textBox_fileFilter";
            this.textBox_fileFilter.Size = new System.Drawing.Size(409, 21);
            this.textBox_fileFilter.TabIndex = 18;
            this.textBox_fileFilter.Text = "htm|*.htm|html|*.html|aspx|*.aspx|Master|*.Master|ascx|*.ascx|js|*.js|css|*.css";
            // 
            // button_Clipboard
            // 
            this.button_Clipboard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_Clipboard.Location = new System.Drawing.Point(420, 109);
            this.button_Clipboard.Name = "button_Clipboard";
            this.button_Clipboard.Size = new System.Drawing.Size(96, 23);
            this.button_Clipboard.TabIndex = 15;
            this.button_Clipboard.Text = "剪贴板中复制";
            this.button_Clipboard.UseVisualStyleBackColor = true;
            this.button_Clipboard.Click += new System.EventHandler(this.button_Clipboard_Click);
            // 
            // chkIsBackup
            // 
            this.chkIsBackup.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkIsBackup.AutoSize = true;
            this.chkIsBackup.Checked = true;
            this.chkIsBackup.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkIsBackup.Location = new System.Drawing.Point(421, 297);
            this.chkIsBackup.Name = "chkIsBackup";
            this.chkIsBackup.Size = new System.Drawing.Size(48, 16);
            this.chkIsBackup.TabIndex = 20;
            this.chkIsBackup.Text = "备份";
            this.chkIsBackup.UseVisualStyleBackColor = true;
            // 
            // chkUnknownEncoding
            // 
            this.chkUnknownEncoding.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkUnknownEncoding.AutoSize = true;
            this.chkUnknownEncoding.Location = new System.Drawing.Point(420, 178);
            this.chkUnknownEncoding.Name = "chkUnknownEncoding";
            this.chkUnknownEncoding.Size = new System.Drawing.Size(108, 16);
            this.chkUnknownEncoding.TabIndex = 19;
            this.chkUnknownEncoding.Text = "自动识别原编码";
            this.chkUnknownEncoding.UseVisualStyleBackColor = true;
            this.chkUnknownEncoding.CheckedChanged += new System.EventHandler(this.chkUnknownEncoding_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(105, 31);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(383, 12);
            this.label4.TabIndex = 21;
            this.label4.Text = "（坚线分隔，奇数为类型描述，偶数为扩展名。空值或*.*为全部文件）";
            // 
            // FormFileEncode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 418);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.chkIsBackup);
            this.Controls.Add(this.chkUnknownEncoding);
            this.Controls.Add(this.textBox_fileFilter);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.txtResult);
            this.Controls.Add(this.button_Clipboard);
            this.Controls.Add(this.btnSelectFiles);
            this.Controls.Add(this.listSelectedFiles);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.cmbTargetEncode);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbSourceEncode);
            this.Controls.Add(this.buttonRun);
            this.Controls.Add(this.buttonBrowser);
            this.Name = "FormFileEncode";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "文件编码转换";
            this.Load += new System.EventHandler(this.FileEncodeForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.Button buttonBrowser;
        private System.Windows.Forms.ComboBox cmbSourceEncode;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbTargetEncode;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.TextBox txtResult;
        private System.Windows.Forms.Button btnSelectFiles;
        private System.Windows.Forms.ListBox listSelectedFiles;
        private System.Windows.Forms.TextBox textBox_fileFilter;
        private System.Windows.Forms.Button button_Clipboard;
        private System.Windows.Forms.CheckBox chkIsBackup;
        private System.Windows.Forms.CheckBox chkUnknownEncoding;
        private System.Windows.Forms.Label label4;
    }
}