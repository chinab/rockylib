using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace System.Agent
{
    internal sealed class AgentApp : IHubEntry
    {
        internal static AgentApp Instance;

        internal HttpTunnelClient FirstClient { get; private set; }

        public AgentApp()
        {
            Instance = this;
        }

        public void Main(object arg)
        {
            this.CatchExec(() => SecurityPolicy.App2Fw("Agent", Hub.CombinePath("Agent.exe")), "防火墙例外");
            if (!this.CatchExec(() => CryptoManaged.TrustCert(Hub.GetResourceStream("System.Agent.Resource.CA.crt")), "导入Http证书"))
            {
                Console.Out.WriteWarning("导入Http证书失败。");
            }
            try
            {
                var config = AgentHubConfig.AppConfig;
                Console.Out.WriteInfo("加载配置{0}成功...", AgentHubConfig.AppConfigPath);
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
            }
            catch (WebException ex)
            {
                Console.Out.WriteError("凭证验证失败：{0}", ex.Message);
            }
            catch (Exception ex)
            {
                Console.Out.WriteError(ex.Message);
                Hub.LogError(ex, "Agent");
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
            var config = AgentHubConfig.AppConfig;
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

        private Uri[] GetServerBalance(AgentHubConfig config)
        {
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
                    if (!this.CatchExec(() => CryptoManaged.TrustCert(Hub.GetResourceStream("System.Agent.Resource.xine.pfx"), "xine"), "导入证书"))
                    {
                        config.EnableSsl = false;
                        Console.Out.WriteWarning("导入证书失败，将不启用加密通讯。");
                    }
                }
                //服务端分配域名
                string domain = "azure.xineworld.com";
                //domain = "localhost:3463";
                var client = new HttpClient(new Uri(string.Format("http://{0}/Let.ashx", domain)));
                var res = client.GetResponse();
                var q = from t in res.GetResponseText().Split('#')
                        where !string.IsNullOrEmpty(t)
                        select new Uri(string.Format("{0}://{1}/Go.ashx", config.EnableSsl ? Uri.UriSchemeHttps : Uri.UriSchemeHttp, t));
                serverBalance.AddRange(q);
                Console.Out.WriteInfo("连接服务器{0}...", q.First());
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
                Hub.LogError(ex, string.Format("CatchExec: {0}", msg));
            }
            return false;
        }
        #endregion
    }
}