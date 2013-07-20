using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetFwTypeLib;

namespace System.Agent
{
    internal class SecurityPolicy
    {
        internal static readonly string PipeName = Guid.NewGuid().ToString("N");

        internal static void Check()
        {
            var evi = new System.Security.Policy.Evidence();
            evi.AddHostEvidence(new System.Security.Policy.Zone(System.Security.SecurityZone.Intranet));
            var ps = new System.Security.PermissionSet(System.Security.Permissions.PermissionState.None);
            ps.AddPermission(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.SecurityPermissionFlag.Assertion | System.Security.Permissions.SecurityPermissionFlag.Execution | System.Security.Permissions.SecurityPermissionFlag.BindingRedirects));
            ps.AddPermission(new System.Security.Permissions.FileIOPermission(System.Security.Permissions.PermissionState.Unrestricted));
            var domainSetup = new AppDomainSetup();
            domainSetup.ApplicationBase = Environment.CurrentDirectory;
            var domain = AppDomain.CreateDomain("CrackCheck", evi, domainSetup, ps, null);
            try
            {
                string crackCheck = (string)domain.CreateInstanceFromAndUnwrap(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName, typeof(string).FullName);
                AppDomain.Unload(domain);
            }
            catch (Exception ex)
            {
                if (ex.Message.Equals("8013141a", StringComparison.OrdinalIgnoreCase))
                {
                    Console.Out.WriteError("对本应用的校验不合法，请从官方重新安装应用。");
                    System.Threading.Thread.Sleep(6000);
                    Environment.Exit(0);
                }
            }
        }

        /// <summary>
        /// 将应用程序添加到防火墙例外
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="execPath"></param>
        public static void App2Fw(string appName, string execPath)
        {
            var mgr = (INetFwMgr)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwMgr"));
            var q = from t in mgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Cast<INetFwAuthorizedApplication>()
                    where t.Name == appName
                    select t;
            var app = q.FirstOrDefault();
            if (app != null)
            {
                if (app.ProcessImageFileName == execPath)
                {
                    return;
                }
                mgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Remove(app.ProcessImageFileName);
            }
            app = (INetFwAuthorizedApplication)Activator.CreateInstance(Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication"));
            app.Enabled = true;
            app.IpVersion = NET_FW_IP_VERSION_.NET_FW_IP_VERSION_ANY;
            app.Scope = NET_FW_SCOPE_.NET_FW_SCOPE_ALL;
            app.Name = appName;
            app.ProcessImageFileName = execPath;
            mgr.LocalPolicy.CurrentProfile.AuthorizedApplications.Add(app);
        }
    }
}