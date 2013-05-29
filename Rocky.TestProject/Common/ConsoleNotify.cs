﻿using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Rocky.TestProject
{
    public class ConsoleNotify
    {
        #region Win32API
        [DllImport("kernel32")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("kernel32")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        private static extern IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        public delegate bool HandlerRoutine(CtrlTypes dwCtrlType);
        /// <summary>
        /// An enumerated type for the control messages sent to the handler routine.
        /// </summary>
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0, // From wincom.h
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }
        #endregion

        #region Static
        private static volatile bool _visible = true;

        public static bool Visible
        {
            get { return _visible; }
            set
            {
                _visible = value;
                uint SW_SHOW;
                if (_visible)
                {
                    SW_SHOW = 5;
                }
                else
                {
                    SW_SHOW = 0;
                }
                ShowWindow(GetConsoleHandle(), SW_SHOW);
            }
        }

        /// <summary>
        /// 禁用关闭按钮
        /// </summary>        
        public static void DisableCloseButton()
        {
            IntPtr closeMenu = GetSystemMenu(GetConsoleHandle(), IntPtr.Zero);
            uint SC_CLOSE = 0xF060;
            RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        }

        /// <summary>
        /// 自动寻找当前控制台句柄
        /// </summary>        
        private static IntPtr GetConsoleHandle()
        {
            Contract.Ensures(Contract.Result<IntPtr>() != IntPtr.Zero);

            return GetConsoleWindow();
        }

        /// <summary>
        /// 获取客户端发布版本号
        /// </summary>
        /// <returns>当前版本号</returns>
        public static string GetVersiion()
        {
            var version = string.Empty;
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                return version;
            }
            var ad = ApplicationDeployment.CurrentDeployment;
            UpdateCheckInfo info = null;
            try
            {
                info = ad.CheckForDetailedUpdate();
            }
            catch (DeploymentDownloadException dde)
            {
                Console.Out.WriteWarning("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                return version;
            }
            catch (InvalidDeploymentException ide)
            {
                Console.Out.WriteWarning("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                return version;
            }
            catch (InvalidOperationException ioe)
            {
                Console.Out.WriteWarning("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                return version;
            }

            if (info.UpdateAvailable)
            {
                if (info.IsUpdateRequired)
                {
                    // Display a message that the app MUST reboot. Display the minimum required version.
                    Console.Out.WriteWarning("This application has detected a mandatory update from your current " +
                        "version to version " + info.MinimumRequiredVersion.ToString() +
                        ". The application will now install the update and restart.");
                }
                try
                {
                    ad.Update();
                    Console.Out.WriteWarning("The application has been upgraded, and will now restart.");
                    Application.Restart();
                }
                catch (DeploymentDownloadException dde)
                {
                    Console.Out.WriteWarning("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
                    return version;
                }
            }
            var ver = ad.CurrentVersion;
            version = string.Format("v{0}.{1}.{2}.{3}", ver.Major, ver.Minor, ver.Build, ver.Revision);
            return version;
        }

        public static void ShowWindow(IntPtr lpWindowName, bool visible)
        {
            ShowWindow(lpWindowName, visible ? (uint)9 : 0);
        }
        #endregion

        #region Fields
        private NotifyIcon _notify;
        private HandlerRoutine _handler;
        private CancellationTokenSource _tokenSource;
        #endregion

        #region Constructors
        public ConsoleNotify(string notifyText, bool createNew, bool administrative = false)
        {
            if (!createNew)
            {
                IntPtr handle = FindWindow(null, Console.Title);
                if (handle == IntPtr.Zero)
                {
                    Console.Out.WriteError("TunnelClient已启动，按任意键退出。");
                    Console.Read();
                }
                else
                {
                    ShowWindow(handle, 5);
                }
                Environment.Exit(0);
            }
            if (administrative)
            {
                var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                bool administrativeMode = principal.IsInRole(WindowsBuiltInRole.Administrator);
                if (!administrativeMode)
                {
                    var startInfo = new ProcessStartInfo();
                    startInfo.FileName = Application.ExecutablePath;
                    startInfo.Verb = "runas";
                    try
                    {
                        Process.Start(startInfo);
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Runtime.LogError(ex, "administrativeMode");
                    }
                }
            }

            _notify = new NotifyIcon()
            {
                Visible = _visible,
                Icon = new Icon(Runtime.GetResourceStream("Rocky.TestProject.favicon.ico")),
                Text = notifyText,
                ContextMenuStrip = new ContextMenuStrip() { RenderMode = ToolStripRenderMode.System }
            };
            _notify.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ConsoleNotify.Visible = !ConsoleNotify.Visible;
                }
            };
            this.InitMenu();
        }
        #endregion

        #region InitMenu
        private void InitMenu()
        {
            ushort i = 0;
            Action<string> act = txt =>
            {
                var menuItem = new ToolStripMenuItem();
                menuItem.Name = "Item" + (i++);
                menuItem.Text = txt;
                _notify.ContextMenuStrip.Items.Add(menuItem);
            };
            act("配置");
            act("日志");
            act("我的设备");
            act("启动Proxifier");
            act("开机启动/禁止");
            act("显示/隐藏");
            act("重新载入");
            act("退出");
            _notify.ContextMenuStrip.ItemClicked += new ToolStripItemClickedEventHandler(ContextMenuStrip_ItemClicked);
        }
        void ContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Name)
            {
                case "Item0":
                    Process.Start("Explorer.exe", "/select," + CloudAgentConfig.AppConfigPath);
                    break;
                case "Item1":
                    Process.Start("Explorer.exe", Runtime.CombinePath(@"logs\"));
                    break;
                case "Item2":
                    using (var pipeClient = new NamedPipeClientStream(SecurityPolicy.PipeName))
                    {
                        pipeClient.Connect();
                        pipeClient.WriteByte(0);
                        pipeClient.WaitForPipeDrain();
                    }
                    break;
                case "Item3":
                    using (var pipeClient = new NamedPipeClientStream(SecurityPolicy.PipeName))
                    {
                        pipeClient.Connect();
                        pipeClient.WriteByte(1);
                        pipeClient.WaitForPipeDrain();
                    }
                    break;
                case "Item4":
                    string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), string.Format(@"{0}\{1}.appref-ms", Application.CompanyName, Application.ProductName));
                    Console.Out.WriteInfo(shortcutPath);
                    var reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (reg.GetValue(_notify.Text) == null)
                    {
                        reg.SetValue(_notify.Text, shortcutPath);
                        Console.Out.WriteTip("开机启动 Ok.");
                    }
                    else
                    {
                        reg.DeleteValue(_notify.Text, false);
                        Console.Out.WriteTip("开机禁止 Ok.");
                    }
                    break;
                case "Item5":
                    ConsoleNotify.Visible = !ConsoleNotify.Visible;
                    break;
                case "Item6":
                    Application.Restart();
                    this.Exit();
                    break;
                default:
                    this.Exit();
                    break;
            }
        }
        #endregion

        #region Methods
        public void Run(IRawEntry entry, Form entryForm = null)
        {
            //ConsoleNotify.DisableCloseButton();
            _notify.ShowBalloonTip(3000, _notify.Text, string.Format("{0}已启动，单击托盘图标可以最小化！", _notify.Text), ToolTipIcon.Info);
            _handler = new HandlerRoutine(eventType =>
            {
                ConsoleNotify.Visible = false;
                _notify.Dispose();
                if (entryForm != null)
                {
                    entryForm.Dispose();
                }
                return false;
            });
            SetConsoleCtrlHandler(_handler, true);

            _tokenSource = new CancellationTokenSource();
            var task = TaskHelper.Factory.StartNew((state) =>
            {
                entry.Main(state);
            }, null, _tokenSource.Token).ObservedException().ContinueWith(t => this.Exit());
            if (entryForm == null)
            {
                Application.Run();
            }
            else
            {
                Application.EnableVisualStyles();
                Application.Run(entryForm);
            }
            _handler(CtrlTypes.CTRL_C_EVENT);
        }

        public void Exit()
        {
            _tokenSource.Cancel(true);
            _handler(CtrlTypes.CTRL_C_EVENT);
            Application.ExitThread();
        }
        #endregion
    }
}