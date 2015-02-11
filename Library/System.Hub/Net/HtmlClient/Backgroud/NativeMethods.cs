using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
    public static class WinInetInterop
    {
        #region Fields
        /// <summary>
        /// This name is used as the user agent in the HTTP protocol.
        /// </summary>
        private const string ApplicationName = "Chrome/33.0.1750.154";
        private static int ProxyLocker;
        #endregion

        #region PInvoke
        private const int INTERNET_OPEN_TYPE_PRECONFIG = 0; // read registry
        private const int INTERNET_OPEN_TYPE_DIRECT = 1;  // direct to net
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr InternetOpen(string lpszAgent, int dwAccessType, string lpszProxyName, string lpszProxyBypass, int dwFlags);

        private enum INTERNET_OPTION
        {
            // Sets or retrieves an INTERNET_PER_CONN_OPTION_LIST structure that specifies
            // a list of options for a particular connection.
            INTERNET_OPTION_PER_CONNECTION_OPTION = 75,

            // Notify the system that the registry settings have been changed so that
            // it verifies the settings on the next call to InternetConnect.
            INTERNET_OPTION_SETTINGS_CHANGED = 39,

            // Causes the proxy data to be reread from the registry for a handle.
            INTERNET_OPTION_REFRESH = 37
        }
        /// <summary>
        /// Sets an Internet option.
        /// </summary>
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern bool InternetSetOption(IntPtr hInternet, INTERNET_OPTION dwOption, IntPtr lpBuffer, int lpdwBufferLength);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct INTERNET_PER_CONN_OPTION_LIST
        {
            public int Size;

            // The connection to be set. NULL means LAN.
            public IntPtr Connection;

            public int OptionCount;
            public int OptionError;

            // List of INTERNET_PER_CONN_OPTIONs.
            public IntPtr pOptions;
        }
        /// <summary>
        /// Queries an Internet option on the specified handle. The Handle will be always 0.
        /// </summary>
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Ansi, EntryPoint = "InternetQueryOption")]
        private extern static bool InternetQueryOptionList(IntPtr Handle, INTERNET_OPTION OptionFlag, ref INTERNET_PER_CONN_OPTION_LIST OptionList, ref int size);

        [DllImport("wininet.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternetCloseHandle(IntPtr hInternet);

        private const int INTERNET_COOKIE_HTTPONLY = 0x00002000;
        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool InternetGetCookieEx(string pchURL, string pchCookieName, StringBuilder pchCookieData, ref uint pcchCookieData, int dwFlags, IntPtr lpReserved);

        [DllImport("wininet.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool InternetSetCookieEx(string lpszUrlName, string lpszCookieName, string lpszCookieData, uint dwFlags, IntPtr dwReserved);
        #endregion

        #region ProxyMethods
        [StructLayout(LayoutKind.Sequential)]
        private struct INTERNET_PER_CONN_OPTION
        {
            // A value in INTERNET_PER_CONN_OptionEnum.
            public int dwOption;
            public INTERNET_PER_CONN_OPTION_OptionUnion Value;
        }
        private enum INTERNET_PER_CONN_OptionEnum
        {
            INTERNET_PER_CONN_FLAGS = 1,
            INTERNET_PER_CONN_PROXY_SERVER = 2,
            INTERNET_PER_CONN_PROXY_BYPASS = 3,
            INTERNET_PER_CONN_AUTOCONFIG_URL = 4,
            INTERNET_PER_CONN_AUTODISCOVERY_FLAGS = 5,
            INTERNET_PER_CONN_AUTOCONFIG_SECONDARY_URL = 6,
            INTERNET_PER_CONN_AUTOCONFIG_RELOAD_DELAY_MINS = 7,
            INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_TIME = 8,
            INTERNET_PER_CONN_AUTOCONFIG_LAST_DETECT_URL = 9,
            INTERNET_PER_CONN_FLAGS_UI = 10
        }
        /// <summary>
        /// Used in INTERNET_PER_CONN_OPTION.
        /// When create a instance of OptionUnion, only one filed will be used.
        /// The StructLayout and FieldOffset attributes could help to decrease the struct size.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct INTERNET_PER_CONN_OPTION_OptionUnion
        {
            // A value in INTERNET_OPTION_PER_CONN_FLAGS.
            [FieldOffset(0)]
            public int dwValue;
            [FieldOffset(0)]
            public IntPtr pszValue;
            [FieldOffset(0)]
            public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;
        }
        /// <summary>
        /// Constants used in INTERNET_PER_CONN_OPTON struct.
        /// </summary>
        private enum INTERNET_OPTION_PER_CONN_FLAGS
        {
            PROXY_TYPE_DIRECT = 0x00000001,   // direct to net
            PROXY_TYPE_PROXY = 0x00000002,   // via named proxy
            PROXY_TYPE_AUTO_PROXY_URL = 0x00000004,   // autoproxy URL
            PROXY_TYPE_AUTO_DETECT = 0x00000008   // use autoproxy detection
        }

        /// <summary>
        /// Set the proxy server for LAN connection.
        /// <param name="proxyServer">127.0.0.1:8888</param>
        /// </summary>
        public static bool SetConnectionProxy(string proxyServer)
        {
            Contract.Requires(!string.IsNullOrEmpty(proxyServer));

            if (Interlocked.Exchange(ref ProxyLocker, 1) == 0)
            {
                IntPtr hInternet = InternetOpen(ApplicationName, INTERNET_OPEN_TYPE_DIRECT, null, null, 0);

                //// Create 3 options.
                //INTERNET_PER_CONN_OPTION[] Options = new INTERNET_PER_CONN_OPTION[3];

                // Create 2 options.
                INTERNET_PER_CONN_OPTION[] Options = new INTERNET_PER_CONN_OPTION[2];

                // Set PROXY flags.
                Options[0] = new INTERNET_PER_CONN_OPTION();
                Options[0].dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_FLAGS;
                Options[0].Value.dwValue = (int)INTERNET_OPTION_PER_CONN_FLAGS.PROXY_TYPE_PROXY;

                // Set proxy name.
                Options[1] = new INTERNET_PER_CONN_OPTION();
                Options[1].dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_SERVER;
                Options[1].Value.pszValue = Marshal.StringToHGlobalAnsi(proxyServer);

                //// Set proxy bypass.
                //Options[2] = new INTERNET_PER_CONN_OPTION();
                //Options[2].dwOption =
                //    (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_BYPASS;
                //Options[2].Value.pszValue = Marshal.StringToHGlobalAnsi("local");

                //// Allocate a block of memory of the options.
                //IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(Options[0])
                //    + Marshal.SizeOf(Options[1]) + Marshal.SizeOf(Options[2]));

                // Allocate a block of memory of the options.
                IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(Options[0]) + Marshal.SizeOf(Options[1]));

                IntPtr current = buffer;

                // Marshal data from a managed object to an unmanaged block of memory.
                for (int i = 0; i < Options.Length; i++)
                {
                    Marshal.StructureToPtr(Options[i], current, false);
                    current = (IntPtr)((int)current + Marshal.SizeOf(Options[i]));
                }

                // Initialize a INTERNET_PER_CONN_OPTION_LIST instance.
                INTERNET_PER_CONN_OPTION_LIST option_list = new INTERNET_PER_CONN_OPTION_LIST();

                // Point to the allocated memory.
                option_list.pOptions = buffer;

                // Return the unmanaged size of an object in bytes.
                option_list.Size = Marshal.SizeOf(option_list);

                // IntPtr.Zero means LAN connection.
                option_list.Connection = IntPtr.Zero;

                option_list.OptionCount = Options.Length;
                option_list.OptionError = 0;
                int size = Marshal.SizeOf(option_list);

                // Allocate memory for the INTERNET_PER_CONN_OPTION_LIST instance.
                IntPtr intptrStruct = Marshal.AllocCoTaskMem(size);

                // Marshal data from a managed object to an unmanaged block of memory.
                Marshal.StructureToPtr(option_list, intptrStruct, true);

                // Set internet settings.
                bool bReturn = InternetSetOption(hInternet, INTERNET_OPTION.INTERNET_OPTION_PER_CONNECTION_OPTION, intptrStruct, size);

                // Free the allocated memory.
                Marshal.FreeCoTaskMem(buffer);
                Marshal.FreeCoTaskMem(intptrStruct);
                InternetCloseHandle(hInternet);

                // Throw an exception if this operation failed.
                if (!bReturn)
                {
                    throw new ApplicationException("Set Internet Option Failed!");
                }

                return bReturn;
            }
            return false;
        }

        /// <summary>
        /// Restore the options for LAN connection.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool RestoreSystemProxy()
        {
            if (Interlocked.Exchange(ref ProxyLocker, 0) == 1)
            {
                IntPtr hInternet = InternetOpen(ApplicationName, INTERNET_OPEN_TYPE_DIRECT, null, null, 0);

                INTERNET_PER_CONN_OPTION_LIST request = GetSystemProxy();
                int size = Marshal.SizeOf(request);

                // Allocate memory. 
                IntPtr intptrStruct = Marshal.AllocCoTaskMem(size);

                // Convert structure to IntPtr 
                Marshal.StructureToPtr(request, intptrStruct, true);

                // Set internet options.
                bool bReturn = InternetSetOption(hInternet, INTERNET_OPTION.INTERNET_OPTION_PER_CONNECTION_OPTION, intptrStruct, size);

                // Free the allocated memory.
                Marshal.FreeCoTaskMem(request.pOptions);
                Marshal.FreeCoTaskMem(intptrStruct);

                if (!bReturn)
                {
                    throw new ApplicationException("Set Internet Option Failed! ");
                }

                // Notify the system that the registry settings have been changed and cause
                // the proxy data to be reread from the registry for a handle.
                InternetSetOption(hInternet, INTERNET_OPTION.INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
                InternetSetOption(hInternet, INTERNET_OPTION.INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

                InternetCloseHandle(hInternet);

                return bReturn;
            }
            return false;
        }
        /// <summary>
        /// Backup the current options for LAN connection.
        /// Make sure free the memory after restoration. 
        /// </summary>
        private static INTERNET_PER_CONN_OPTION_LIST GetSystemProxy()
        {
            // Query following options. 
            INTERNET_PER_CONN_OPTION[] Options = new INTERNET_PER_CONN_OPTION[3];

            Options[0] = new INTERNET_PER_CONN_OPTION();
            Options[0].dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_FLAGS;
            Options[1] = new INTERNET_PER_CONN_OPTION();
            Options[1].dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_SERVER;
            Options[2] = new INTERNET_PER_CONN_OPTION();
            Options[2].dwOption = (int)INTERNET_PER_CONN_OptionEnum.INTERNET_PER_CONN_PROXY_BYPASS;

            // Allocate a block of memory of the options.
            IntPtr buffer = Marshal.AllocCoTaskMem(Marshal.SizeOf(Options[0]) + Marshal.SizeOf(Options[1]) + Marshal.SizeOf(Options[2]));

            IntPtr current = (IntPtr)buffer;

            // Marshal data from a managed object to an unmanaged block of memory.
            for (int i = 0; i < Options.Length; i++)
            {
                Marshal.StructureToPtr(Options[i], current, false);
                current = (IntPtr)((int)current + Marshal.SizeOf(Options[i]));
            }

            // Initialize a INTERNET_PER_CONN_OPTION_LIST instance.
            INTERNET_PER_CONN_OPTION_LIST Request = new INTERNET_PER_CONN_OPTION_LIST();

            // Point to the allocated memory.
            Request.pOptions = buffer;

            Request.Size = Marshal.SizeOf(Request);

            // IntPtr.Zero means LAN connection.
            Request.Connection = IntPtr.Zero;

            Request.OptionCount = Options.Length;
            Request.OptionError = 0;
            int size = Marshal.SizeOf(Request);

            // Query internet options. 
            bool result = InternetQueryOptionList(IntPtr.Zero, INTERNET_OPTION.INTERNET_OPTION_PER_CONNECTION_OPTION, ref Request, ref size);
            if (!result)
            {
                throw new ApplicationException("Set Internet Option Failed! ");
            }

            return Request;
        }
        #endregion

        #region CookieMethods
        public static void LoadCookies(CookieContainer container, Uri url)
        {
            Contract.Requires(container != null && url != null);

            Uri absoluteUri = new Uri(url.AbsoluteUri);
            uint datasize = 0;
            InternetGetCookieEx(absoluteUri.OriginalString, null, null, ref datasize, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero);
            var cookieData = new StringBuilder((int)datasize);
            if (InternetGetCookieEx(absoluteUri.OriginalString, null, cookieData, ref datasize, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero)
                && cookieData.Length > 0)
            {
                container.SetCookies(absoluteUri, cookieData.Replace(';', ',').ToString());
            }
        }

        public static void SaveCookies(CookieContainer container, Uri url)
        {
            Contract.Requires(container != null && url != null);

            Uri absoluteUri = new Uri(url.AbsoluteUri);
            foreach (Cookie cookie in container.GetCookies(absoluteUri))
            {
                InternetSetCookieEx(absoluteUri.OriginalString, cookie.Name, cookie.Value, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero);
            }
        }
        #endregion

        #region DeleteCache
        public static void CheckTemporaryFiles(float maxMSize = 512f)
        {
            float current;
            string path = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
            //Reference "Windows Script Host Object Model" on the COM tab.
            var FSO = new IWshRuntimeLibrary.FileSystemObject();
            try
            {
                float length = (float)FSO.GetFolder(path).Size;
                current = length / 1024 / 1024;
            }
            finally
            {
                Marshal.FinalReleaseComObject(FSO);
            }
            if (current >= maxMSize)
            {
                DeleteCache(CacheKind.TemporaryInternetFiles);
            }
        }

        public enum CacheKind
        {
            History = 1,
            Cookies = 2,
            TemporaryInternetFiles = 8,
            FormData = 16,
            Passwords = 32,
            DeleteAll = 255,
            DeleteAllAndAdd_ons = 4351
        }
        private static int Locker;
        public static void DeleteCache(CacheKind kind)
        {
            if (Interlocked.Exchange(ref Locker, 1) == 0)
            {
                try
                {
                    var proc = Process.Start(new ProcessStartInfo("cmd.exe")
                    {
                        CreateNoWindow = true,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    });
                    proc.StandardInput.WriteLine("rundll32.exe InetCpl.cpl,ClearMyTracksByProcess {0}", (int)kind);
                    proc.StandardInput.WriteLine("exit");
                }
                finally
                {
                    Interlocked.Exchange(ref Locker, 0);
                }
            }
        }
        #endregion
    }

    internal class NativeMethods
    {
        #region BrowserFeature
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/ee330720(v=vs.85).aspx
        /// </summary>
        public static void SetBrowserFeatureControl()
        {
            // FeatureControl settings are per-process
            string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
            string[] skip = new string[] { "devenv.exe", "XDesProc.exe" };
            if (skip.Any(p => p.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            Action<string, string, uint> SetBrowserFeatureControlKey = (feature, appName, value) =>
            {
                using (var key = Registry.CurrentUser.CreateSubKey(
                    String.Concat(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\", feature),
                    RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    key.SetValue(appName, value, RegistryValueKind.DWord);
                }
            };
            SetBrowserFeatureControlKey("FEATURE_BROWSER_EMULATION", fileName, GetBrowserEmulationMode());
            SetBrowserFeatureControlKey("FEATURE_MANAGE_SCRIPT_CIRCULAR_REFS", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_GPU_RENDERING ", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_AJAX_CONNECTIONEVENTS", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_DOMSTORAGE ", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_IVIEWOBJECTDRAW_DMLT9_WITH_GDI  ", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_NINPUT_LEGACYMODE", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_DISABLE_LEGACY_COMPRESSION", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_LOCALMACHINE_LOCKDOWN", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_BLOCK_LMZ_OBJECT", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_BLOCK_LMZ_SCRIPT", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_DISABLE_NAVIGATION_SOUNDS", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_SCRIPTURL_MITIGATION", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_SPELLCHECKING", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_STATUS_BAR_THROTTLING", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_TABBED_BROWSING", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_VALIDATE_NAVIGATE_URL", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_WEBOC_DOCUMENT_ZOOM", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_WEBOC_POPUPMANAGEMENT", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_WEBOC_MOVESIZECHILD", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_ADDON_MANAGEMENT", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_WEBSOCKET", fileName, 1);
            //SetBrowserFeatureControlKey("FEATURE_WINDOW_RESTRICTIONS ", fileName, 0);
            //SetBrowserFeatureControlKey("FEATURE_XMLHTTP", fileName, 1);
        }

        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/ie/ee330730(v=vs.85).aspx
        /// </summary>
        /// <returns></returns>
        private static uint GetBrowserEmulationMode()
        {
            int browserVersion;
            using (var ieKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Internet Explorer",
                RegistryKeyPermissionCheck.ReadSubTree, System.Security.AccessControl.RegistryRights.QueryValues))
            {
                var version = ieKey.GetValue("svcVersion") ?? ieKey.GetValue("Version");
                if (version == null)
                {
                    throw new ApplicationException("Microsoft Internet Explorer is required!");
                }
                int.TryParse(version.ToString().Split('.')[0], out browserVersion);
            }
            if (browserVersion < 8)
            {
                throw new ApplicationException("Microsoft Internet Explorer 8 is required!");
            }
            switch (browserVersion)
            {
                case 9:
                    return 9000;
                case 10:
                    return 10000;
                case 11:
                    return 11000;
                default:
                    return 8000;
            }
        }
        #endregion

        #region DrawTo
        [Flags]
        internal enum ErrorModes : uint
        {
            SYSTEM_DEFAULT = 0x0,
            SEM_FAILCRITICALERRORS = 0x0001,
            SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
            SEM_NOGPFAULTERRORBOX = 0x0002,
            SEM_NOOPENFILEERRORBOX = 0x8000
        }
        /// <summary>
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/ms680621%28v=vs.85%29.aspx
        /// </summary>
        /// <param name="mode"></param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        internal static extern ErrorModes SetErrorMode(ErrorModes mode);

        [ComVisible(true), ComImport]
        [Guid("0000010D-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IViewObject
        {
            [return: MarshalAs(UnmanagedType.I4)]
            [PreserveSig]
            int Draw(
                [MarshalAs(UnmanagedType.U4)] System.Runtime.InteropServices.ComTypes.DVASPECT dwAspect,
                int lindex,
                IntPtr pvAspect,
                [In] IntPtr ptd,
                IntPtr hdcTargetDev,
                IntPtr hdcDraw,
                [MarshalAs(UnmanagedType.Struct)] ref Rectangle lprcBounds,
                [In] IntPtr lprcWBounds,
                IntPtr pfnContinue,
                [MarshalAs(UnmanagedType.U4)] uint dwContinue);
        }

        public static void DrawTo(object vObj, Image destination, Color backgroundColor)
        {
            Contract.Requires(vObj != null && destination != null);

            using (var g = Graphics.FromImage(destination))
            {
                IntPtr deviceContextHandle = IntPtr.Zero;
                var rectangle = new Rectangle(Point.Empty, destination.Size);
                g.Clear(backgroundColor);
                try
                {
                    deviceContextHandle = g.GetHdc();
                    var viewObject = (IViewObject)vObj;
                    viewObject.Draw(System.Runtime.InteropServices.ComTypes.DVASPECT.DVASPECT_CONTENT, -1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, deviceContextHandle, ref rectangle, IntPtr.Zero, IntPtr.Zero, 0);
                }
                finally
                {
                    if (deviceContextHandle != IntPtr.Zero)
                    {
                        g.ReleaseHdc(deviceContextHandle);
                    }
                }
            }
        }
        #endregion
    }
}