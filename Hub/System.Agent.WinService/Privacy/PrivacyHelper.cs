using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace System.Agent.Privacy
{
    internal static class PrivacyHelper
    {
        #region IdleFinder
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
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

        #region FormatDrive
        /// <summary>
        /// test if the provided filesystem value is valid
        /// </summary>
        /// <param name="fileSystem">file system. Possible values : "FAT", "FAT32", "EXFAT", "NTFS", "UDF".</param>
        /// <returns>true if valid, false if invalid</returns>
        public static bool IsFileSystemValid(string fileSystem)
        {
            switch (fileSystem)
            {
                case "FAT":
                case "FAT32":
                case "EXFAT":
                case "NTFS":
                case "UDF":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Format a drive using Format.com windows file
        /// </summary>
        /// <param name="driveLetter">drive letter. Example : 'A', 'B', 'C', 'D', ..., 'Z'.</param>
        /// <param name="label">label for the drive</param>
        /// <param name="fileSystem">file system. Possible values : "FAT", "FAT32", "EXFAT", "NTFS", "UDF".</param>
        /// <param name="quickFormat">quick formatting?</param>
        /// <param name="enableCompression">enable drive compression?</param>
        /// <param name="clusterSize">cluster size (default=null for auto). Possible value depends on the file system : 512, 1024, 2048, 4096, 8192, 16384, 32768, 65536, ...</param>
        /// <returns>true if success, false if failure</returns>
        public static void FormatDrive(char driveLetter, string label = "", string fileSystem = "NTFS", bool quickFormat = true, bool enableCompression = false, int? clusterSize = null)
        {
            Contract.Requires(Char.IsLetter(driveLetter) && IsFileSystemValid(fileSystem));

            string drive = driveLetter + ":";
            var psi = new ProcessStartInfo();
            psi.FileName = "format.com";
            psi.WorkingDirectory = Environment.SystemDirectory;
            psi.Arguments = "/FS:" + fileSystem +
                                         " /Y" +
                                         " /V:" + label +
                                         (quickFormat ? " /Q" : "") +
                                         ((fileSystem == "NTFS" && enableCompression) ? " /C" : "") +
                                         (clusterSize.HasValue ? " /A:" + clusterSize.Value : "") +
                                         " " + drive;
            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            var formatProcess = Process.Start(psi);
            var swStandardInput = formatProcess.StandardInput;
            swStandardInput.WriteLine();
            formatProcess.WaitForExit();
        }
        #endregion
    }

    [SuppressUnmanagedCodeSecurity]
    class NativeMethods
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        public enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        public enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        public const int GENERIC_ALL_ACCESS = 0x10000000;
        public const int CREATE_NO_WINDOW = 0x08000000;

        [
           DllImport("kernel32.dll",
              EntryPoint = "CloseHandle", SetLastError = true,
              CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)
        ]
        public static extern bool CloseHandle(IntPtr handle);

        [
           DllImport("advapi32.dll",
              EntryPoint = "CreateProcessAsUser", SetLastError = true,
              CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)
        ]
        public static extern bool
           CreateProcessAsUser(IntPtr hToken, string lpApplicationName, string lpCommandLine,
                               ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes,
                               bool bInheritHandle, Int32 dwCreationFlags, IntPtr lpEnvrionment,
                               string lpCurrentDirectory, ref STARTUPINFO lpStartupInfo,
                               ref PROCESS_INFORMATION lpProcessInformation);

        [
           DllImport("advapi32.dll",
              EntryPoint = "DuplicateTokenEx")
        ]
        public static extern bool
           DuplicateTokenEx(IntPtr hExistingToken, Int32 dwDesiredAccess,
                            ref SECURITY_ATTRIBUTES lpThreadAttributes,
                            Int32 ImpersonationLevel, Int32 dwTokenType,
                            ref IntPtr phNewToken);



        public static Process CreateProcessAsUser(string filename, string args)
        {
            var hToken = WindowsIdentity.GetCurrent().Token;
            var hDupedToken = IntPtr.Zero;

            var pi = new PROCESS_INFORMATION();
            var sa = new SECURITY_ATTRIBUTES();
            sa.Length = Marshal.SizeOf(sa);

            try
            {
                if (!DuplicateTokenEx(
                        hToken,
                        GENERIC_ALL_ACCESS,
                        ref sa,
                        (int)SECURITY_IMPERSONATION_LEVEL.SecurityIdentification,
                        (int)TOKEN_TYPE.TokenPrimary,
                        ref hDupedToken
                    ))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                var si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.lpDesktop = "";

                var path = Path.GetFullPath(filename);
                var dir = Path.GetDirectoryName(path);

                // Revert to self to create the entire process; not doing this might
                // require that the currently impersonated user has "Replace a process
                // level token" rights - we only want our service account to need
                // that right.
                using (var ctx = WindowsIdentity.Impersonate(IntPtr.Zero))
                {
                    //UInt32 dwSessionId = 0;  // set it to session 0
                    //SetTokenInformation(hDupedToken, TokenInformationClass.TokenSessionId, ref dwSessionId, (UInt32)IntPtr.Size);
                    if (!CreateProcessAsUser(
                                            hDupedToken,
                                            path,
                                            string.Format("\"{0}\" {1}", filename.Replace("\"", "\"\""), args),
                                            ref sa, ref sa,
                                            false, 0, IntPtr.Zero,
                                            dir, ref si, ref pi
                                    ))
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                }

                return Process.GetProcessById(pi.dwProcessID);
            }
            finally
            {
                if (pi.hProcess != IntPtr.Zero)
                    CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero)
                    CloseHandle(pi.hThread);
                if (hDupedToken != IntPtr.Zero)
                    CloseHandle(hDupedToken);
            }
        }
    }
}