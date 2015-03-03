namespace System.Net
{
    partial class MonitorUserControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // MonitorUserControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Name = "MonitorUserControl";
            this.Size = new System.Drawing.Size(500, 500);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MonitorUserControl_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MonitorUserControl_MouseMove);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.MonitorUserControl_KeyUp);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MonitorUserControl_Paint);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MonitorUserControl_MouseUp);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MonitorUserControl_KeyDown);
            this.ResumeLayout(false);

        }

        #endregion
    }
}