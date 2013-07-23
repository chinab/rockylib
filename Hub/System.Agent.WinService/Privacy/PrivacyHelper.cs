using Db4objects.Db4o;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Agent.Privacy
{
    internal static class PrivacyHelper
    {
        #region Config
        private static IEmbeddedObjectContainer _Db;

        internal static ConfigEntity Config
        {
            get
            {
                using (var db = OpenDb())
                {
                    var config = db.Query<ConfigEntity>().FirstOrDefault();
                    if (config == null)
                    {
                        var disk = from t in DriveInfo.GetDrives()
                                   where t.DriveType == DriveType.Fixed || t.DriveType == DriveType.Removable
                                   orderby t.Name ascending
                                   select t;
                        char diskName = disk.Last().Name[0];
                        config = new ConfigEntity()
                        {
                            Password = "123456",
                            Drive = diskName
                        };
                    }
                    return config;
                }
            }
            set
            {
                using (var db = OpenDb())
                {
                    var config = db.Query<ConfigEntity>().FirstOrDefault();
                    if (config != null && config != value)
                    {
                        config.Password = value.Password;
                        config.Background = value.Background;
                        config.Drive = value.Drive;
                    }
                    db.Store(config);
                }
            }
        }

        private static IObjectContainer OpenDb()
        {
            if (_Db == null)
            {
                string path = Hub.CombinePath("Db.yap");
                _Db = Db4oEmbedded.OpenFile(path);
            }
            return _Db.Ext().OpenSession();
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