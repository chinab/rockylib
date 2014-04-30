using System;
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
using System.Configuration;

namespace System.Agent
{
    public class ConsoleNotify
    {
        #region WinAPI
        /// <summary>
        /// 必须调用此API
        /// </summary>
        /// <returns></returns>
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("User32.dll", EntryPoint = "GetSystemMenu")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("User32.dll", EntryPoint = "RemoveMenu")]
        private static extern IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);
        [DllImport("Kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        public delegate bool HandlerRoutine(CtrlTypes dwCtrlType);
        /// <summary>
        /// An enumerated type for the control messages sent to the handler routine.
        /// </summary>
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
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
                int SW_SHOW = _visible ? 5 : 0;
                ShowWindow(WindowHandle, SW_SHOW);
            }
        }
        internal static IntPtr WindowHandle
        {
            get
            {
                Contract.Ensures(Contract.Result<IntPtr>() != IntPtr.Zero);

                //var proc = Process.GetCurrentProcess();
                //return proc.MainWindowHandle;
                return GetConsoleWindow();
            }
        }
        internal static bool Closing { get; private set; }

        /// <summary>
        /// 禁用关闭按钮
        /// </summary>        
        private static void DisableCloseButton()
        {
            IntPtr closeMenu = GetSystemMenu(WindowHandle, false);
            uint SC_CLOSE = 0xF060;
            RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        }

        public static void ShowWindow(IntPtr windowName, bool visible)
        {
            ShowWindow(windowName, visible ? 9 : 0);
        }

        public static string GetExecPath()
        {
            string shortcutPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), string.Format(@"{0}\{1}.appref-ms", Application.CompanyName, Application.ProductName));
            Console.Out.WriteLine(shortcutPath);
            return shortcutPath;
        }

        /// <summary>
        /// 获取客户端发布版本号
        /// </summary>
        /// <returns>当前版本号</returns>
        public static string GetVersion()
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
                var proc = Process.GetCurrentProcess();
                var q = from t in Process.GetProcessesByName(proc.ProcessName)
                        where t.Id != proc.Id
                        select t.MainWindowHandle;
                if (q.Any())
                {
                    ShowWindow(q.First(), 5);
                }
                else
                {
                    Console.Out.WriteError("控制台已启动，按任意键退出。");
                    Console.Read();
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
                    //startInfo.FileName = GetExecPath();
                    startInfo.FileName = Application.ExecutablePath;
                    startInfo.Verb = "runas";
                    try
                    {
                        Process.Start(startInfo);
                        Environment.Exit(0);
                    }
                    catch (Exception ex)
                    {
                        Hub.LogError(ex, "administrativeMode");
                    }
                }
            }

            var stream = Hub.GetResourceStream(string.Format("{0}.favicon.ico", typeof(ConsoleNotify).Namespace));
            if (stream == null)
            {
                throw new InvalidOperationException("未嵌入NotifyIcon源");
            }
            _notify = new NotifyIcon()
            {
                Visible = _visible,
                Icon = new Icon(stream),
                Text = notifyText,
                ContextMenuStrip = new ContextMenuStrip() { RenderMode = ToolStripRenderMode.System }
            };
            _notify.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Visible = !Visible;
                }
            };
            this.InitMenu();
        }
        #endregion

        #region InitMenu
        private void InitMenu()
        {
            this.CreateMenuItem("配置", "C");
            this.CreateMenuItem("日志", "L");
            this.CreateMenuItem("我的设备", "D");
            this.CreateMenuItem("启动Proxifier", "P");
            this.CreateMenuItem("安装隐私服务", "PS");
            this.CreateMenuItem("帮助", "H", true);
            this.CreateMenuItem("开机启动/禁止", "S");
            this.CreateMenuItem("显示/隐藏", "V");
            this.CreateMenuItem("重新载入", "R");
            this.CreateMenuItem("退出", "E");
            _notify.ContextMenuStrip.ItemClicked += new ToolStripItemClickedEventHandler(ContextMenuStrip_ItemClicked);
        }
        void ContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            switch (e.ClickedItem.Name)
            {
                case "C":
                    Process.Start("Explorer.exe", "/select," + AgentHubConfig.AppConfigPath);
                    break;
                case "L":
                    Process.Start("Explorer.exe", Hub.CombinePath(@"logs\"));
                    break;
                case "D":
                    using (var pipeClient = new NamedPipeClientStream(SecurityPolicy.PipeName))
                    {
                        pipeClient.Connect();
                        pipeClient.WriteByte(0);
                        pipeClient.WaitForPipeDrain();
                    }
                    break;
                case "P":
                    using (var pipeClient = new NamedPipeClientStream(SecurityPolicy.PipeName))
                    {
                        pipeClient.Connect();
                        pipeClient.WriteByte(1);
                        pipeClient.WaitForPipeDrain();
                    }
                    break;
                case "PS":
                    using (var pipeClient = new NamedPipeClientStream(SecurityPolicy.PipeName))
                    {
                        pipeClient.Connect();
                        pipeClient.WriteByte(2);
                        pipeClient.WaitForPipeDrain();
                    }
                    break;
                case "H":
                    Process.Start("http://www.cnblogs.com/Googler/archive/2013/05/30/3109402.html");
                    break;
                case "S":
                    var reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (reg.GetValue(_notify.Text) == null)
                    {
                        reg.SetValue(_notify.Text, GetExecPath());
                        Console.Out.WriteTip("开机启动 Ok.");
                    }
                    else
                    {
                        reg.DeleteValue(_notify.Text, false);
                        Console.Out.WriteTip("开机禁止 Ok.");
                    }
                    break;
                case "V":
                    Visible = !Visible;
                    break;
                case "R":
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
        public void Run(IHubEntry entry, Form form = null)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var setter = config.AppSettings.Settings["ShowTip"];
            bool showTip;
            if (setter != null && bool.TryParse(setter.Value, out showTip) && showTip)
            {
                _notify.ShowBalloonTip(3000, _notify.Text, string.Format("{0}已启动，单击托盘图标可以最小化！", _notify.Text), ToolTipIcon.Info);
                setter.Value = bool.FalseString;
                config.Save();
            }

            DisableCloseButton();
            SetConsoleCtrlHandler(_handler = new HandlerRoutine(eventType =>
            {
                Visible = false;
                Closing = true;
                if (form != null)
                {
                    form.Close();
                }
                _notify.Dispose();
                return false;
            }), true);

            _tokenSource = new CancellationTokenSource();
            TaskHelper.Factory.StartNew(() =>
            {
                entry.Main(null);
                this.Exit();
            }, _tokenSource.Token).ObservedException();
            if (form == null)
            {
                Application.Run();
            }
            else
            {
                Application.EnableVisualStyles();
                Application.Run(form);
            }
        }

        public void Exit()
        {
            _tokenSource.Cancel(true);
            _handler(CtrlTypes.CTRL_C_EVENT);
            Application.Exit();
            Environment.Exit(0);
        }

        private void CreateMenuItem(string txt, string name, bool doSeparator = false)
        {
            var menuItem = new ToolStripMenuItem();
            menuItem.Name = name;
            menuItem.Text = txt;
            _notify.ContextMenuStrip.Items.Add(menuItem);
            if (doSeparator)
            {
                _notify.ContextMenuStrip.Items.Add(new ToolStripSeparator());
            }
        }
        #endregion
    }
}