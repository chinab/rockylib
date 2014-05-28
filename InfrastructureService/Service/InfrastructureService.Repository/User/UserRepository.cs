using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Data;
using EntityFramework.Extensions;
using InfrastructureService.Model.User;
using InfrastructureService.Repository.DataAccess;
using System.Net.WCF;

namespace InfrastructureService.Repository.User
{
    public class UserRepository : RepositoryBase
    {
        #region Sign
        /// <summary>
        /// 验证用户名是否存在
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public bool IsUserNameExists(IsUserNameExistsParameter param)
        {
            using (var context = base.CreateUserContext())
            {
                var q = from t in context.Accounts
                        where t.AppID == param.AppID && t.UserName == param.UserName
                        select t;
                return q.Any();
            }
        }

        /// <summary>
        /// 注册
        /// </summary>
        /// <param name="param"></param>
        public void SignUp(SignUpParameter param)
        {
            string orgPwd = param.Password;
            param.Password = CryptoManaged.MD5Hex(param.Password);
            using (var scope = DbScope.Create())
            using (var context = base.CreateUserContext())
            {
                scope.BeginTransaction();

                if (this.IsUserNameExists(new IsUserNameExistsParameter()
                {
                    AppID = param.AppID,
                    UserName = param.UserName
                }))
                {
                    throw new InvalidInvokeException(SignUpErrorCode.AccountExist.ToDescription());
                }

                var dataObj = new Account();
                EntityMapper.Map<SignUpParameter, Account>(param, dataObj);
                dataObj.RowID = Guid.NewGuid();
                dataObj.CreateDate = DateTime.Now;
                context.Accounts.Add(dataObj);
                context.SaveChanges();

                if (param.SmsCode != default(int))
                {
                    VerifyMobile(new VerifyMobileParameter()
                    {
                        Mobile = param.Mobile,
                        SmsCode = param.SmsCode
                    });
                }

                scope.Complete();

                if (!string.IsNullOrEmpty(param.Email))
                {
                    this.SendAuthEmail(new SendAuthEmailParameter()
                    {
                        AppID = param.AppID,
                        UserID = dataObj.RowID,
                        Email = param.Email,
                        Kind = AuthEmailKind.SignUp
                    });
                }
            }
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public SSOIdentity SignIn(SignInParameter param)
        {
            if (param.Password.Length < 32)
            {
                param.Password = CryptoManaged.MD5Hex(param.Password);
            }

            using (var context = base.CreateUserContext())
            {
                var q = from t in context.Accounts
                        where t.AppID == param.AppID
                        && t.UserName == param.UserName && t.Password == param.Password
                        select new SSOIdentity
                        {
                            UserID = t.RowID,
                            UserName = t.UserName,
                            Token = Guid.NewGuid().ToString("N"),
                            IssueDate = DateTime.Now,
                            IsAuthenticated = true
                        };
                var result = q.DefaultIfEmpty(new SSOIdentity()
                {
                    UserName = param.UserName,
                    IsAuthenticated = false
                }).Single();
                if (param.LogSignIn)
                {
                    var log = new SignInLog();
                    log.UserName = param.UserName;
                    log.ClientIP = param.ClientIP;
                    log.Platform = param.Platform;
                    log.SignInDate = DateTime.Now;
                    log.IsSuccess = result.IsAuthenticated;
                    context.SignInLogs.Add(log);
                    context.SaveChanges();
                }
                return result;
            }
        }

        /// <summary>
        /// 创建持久Identity
        /// </summary>
        /// <param name="id"></param>
        /// <param name="expiresDate"></param>
        /// <param name="newToken"></param>
        public void CreatePersistentIdentity(SSOIdentity id, DateTime expiresDate, Guid? newToken = null)
        {
            Guid token = Guid.ParseExact(id.Token, "N");
            using (var context = base.CreateUserContext())
            {
                if (newToken == null)
                {
                    var dataObj = new PersistentSession()
                    {
                        Token = token,
                        UserID = id.UserID,
                        ExpiresDate = expiresDate
                    };
                    context.PersistentSessions.Add(dataObj);
                }
                else
                {
                    var dataObj = context.PersistentSessions.Where(t => t.Token == token).Single();
                    dataObj.Token = newToken.Value;
                }
                context.SaveChanges();
            }
        }

        /// <summary>
        /// 查询持久Identity
        /// </summary>
        /// <param name="sToken"></param>
        /// <returns></returns>
        public SSOIdentity QueryPersistentIdentity(string sToken)
        {
            Guid token = Guid.ParseExact(sToken, "N");
            using (var context = base.CreateUserContext())
            {
                var dataObj = context.PersistentSessions.Where(t => t.Token == token).SingleOrDefault();
                if (dataObj == null || dataObj.ExpiresDate < DateTime.Now)
                {
                    return null;
                }
                var q = from t in context.Accounts
                        where t.RowID == dataObj.UserID
                        select new SSOIdentity
                        {
                            UserID = t.RowID,
                            UserName = t.UserName,
                            Token = dataObj.Token.ToString("N"),
                            IssueDate = DateTime.Now,
                            IsAuthenticated = true
                        };
                var result = q.DefaultIfEmpty(new SSOIdentity()
                {
                    UserName = dataObj.Account.UserName,
                    IsAuthenticated = false
                }).Single();
                return result;
            }
        }
        #endregion

        #region OAuth
        public SSOIdentity OAuth(OAuthParameter param)
        {
            int kind = EnumToValue(param.OAuthKind);
            using (var context = base.CreateUserContext())
            {
                var q = from t in context.OpenOAuths
                        where t.OpenID == param.OpenID
                        && t.OAuthKind == kind
                        select t;
                var entity = q.SingleOrDefault();
                Guid userID = Guid.Empty;
                // 验证OAuth返回
                if (string.IsNullOrEmpty(param.UserName))
                {
                    if (entity == null)
                    {
                        return null;
                    }

                    var q2 = from t in context.Accounts
                             join t2 in context.OpenOAuths on t.RowID equals t2.UserID
                             where t2.OpenID == param.OpenID && t2.OAuthKind == kind
                             select new string[] { t.UserName, t.Password };
                    var args = q2.SingleOrDefault();
                    if (args == null)
                    {
                        throw new InvalidInvokeException("用户不存在");
                    }
                    param.UserName = args[0];
                    param.Password = args[1];
                }
                else
                {
                    // 没有帐号，绑定新帐号
                    if (param.UserName == param.OpenID)
                    {
                        param.UserName = CreateNewUserName(param.OpenID, param.OAuthKind);
                        if (!context.Accounts.Any(t => t.AppID == param.AppID && t.UserName == param.UserName))
                        {
                            this.SignUp(new SignUpParameter()
                            {
                                AppID = param.AppID,
                                UserName = param.UserName,
                                Password = param.Password
                            });
                            Thread.Sleep(200);
                        }
                        goto signIn;
                    }

                    param.Password = CryptoManaged.MD5Hex(param.Password);
                    var q2 = from t in context.Accounts
                             where t.AppID == param.AppID
                             && t.UserName == param.UserName && t.Password == param.Password
                             select t.RowID;
                    userID = q2.SingleOrDefault();
                    if (userID == Guid.Empty)
                    {
                        throw new InvalidInvokeException("帐号或密码错误");
                    }

                    var q3 = from t in context.OpenOAuths
                             where t.UserID == userID && t.OAuthKind == kind
                             select t;
                    if (q3.Any())
                    {
                        throw new InvalidInvokeException("已经绑定过其它账户");
                    }
                }
            signIn:
                var id = this.SignIn(param);
                if (entity == null)
                {
                    if (id.IsAuthenticated)
                    {
                        userID = id.UserID;
                    }
                    if (userID == Guid.Empty)
                    {
                        throw new InvalidInvokeException("UserID's null");
                    }
                    entity = new OpenOAuth();
                    EntityMapper.Map<OAuthParameter, OpenOAuth>(param, entity);
                    entity.UserID = userID;
                    entity.CreateDate = DateTime.Now;
                    context.OpenOAuths.Add(entity);
                    context.SaveChanges();
                }
                return id;
            }
        }

        private string CreateNewUserName(string openID, OAuthKind kind)
        {
            string userName = kind.ToString() + "_" + CryptoManaged.MD5Hash(openID);
            return userName;
        }
        #endregion

        #region Email
        /// <summary>
        /// 将验证邮件的验证码写入数据库
        /// </summary>
        /// <param name="param"></param>
        public void SendAuthEmail(SendAuthEmailParameter param)
        {
            using (var context = base.CreateUserContext())
            {
                if (param.Kind != AuthEmailKind.ChangeEmail && !context.Accounts.Any(t => t.AppID == param.AppID && t.Email == param.Email))
                {
                    throw new InvalidInvokeException("Email不存在");
                }

                string userName = context.Accounts.Where(t => t.AppID == param.AppID && t.RowID == param.UserID).Select(t => t.UserName).Single();
                Guid authCode = Guid.NewGuid();
                switch (param.Kind)
                {
                    case AuthEmailKind.SignUp:
                    case AuthEmailKind.ChangeEmail:
                        Utility.SendVerifyEmail(param.AppID, userName, param.Email, authCode);
                        break;
                    case AuthEmailKind.FindPassword:
                        Utility.SendFindPwdEmail(param.AppID, userName, param.Email, authCode);
                        break;
                }

                var entity = new EmailAuth();
                EntityMapper.Map<SendAuthEmailParameter, EmailAuth>(param, entity);
                entity.AuthKey = authCode.ToString();
                entity.CreateDate = DateTime.Now;
                entity.Status = (int)ActivationStatus.NotActive;
                context.EmailAuths.Add(entity);
                context.SaveChanges();
            }
        }

        /// <summary>
        /// 通过传递的验证数值 验证邮箱的真实性
        /// </summary>
        /// <param name="authCode"></param>
        public void VerifyEmail(Guid authCode)
        {
            using (var scope = DbScope.Create())
            using (var context = base.CreateUserContext())
            {
                scope.BeginTransaction();

                var entity = CheckUserEmailAuth(context, authCode);
                entity.Status = (int)ActivationStatus.Activated;
                context.SaveChanges();
                context.Accounts.Update(t => t.RowID == entity.UserID, t => new Account()
                {
                    Email = entity.Email,
                    Flags = t.Flags | (int)UserFlags.AuthenticEmail
                });

                scope.Complete();
            }
        }

        private EmailAuth CheckUserEmailAuth(InfrastructureService_UserEntities context, Guid authCode)
        {
            var entity = context.EmailAuths.SingleOrDefault(t => t.AuthKey == authCode.ToString());
            if (entity == null)
            {
                throw new InvalidInvokeException("当前记录不存在")
                {
                    FaultLevel = InvokeFaultLevel.SystemUnusual
                };
            }
            if ((ActivationStatus)entity.Status == ActivationStatus.Activated)
            {
                throw new InvalidInvokeException("已经通过验证");
            }
            if (!(entity.CreateDate.AddHours(-24) <= DateTime.Now && DateTime.Now <= entity.CreateDate.AddHours(24)))
            {
                throw new InvalidInvokeException("Email验证已过期");
            }
            return entity;
        }
        #endregion

        #region Mobile
        /// <summary>
        /// 将手机验证吗写入数据库
        /// </summary>
        /// <param name="param"></param>
        public void SendAuthMobile(SendAuthMobileParameter param)
        {
            using (var context = base.CreateUserContext())
            {
                if (context.MobileAuths.Where(t => t.Mobile == param.Mobile && t.CreateDate.Date == DateTime.Now.Date).Count() > 2)
                {
                    throw new InvalidInvokeException("1天内已经发送超过3次，不再发送");
                }
                var q = from t in context.MobileAuths
                        where t.UserName == param.UserName && t.Mobile == param.Mobile
                        orderby t.CreateDate descending
                        select t.CreateDate;
                DateTime first = q.FirstOrDefault();
                if (first != DateTime.MinValue)
                {
                    TimeSpan ts = DateTime.Now - first;
                    if (ts.TotalSeconds <= 60)
                    {
                        throw new InvalidInvokeException("1分钟内仅发送1次");
                    }
                }

                var entity = new MobileAuth();
                EntityMapper.Map<SendAuthMobileParameter, MobileAuth>(param, entity);
                Random rnd = new Random();
                entity.SmsCode = rnd.Next(1000, 9999).ToString();
                entity.CreateDate = DateTime.Now;
                entity.Status = (int)ActivationStatus.NotActive;
                context.MobileAuths.Add(entity);
                context.SaveChanges();

                switch (param.Kind)
                {
                    case AuthMobileKind.SignUp:
                        Utility.SendSignUpSMS(param.AppID, entity.Mobile, int.Parse(entity.SmsCode));
                        break;
                    case AuthMobileKind.FindPassword:
                        Utility.SendFindPwdSMS(param.AppID, entity.Mobile, int.Parse(entity.SmsCode));
                        break;
                }
            }
        }

        /// <summary>
        /// 验证手机
        /// </summary>
        /// <param name="smsCode"></param>
        /// <returns></returns>
        public void VerifyMobile(VerifyMobileParameter param)
        {
            using (var scope = DbScope.Create())
            using (var context = base.CreateUserContext())
            {
                scope.BeginTransaction();

                var entity = this.CheckUserMobileAuth(context, param.Mobile, param.SmsCode);
                entity.Status = (int)ActivationStatus.Activated;
                context.SaveChanges();
                context.Accounts.Update(t => t.UserName == entity.UserName, t => new Account()
                {
                    Mobile = entity.Mobile,
                    Flags = t.Flags | (int)UserFlags.AuthenticMobile
                });

                scope.Complete();
            }
        }

        private MobileAuth CheckUserMobileAuth(InfrastructureService_UserEntities context, string mobile, int authCode)
        {
            var entity = context.MobileAuths.Where(t => t.Mobile == mobile && t.SmsCode == authCode.ToString()).FirstOrDefault();
            if (entity == null)
            {
                throw new InvalidInvokeException("手机验证码不存在");
            }
            if ((ActivationStatus)entity.Status == ActivationStatus.Activated)
            {
                throw new InvalidInvokeException("已经通过验证");
            }
            if (!(entity.CreateDate.AddHours(-1) <= DateTime.Now && DateTime.Now <= entity.CreateDate.AddHours(1)))
            {
                throw new InvalidInvokeException("手机验证码已过期");
            }
            return entity;
        }
        #endregion

        #region Info
        public void SendFindPwdCode(SendFindPwdCodeParameter param)
        {
            using (var context = base.CreateUserContext())
            {
                bool isMobile = StringHelper.IsMobile(param.EmailOrMobile);
                var q = from t in context.Accounts
                        where t.AppID == param.AppID
                        && (isMobile ? t.Mobile == param.EmailOrMobile : t.Email == param.EmailOrMobile)
                        select t;
                var user = q.SingleOrDefault();
                if (user == null)
                {
                    throw new InvalidInvokeException("Email或手机未注册");
                }
                if (isMobile)
                {
                    var flags = (UserFlags)user.Flags;
                    if ((flags & UserFlags.AuthenticMobile) != UserFlags.AuthenticMobile)
                    {
                        throw new InvalidInvokeException("手机未验证");
                    }
                    this.SendAuthMobile(new SendAuthMobileParameter()
                    {
                        AppID = user.AppID,
                        UserName = user.UserName,
                        Mobile = param.EmailOrMobile,
                        Kind = AuthMobileKind.FindPassword
                    });
                }
                else
                {
                    this.SendAuthEmail(new SendAuthEmailParameter()
                    {
                        AppID = user.AppID,
                        UserID = user.RowID,
                        Email = param.EmailOrMobile,
                        Kind = AuthEmailKind.FindPassword
                    });
                }
            }
        }

        public void ChangePassword(ChangePasswordParameter param)
        {
            using (var context = base.CreateUserContext())
            {
                EmailAuth emailAuth = null;
                MobileAuth mobileAuth = null;
                if (param.AuthCode != null)
                {
                    Guid emailAuthCode;
                    if (Guid.TryParse(param.AuthCode, out emailAuthCode))
                    {
                        emailAuth = this.CheckUserEmailAuth(context, emailAuthCode);
                    }
                    else
                    {
                        string[] mobileAuthCode = param.AuthCode.Split(',');
                        if (mobileAuthCode.Length != 2)
                        {
                            throw new InvalidInvokeException("参数错误");
                        }
                        mobileAuth = this.CheckUserMobileAuth(context, mobileAuthCode[0], int.Parse(mobileAuthCode[1]));
                        param.UserName = mobileAuth.UserName;
                    }
                }

                var id = this.SignIn(new SignInParameter()
                {
                    AppID = param.AppID,
                    UserName = param.UserName,
                    Password = param.OldPassword
                });
                if (!id.IsAuthenticated)
                {
                    throw new InvalidInvokeException("账户不存在或密码错误");
                }

                using (var scope = DbScope.Create())
                {
                    scope.BeginTransaction();

                    param.NewPassword = CryptoManaged.MD5Hex(param.NewPassword);
                    context.Accounts.Update(t => t.RowID == id.UserID, t => new Account() { Password = param.NewPassword });
                    if (emailAuth != null)
                    {
                        emailAuth.Status = (int)ActivationStatus.Activated;
                    }
                    if (mobileAuth != null)
                    {
                        mobileAuth.Status = (int)ActivationStatus.Activated;
                    }
                    context.SaveChanges();

                    scope.Complete();
                }
            }
        }

        public QueryUsersResult QueryUsers(QueryUsersParameter param)
        {
            var result = new QueryUsersResult();
            using (var context = base.CreateUserContext())
            {
                var q = from t in context.Accounts
                        where t.AppID == param.AppID
                        && (param.UserID == null || t.RowID == param.UserID)
                        && (string.IsNullOrEmpty(param.Keyword) || (t.UserName.Contains(param.Keyword) || t.Email.Contains(param.Keyword)))
                        select new UserEntity
                        {
                            UserID = t.RowID,
                            UserName = t.UserName,
                            Email = t.Email,
                            Mobile = t.Mobile,
                            Flags = (UserFlags)t.Flags,
                            CreateDate = t.CreateDate
                        };
                if (param.OrderBy == QueryUsersOrderBy.CreateDateDesc)
                {
                    q = q.OrderBy(t => t.CreateDate);
                }
                else
                {
                    q = q.OrderByDescending(t => t.CreateDate);
                }
                result.PageResult(q, param);
            }
            return result;
        }

        public QuerySignInLogsResult QuerySignInLogs(QuerySignInLogsParameter param)
        {
            var result = new QuerySignInLogsResult();
            using (var context = base.CreateUserContext())
            {
                var q = from t in context.SignInLogs
                        where (param.IsSuccess == null || t.IsSuccess == param.IsSuccess)
                        && (string.IsNullOrEmpty(param.UserName) || t.UserName == param.UserName)
                        select new QuerySignInLogsResult.TResult
                        {
                            UserName = t.UserName,
                            ClientIP = t.ClientIP,
                            Platform = t.Platform,
                            SignInDate = t.SignInDate,
                            IsSuccess = t.IsSuccess
                        };
                result.SignInCount = q.Where(t => t.IsSuccess).Count();
                result.PageResult(q, param);
            }
            return result;
        }
        #endregion
    }
}