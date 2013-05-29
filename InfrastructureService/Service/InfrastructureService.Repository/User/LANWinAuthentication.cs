using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace InfrastructureService.Repository.User
{
    /// <summary>
    /// http://www.iis.net/ConfigReference/system.webServer/security/authentication/windowsAuthentication#006
    /// http://msdn.microsoft.com/zh-cn/library/ff647405.aspx
    /// http://www.cnblogs.com/kting/archive/2011/12/02/2272336.html
    /// 1、服务器与客户端必须在同一个域名下。
    /// 2、IIS网站属性的身份验证方式：取消匿名身份验证，只保留Windows Authentication。
    /// 3、Web.config文件中的身份验证方式，使用Windows身份验证。<identity impersonate="true"/><authentication mode="Windows"/>
    /// 4、发布到IIS后，将Application Pool选择为Classic；ISAPI和CGI Restrictions设置为Allowed。
    /// </summary>
    public class LANWinAuthentication : IDisposable
    {
        #region WinAPI
        private const int LOGON32_LOGON_INTERACTIVE = 2;
        private const int LOGON32_PROVIDER_DEFAULT = 0;
        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        private static extern int LogonUser(String lpszUserName,
                                          String lpszDomain,
                                          String lpszPassword,
                                          int dwLogonType,
                                          int dwLogonProvider,
                                          ref IntPtr phToken);
        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private extern static int DuplicateToken(IntPtr hToken,
                                          int impersonationLevel,
                                          ref IntPtr hNewToken);
        #endregion

        private WindowsImpersonationContext _impersonationContext;

        public string Domain { get; private set; }
        public string UserName { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="principal">
        /// <example>
        /// HttpContext.Current.User
        /// </example>
        /// </param>
        public LANWinAuthentication(IPrincipal principal = null)
        {
            WindowsPrincipal user;
            if (principal != null)
            {
                user = new WindowsPrincipal(new WindowsIdentity(principal.Identity.Name, principal.Identity.AuthenticationType));
            }
            else
            {
                user = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            }
            string[] arg = user.Identity.Name.Split(new char[1] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (arg.Length > 1)
            {
                this.Domain = arg[0];
                this.UserName = arg[1];
            }
        }

        /// <summary>
        /// 输入登录域、用户名、密码判断是否成功
        /// </summary>
        /// <param name="userName">账户名称</param>
        /// <param name="domain">要登录的域</param>
        /// <param name="password">账户密码</param>
        /// <returns>成功返回true,否则返回false</returns>
        public bool ImpersonateUser(string userName, string domain, string password)
        {
            IntPtr token = IntPtr.Zero;
            if (LogonUser(userName, domain, password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, ref token) != 0)
            {
                IntPtr tokenDuplicate = IntPtr.Zero;
                if (DuplicateToken(token, 2, ref tokenDuplicate) != 0)
                {
                    WindowsIdentity identity = new WindowsIdentity(tokenDuplicate);
                    _impersonationContext = identity.Impersonate();
                    if (_impersonationContext != null)
                    {
                        this.UserName = userName;
                        this.Domain = domain;
                        return true;
                    }
                }
            }
            return false;
        }

        public void Dispose()
        {
            if (_impersonationContext != null)
            {
                _impersonationContext.Undo();
                _impersonationContext.Dispose();
                _impersonationContext = null;
            }
        }
    }
}