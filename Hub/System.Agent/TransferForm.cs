using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace System.Agent
{
    public partial class TransferForm : Form
    {
        private bool _isSend;
        private FileTransfer _trans;

        public TransferForm()
        {
            InitializeComponent();
        }

        public void Start(string filePath, IPAddress addr)
        {
            _isSend = true;
            var config = new TransferConfig(filePath);
            labName.Text = config.FileName;
            labChecksum.Text = config.Checksum.ToString();
            labprog.Text = string.Format("0KB/s 0/{0}", config.FileLength);
            var remoteIpe = new IPEndPoint(addr, MainForm.TransferPort);
            _trans = new FileTransfer();
            _trans.ProgressChanged += trans_ProgressChanged;
            _trans.Completed += trans_Completed;
            TaskHelper.Factory.StartNew(() => _trans.Send(config, remoteIpe));
        }
        public void Start(FileTransfer trans)
        {
            _isSend = false;
            _trans = trans;
            _trans.ProgressChanged += trans_ProgressChanged;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isSend)
            {
                e.Cancel = !MainForm.Confirm("确认取消传送吗？");
            }
            else
            {
                _trans.ProgressChanged -= trans_ProgressChanged;
            }
            base.OnFormClosing(e);
        }

        void trans_ProgressChanged(object sender, TransferEventArgs e)
        {
            var trans = (FileTransfer)sender;
            progressBar1.Value = e.Progress.ProgressPercentage;
            labprog.Text = string.Format("{0}KB/s {1}/{2}#{3}",
                e.Progress.GetSpeed(), e.Progress.BytesTransferred, e.Progress.ContentLength, e.Config.ChunkCount);
        }
        void trans_Completed(object sender, TransferEventArgs e)
        {
            Thread.Sleep(2000);
            this.Close();
        }
    }
}