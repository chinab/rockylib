namespace Rocky.TestProject
{
    partial class TransferForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TransferForm));
            this.labName = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.labprog = new System.Windows.Forms.Label();
            this.labChecksum = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labName
            // 
            this.labName.AutoSize = true;
            this.labName.Location = new System.Drawing.Point(12, 9);
            this.labName.Name = "labName";
            this.labName.Size = new System.Drawing.Size(65, 12);
            this.labName.TabIndex = 0;
            this.labName.Text = "文件名称: ";
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(14, 65);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(360, 23);
            this.progressBar1.TabIndex = 1;
            // 
            // labprog
            // 
            this.labprog.AutoSize = true;
            this.labprog.Location = new System.Drawing.Point(183, 101);
            this.labprog.Name = "labprog";
            this.labprog.Size = new System.Drawing.Size(11, 12);
            this.labprog.TabIndex = 2;
            this.labprog.Text = "/";
            // 
            // labChecksum
            // 
            this.labChecksum.AutoSize = true;
            this.labChecksum.Location = new System.Drawing.Point(19, 35);
            this.labChecksum.Name = "labChecksum";
            this.labChecksum.Size = new System.Drawing.Size(53, 12);
            this.labChecksum.TabIndex = 3;
            this.labChecksum.Text = "MD5校验:";
            // 
            // TransferForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 162);
            this.Controls.Add(this.labChecksum);
            this.Controls.Add(this.labprog);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.labName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "TransferForm";
            this.Text = "文件传输";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labName;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label labprog;
        private System.Windows.Forms.Label labChecksum;

    }
}