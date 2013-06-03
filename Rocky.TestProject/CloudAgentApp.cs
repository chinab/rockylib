//#define Mono
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Rocky.Net;

namespace Rocky.TestProject
{
    internal sealed class CloudAgentApp : IRawEntry
    {
        internal static CloudAgentApp Instance;

        internal HttpTunnelClient FirstClient { get; private set; }

        public CloudAgentApp()
        {
            Instance = this;
        }

        public void Main(object arg)
        {
#if !Mono
            this.CatchExec(() => SecurityPolicy.App2Fw("CloudAgent", Runtime.CombinePath("CloudAgent.exe")), "防火墙例外");
#endif
            if (!this.CatchExec(() => CryptoManaged.TrustCert(Runtime.GetResourceStream("Rocky.TestProject.Resource.CA.crt")), "导入Http证书"))
            {
                Console.Out.WriteWarning("导入Http证书失败。");
            }
            try
            {
                var config = CloudAgentConfig.AppConfig;
                Console.Out.WriteInfo("加载配置{0}成功...", CloudAgentConfig.AppConfigPath);
                //创建隧道客户端
                foreach (var tunnel in config.TunnelList)
                {
                    string remoteMsg = string.Empty;
                    Guid? remoteID = null;
                    string mode = tunnel.Item2;
                    int i = mode.LastIndexOf(@"\");
                    if (i != -1)
                    {
                        string sRemoteID = mode.Substring(i + 1);
                        remoteMsg = string.Format("ReverseTo={0}", sRemoteID);
                        remoteID = Guid.Parse(sRemoteID);
                        mode = mode.Substring(0, i);
                    }
                    SocksProxyType runType;
                    if (Enum.TryParse(mode, out runType))
                    {
                        var client = CreateTunnelClient(tunnel.Item1, runType, null, remoteID);
                        Console.Out.WriteTip("隧道 {0}:{1} RunMode={2} {3}\t开启...", client.AgentHost, tunnel.Item1, runType, remoteMsg);
                    }
                    else
                    {
                        var directTo = SocketHelper.ParseEndPoint(mode);
                        var client = CreateTunnelClient(tunnel.Item1, runType, directTo, remoteID);
                        Console.Out.WriteTip("隧道 {0}:{1} DirectTo={2} {3}\t开启...", client.AgentHost, tunnel.Item1, directTo, remoteMsg);
                    }
                }
                //MonitorChannel.Server(53);
            }
            catch (WebException ex)
            {
                Console.Out.WriteError("凭证验证失败：{0}", ex.Message);
            }
            catch (Exception ex)
            {
                Console.Out.WriteError(ex.Message);
                Runtime.LogError(ex, "CloudAgent");
            }
            finally
            {
                Console.ReadLine();
            }
        }

        public IEnumerable<Tuple<string, Guid>> GetDeviceIdentity()
        {
            Contract.Requires(this.FirstClient != null);

            return this.FirstClient.GetDeviceIdentity().Select(t => Tuple.Create(string.Format(@"{0}\{1}", t.WAN, t.LAN), t.ID));
        }

        public HttpTunnelClient CreateTunnelClient(ushort listenPort, SocksProxyType runType, IPEndPoint directTo, Guid? remoteID)
        {
            var config = CloudAgentConfig.AppConfig;
            var serverBalance = this.GetServerBalance(config);
            HttpTunnelClient client;
            if (directTo == null)
            {
                client = new HttpTunnelClient(listenPort, serverBalance, config.Credential, runType);
            }
            else
            {
                client = new HttpTunnelClient(listenPort, serverBalance, config.Credential, directTo);
            }
            client.ReverseRemoteID = remoteID;
            if (this.FirstClient == null)
            {
                this.FirstClient = client;
            }
            return client;
        }

        private Uri[] GetServerBalance(CloudAgentConfig config)
        {
            string domain = "azure.xineworld.com";
            //domain = "localhost:3463";
            Console.Out.WriteInfo("连接服务器{0}...", domain);
            var serverBalance = new List<Uri>();
            if (config.AsServerNode)
            {
                //创建本地服务节点
                Uri serverUrl;
                xHttpServer.Start(@"C:\Packages\azure", out serverUrl);
                Console.Out.WriteInfo("服务端节点{0}开启...", serverUrl);
                serverBalance.Add(serverUrl);
            }
            else
            {
                if (config.EnableSsl)
                {
                    //默认服务端
                    if (!this.CatchExec(() => CryptoManaged.TrustCert(Runtime.GetResourceStream("Rocky.TestProject.Resource.xine.pfx"), "xine"), "导入证书"))
                    {
                        config.EnableSsl = false;
                        Console.Out.WriteWarning("导入证书失败，将不启用加密通讯。");
                    }
                }
                //服务端分配域名
                var client = new HttpClient(new Uri(string.Format("http://{0}/X.ashx", domain)));
                var res = client.GetResponse();
                var q = from t in res.GetResponseText().Split('#')
                        where !string.IsNullOrEmpty(t)
                        select new Uri(string.Format("{0}://{1}/Go.ashx", config.EnableSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp, t));
                serverBalance.AddRange(q);
            }
            return serverBalance.ToArray();
        }

        #region Methods
        private bool CatchExec(Action action, string msg)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                Runtime.LogError(ex, string.Format("CatchExec: {0}", msg));
            }
            return false;
        }
        #endregion
    }

    #region CloudAgentConfig
    public struct CloudAgentConfig
    {
        public static readonly string AppConfigPath;
        public static CloudAgentConfig AppConfig
        {
            get
            {
                var exe = ConfigurationManager.OpenMappedExeConfiguration(new ExeConfigurationFileMap()
                {
                    ExeConfigFilename = AppConfigPath
                }, ConfigurationUserLevel.None);
                var config = new CloudAgentConfig();
                config.AsServerNode = Convert.ToBoolean(GetValue(exe, "AsServerNode"));
                config.EnableSsl = Convert.ToBoolean(GetValue(exe, "EnableSsl"));
                config.Credential = GetCredential(exe, "Credential");
                config.TunnelList = GetTunnelList(exe, "TunnelList");
                return config;
            }
        }

        static CloudAgentConfig()
        {
            AppConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + @"\JeansMan Studio\CloudAgent.config";
            Runtime.CreateDirectory(AppConfigPath);
            if (!File.Exists(AppConfigPath))
            {
                Runtime.CreateFileFromResource("Rocky.TestProject.CloudAgent.config", AppConfigPath, "CloudAgent.exe");
            }
        }

        private static string GetValue(Configuration exe, string key)
        {
            var item = exe.AppSettings.Settings[key];
            if (item == null || string.IsNullOrEmpty(item.Value))
            {
                throw new InvalidOperationException(string.Format("配置项 {0} 错误", key));
            }
            return item.Value;
        }

        private static NetworkCredential GetCredential(Configuration exe, string key)
        {
            var sCred = GetValue(exe, key);
            if (string.IsNullOrEmpty(sCred))
            {
                throw new InvalidOperationException("配置项 Credential 错误");
            }
            var aCred = sCred.Split(':');
            return new NetworkCredential(aCred[0], aCred[1]);
        }

        private static Tuple<ushort, string>[] GetTunnelList(Configuration exe, string key)
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

        public bool AsServerNode;
        public bool EnableSsl;
        public NetworkCredential Credential;
        public Tuple<ushort, string>[] TunnelList;
    }
    #endregion
}