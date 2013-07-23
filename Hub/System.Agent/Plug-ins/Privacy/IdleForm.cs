using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace System.Agent.Privacy
{
    public partial class IdleForm : Form
    {
        #region IdleFinder
        [StructLayout(LayoutKind.Sequential)]
        public struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint)Marshal.SizeOf(lastInPut);
            if (!GetLastInputInfo(ref lastInPut))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return TimeSpan.FromMilliseconds(Environment.TickCount - lastInPut.dwTime);
        }
        #endregion

        private LockScreen _locker;

        public IdleForm()
        {
            InitializeComponent();
        }

        private void IdleForm_Load(object sender, EventArgs e)
        {
            new JobTimer(state =>
           {
               var idle = GetIdleTime();
               label2.Text = string.Format("{0}秒", idle.TotalSeconds);
               if (idle.TotalSeconds >= AgentHubConfig.AppConfig.IdleSeconds)
               {
                   this.Invoke(new Action(this.ShowLock));
               }
           }, TimeSpan.FromSeconds(1)).Start();
        }

        private void ShowLock()
        {
            if (_locker == null)
            {
                _locker = new LockScreen();
            }
            if (_locker.ShowDialog() != DialogResult.OK)
            {
                ShowLock();
            }
        }
    }
}