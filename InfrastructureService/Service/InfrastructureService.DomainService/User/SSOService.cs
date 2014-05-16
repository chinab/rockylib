using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Net;
using InfrastructureService.Contract;
using InfrastructureService.Model.User;
using InfrastructureService.Repository.User;

namespace InfrastructureService.DomainService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“SSOService”。
    public class SSOService : ServiceBase, IUserService
    {
        #region Static
        private static readonly List<SSONotify> Domains;
        private static readonly double ExpireMinutes;
        private static readonly MemoryCache _cache;

        static SSOService()
        {
            string clientHandlerFormat = ConfigurationManager.AppSettings["ClientHandlerFormat"];
            if (string.IsNullOrEmpty(clientHandlerFormat))
            {
                throw new InvalidOperationException("ClientHandlerFormat");
            }
            Domains = ConfigurationManager.AppSettings["NotifyDomains"].Split(',').Select(t => new SSONotify()
            {
                Domain = t,
                ClientHandlerUrl = string.Format(clientHandlerFormat, t.Remove(0, 1))
            }).ToList();
            double.TryParse(ConfigurationManager.AppSettings["ClientTimeout"], out ExpireMinutes);
            _cache = new MemoryCache(typeof(SSOService).FullName);
        }

#if DEBUG
        public static KeyValuePair<string, object>[] GetAllItems()
        {
            var all = _cache.ToArray();
            return all;
        }
#endif

        /// <summary>
        /// 异步通知
        /// </summary>
        /// <param name="isOut"></param>
        /// <param name="id"></param>
        private static void NotifyDomains(bool isOut, SSOIdentity id)
        {
            TaskHelper.Factory.StartNew(() =>
            {
                var client = new HttpClient();
                foreach (var domain in Domains)
                {
                    client.SetRequest(new Uri(domain.ClientHandlerUrl));
                    client.Form["_SID"] = id.SessionID;
                    client.Form["Action"] = Convert.ToUInt32(isOut).ToString();
                    client.Form["Token"] = id.Token;
                    App.Retry(() =>
                    {
                        try
                        {
                            string text = client.GetResponse().GetResponseText();
                            return text == "1";
                        }
                        catch (System.Net.WebException ex)
                        {
                            App.LogError(ex, "SSOServiceNotify");
                            return false;
                        }
                    }, 3);
                }
            });
        }
        #endregion

        #region Fields
        private UserRepository mgr;
        #endregion

        #region Constructors
        public SSOService()
        {
            mgr = new UserRepository();
        }
        #endregion

        #region Sign
        public bool IsUserNameExists(IsUserNameExistsParameter param)
        {
            return mgr.IsUserNameExists(param);
        }

        public void SignUp(SignUpParameter param)
        {
            mgr.SignUp(param);
        }

        public SSOIdentity SignIn(SignInParameter param)
        {
            SSOIdentity id = mgr.SignIn(param);
            if (id.IsAuthenticated)
            {
                this.SetSignIn(id);
            }
            return id;
        }

        /// <summary>
        /// 把Identity写入内存，仅供上层调用不作对外服务
        /// </summary>
        /// <param name="identity"></param>
        public void SetSignIn(SSOIdentity identity, DateTime? expiresDate = null)
        {
            var newToken = Guid.NewGuid();
            var policy = new CacheItemPolicy()
            {
                Priority = CacheItemPriority.NotRemovable,
                SlidingExpiration = TimeSpan.FromMinutes(ExpireMinutes)
            };
            if (expiresDate.HasValue)
            {
                if (identity.Token != null)
                {
                    mgr.CreatePersistentIdentity(identity, expiresDate.Value, newToken);
                }
                policy.RemovedCallback = arguments =>
                {
                    if (arguments.RemovedReason == CacheEntryRemovedReason.Removed)
                    {
                        return;
                    }
                    var id = (SSOIdentity)arguments.CacheItem.Value;
                    id.IsAuthenticated = false;
                    mgr.CreatePersistentIdentity(id, expiresDate.Value);
                    NotifyDomains(true, id);
                };
            }
            if (identity.Token != null)
            {
                _cache.Remove(identity.Token);
            }
            //颁发新token，确保token随会话更新
            identity.Token = newToken.ToString("N");
            identity.IssueDate = DateTime.Now;
            identity.IsAuthenticated = true;
            _cache.Set(identity.Token, identity, policy);
            NotifyDomains(false, identity);
        }

        public SSOIdentity GetIdentity(GetIdentityParameter param)
        {
            var id = (SSOIdentity)_cache.Get(param.Token) ?? mgr.QueryPersistentIdentity(param.Token);
            if (id != null && id.IsAuthenticated)
            {
                SetSignIn(id, param.ExpiresDate);
            }
            return id;
        }

        public void SignOut(string token)
        {
            if (_cache.Contains(token))
            {
                _cache.Remove(token);
                //移除PersistentIdentity
            }
        }

        public GetServerConfigResult GetServerConfig()
        {
            var result = new GetServerConfigResult();
            result.Notify = Domains.ToArray();
            result.Timeout = ExpireMinutes == default(double) ? null : (int?)ExpireMinutes;
            return result;
        }

        public SSOIdentity OAuth(OAuthParameter param)
        {
            var id = mgr.OAuth(param);
            if (id.IsAuthenticated)
            {
                this.SetSignIn(id);
            }
            return id;
        }
        #endregion

        #region Email & Mobile
        public void SendAuthEmail(SendAuthEmailParameter param)
        {
            mgr.SendAuthEmail(param);
        }
        public void VerifyEmail(Guid authCode)
        {
            mgr.VerifyEmail(authCode);
        }

        public void SendAuthMobile(SendAuthMobileParameter param)
        {
            mgr.SendAuthMobile(param);
        }
        public void VerifyMobile(VerifyMobileParameter param)
        {
            mgr.VerifyMobile(param);
        }
        #endregion

        #region Info
        public void SendFindPwdCode(SendFindPwdCodeParameter param)
        {
            mgr.SendFindPwdCode(param);
        }

        public void ChangePassword(ChangePasswordParameter param)
        {
            mgr.ChangePassword(param);
        }

        public QueryUsersResult QueryUsers(QueryUsersParameter pager)
        {
            return mgr.QueryUsers(pager);
        }

        public QuerySignInLogsResult QuerySignInLogs(QuerySignInLogsParameter pager)
        {
            return mgr.QuerySignInLogs(pager);
        }
        #endregion
    }
}