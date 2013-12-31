using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace System.Agent
{
    public struct AgentHubConfig
    {
        public static readonly string AppConfigPath;
        public static AgentHubConfig AppConfig
        {
            get
            {
                var exe = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
                {
                    ExeConfigFilename = AppConfigPath
                }, ConfigurationUserLevel.None);
                var config = new AgentHubConfig();
                config.AsServerNode = Convert.ToBoolean(GetValue(exe, "AsServerNode"));
                config.EnableSsl = Convert.ToBoolean(GetValue(exe, "EnableSsl"));
                config.Credential = GetCredential(exe, "Credential");
                config.TunnelList = GetTunnelList(exe, "TunnelList");

                config.LockBg = GetValue(exe, "LockBg", string.Empty);
                config.UnlockPIN = GetValue(exe, "UnlockPIN", "123456");
                config.IdleLock = Convert.ToUInt16(GetValue(exe, "IdleLock", "40"));
                config.BanCount = Convert.ToUInt16(GetValue(exe, "BanCount", "7"));
                return config;
            }
        }

        static AgentHubConfig()
        {
            AppConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\JeansMan Studio\AgentHub.config";
            Hub.CreateDirectory(AppConfigPath);
            if (!File.Exists(AppConfigPath))
            {
                Hub.CreateFileFromResource("System.Agent.AgentHub.config", AppConfigPath, Hub.CombinePath("Agent.exe"));
            }
        }

        #region Methods
        private static string GetValue(System.Configuration.Configuration exe, string key, string defaultVal = null)
        {
            var item = exe.AppSettings.Settings[key];
            if (item == null || string.IsNullOrEmpty(item.Value))
            {
                if (defaultVal != null)
                {
                    return defaultVal;
                }
                throw new InvalidOperationException(string.Format("配置项 {0} 错误", key));
            }
            return item.Value;
        }

        private static NetworkCredential GetCredential(System.Configuration.Configuration exe, string key)
        {
            var sCred = GetValue(exe, key);
            if (string.IsNullOrEmpty(sCred))
            {
                throw new InvalidOperationException("配置项 Credential 错误");
            }
            var aCred = sCred.Split(':');
            return new NetworkCredential(aCred[0], aCred[1]);
        }

        private static Tuple<ushort, string>[] GetTunnelList(System.Configuration.Configuration exe, string key)
        {
            var sList = GetValue(exe, key);
            if (string.IsNullOrEmpty(sList))
            {
                throw new InvalidOperationException("配置项 TunnelList 错误");
            }
            var q = from t in sList.Split(',')
                    let a = t.Trim().Split('#')
                    where !a[0].StartsWith("--")
                    select Tuple.Create(ushort.Parse(a[0]), a[1]);
            return q.ToArray();
        }
        #endregion

        public bool AsServerNode;
        public bool EnableSsl;
        public NetworkCredential Credential;
        public Tuple<ushort, string>[] TunnelList;

        public string LockBg;
        public string UnlockPIN;
        public ushort IdleLock;
        public ushort BanCount;
    }
}