using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.InteropServices;
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
}