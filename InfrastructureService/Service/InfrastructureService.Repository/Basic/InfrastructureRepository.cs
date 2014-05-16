using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using InfrastructureService.Common;
using InfrastructureService.Model.Basic;
using InfrastructureService.Repository.DataAccess;
using System.Threading;
using System.Data;

namespace InfrastructureService.Repository.Basic
{
    public class InfrastructureRepository : RepositoryBase
    {
        #region Constructors
        public InfrastructureRepository(MessageConfig config = null)
        {
            _config = config ?? MessageConfig.Default;

            if (_config.ResendFailEmail)
            {
                if (_job == null)
                {
                    _job = new JobTimer(t =>
                    {
                        var repository = new InfrastructureRepository();
                        repository.ResendFailEmail();
                    }, TimeSpan.FromMinutes(20D));
                }
                _job.Start();
            }
            else
            {
                if (_job != null)
                {
                    _job.Stop();
                }
            }
            //new JobTimer(t =>
            //{
            //    using (var mgr = new MessageRepository())
            //    {
            //        decimal balance = mgr.GetSMSBalance(new Guid("1950ADD2-6001-4738-9E10-6817F1128B78"));
            //        if (balance < 50M)
            //        {
            //            mgr.SendEmail(new SendEmailParameter()
            //            {
            //                AppID = new Guid("B200553E-BF3A-4EE6-9B24-454A523CE236"),
            //                Subject = "短信网关可用余额不足",
            //                Body = string.Format("短信网关可用余额不足，可用余额：{0}", balance),
            //                Recipients = new string[] { "wangxiaoming@0710.com" }
            //            });
            //        }
            //    }
            //}, TimeSpan.FromMinutes(10D)).Start();
        }
        #endregion

        #region Message
        #region Fields
        private static JobTimer _job;
        private MessageConfig _config;
        #endregion

        #region Email
        public void SendEmail(SendEmailParameter param)
        {
            var client = GetMailClient(param);
            foreach (string to in param.Recipients)
            {
                client.AddTo(to, string.Empty);
            }
            client.SetBody(param.Subject, param.Body);
            object userToken = null;
            if (_config.LogEmail)
            {
                using (var context = base.CreateContext())
                {
                    var pObj = new EmailMessage();
                    EntityMapper.Map<SendEmailParameter, EmailMessage>(param, pObj);
                    pObj.RowID = Guid.NewGuid();
                    pObj.From = client.From.Address;
                    pObj.To = string.Join(";", param.Recipients);
                    pObj.CreateDate = DateTime.Now;
                    pObj.Status = (int)MessageStatusKind.Unsent;
                    context.EmailMessages.Add(pObj);
                    context.SaveChanges();

                    userToken = pObj.RowID;
                }
                client.SendCompleted += new System.Net.Mail.SendCompletedEventHandler(sender_SendCompleted);
            }

            client.SendAsync(userToken);
        }
        void sender_SendCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Guid rowID = (Guid)e.UserState;
            using (var context = base.CreateContext())
            {
                var msg = context.EmailMessages.Single(t => t.RowID == rowID);
                if (e.Cancelled)
                {
                    msg.Status = (int)MessageStatusKind.Cancelled;
                }
                else if (e.Error != null)
                {
                    App.LogError(e.Error, "SendEmail");
                    msg.Status = (int)MessageStatusKind.Error;
                }
                else
                {
                    msg.Status = (int)MessageStatusKind.OK;
                    msg.SendDate = DateTime.Now;
                }
                context.SaveChanges();
            }
        }

        private MailClient GetMailClient(SendEmailParameter header)
        {
            base.VerifyHeader(header);

            using (var context = base.CreateContext())
            {
                var config = context.EmailConfigs.First(t => header.ConfigID == null || t.RowID == header.ConfigID);
                var client = new MailClient();
                var arr = config.SmtpAuthority.Split(':');
                client.Config(arr[0], arr.Length == 2 ? int.Parse(arr[1]) : 25, config.EnableSsl, config.UserName, config.Password);
                client.SetFrom(config.FromEmail, config.FromDisplayName);
                return client;
            }
        }

        public void ResendFailEmail()
        {
            using (var context = base.CreateContext())
            {
                int status = EnumToValue(MessageStatusKind.Error);
                DateTime start = DateTime.Now.AddDays(-1D), end = DateTime.Now.AddDays(1D);
                var q = from t in context.EmailMessages
                        where t.Status == status
                        && start <= t.CreateDate && t.CreateDate <= end
                        select t;
                foreach (var item in q)
                {
                    var param = new SendEmailParameter();
                    param.AppID = item.AppID;
                    param.ConfigID = _config.ResendFailEmailConfigID;
                    param.Recipients = item.To.Split(';');
                    param.Subject = item.Subject;
                    param.Body = item.Body;

                    var sender = GetMailClient(param);
                    foreach (string to in param.Recipients)
                    {
                        sender.AddTo(to, string.Empty);
                    }
                    sender.SetBody(param.Subject, param.Body);
                    sender.SendCompleted += new System.Net.Mail.SendCompletedEventHandler(sender_SendCompleted);
                    sender.SendAsync(item.RowID);

                    Thread.Sleep(200);
                }
            }
        }
        #endregion

        #region SMS
        public void SendSMS(SendSMSParameter param)
        {
            var client = GetSMSSender(param);
            client.Form["to"] = param.ReceiveMobile;
            client.Form["content"] = param.SendMessage;
            client.Form["time"] = string.Empty;
            client.SetRequest(client.BuildUri(client.Form["url"], client.Form));
            using (var context = base.CreateContext())
            {
                int maxSentPreDay = 3;
                var q = from t in context.SMSMessages
                        where t.ReceiveMobile == param.ReceiveMobile && t.AppID == param.AppID
                        && DateTime.UtcNow.AddDays(-1D) <= t.SendDate && t.SendDate <= DateTime.UtcNow.AddDays(1D)
                        select t;
                int count = q.Count();
                if (count > maxSentPreDay)
                {
                    throw new DomainException("SendSMS");
                }

                var pObj = new SMSMessage();
                EntityMapper.Map<SendSMSParameter, SMSMessage>(param, pObj);
                pObj.RowID = Guid.NewGuid();
                pObj.CreateDate = DateTime.Now;
                pObj.Status = (int)MessageStatusKind.Unsent;
                context.SMSMessages.Add(pObj);
                context.SaveChanges();
                TaskHelper.Factory.StartNew(sender_SendCompleted, new object[] { pObj.RowID, client });
            }
        }
        void sender_SendCompleted(object state)
        {
            var arr = (object[])state;
            Guid rowID = (Guid)arr[0];
            using (var context = base.CreateContext())
            {
                var msg = context.SMSMessages.Single(t => t.RowID == rowID);
                msg.SendDate = DateTime.Now;
                try
                {
                    var sender = (HttpClient)arr[1];
                    msg.ServiceReturn = sender.GetResponse().GetResponseText();
                    msg.Status = (int)MessageStatusKind.OK;
                    msg.SendDate = DateTime.Now;
                }
                catch
                {
                    msg.Status = (int)MessageStatusKind.Error;
                    throw;
                }
                finally
                {
                    context.SaveChanges();
                }
            }
        }

        private HttpClient GetSMSSender(SendSMSParameter header)
        {
            base.VerifyHeader(header);

            using (var context = base.CreateContext())
            {
                var config = context.SMSConfigs.First(t => header.ConfigID == null || t.RowID == header.ConfigID);
                if (!string.IsNullOrEmpty(config.Sign))
                {
                    header.SendMessage += "【" + config.Sign + "】";
                }
                var client = new HttpClient();
                client.Form["url"] = config.WebAuthority;
                client.Form["id"] = config.UserName;
                client.Form["pwd"] = config.Password;
                return client;
            }
        }

        public decimal GetSMSBalance(Guid configID)
        {
            //using (var context = base.CreateContext())
            //{
            //    var config = context.SMSConfigs.First(t => t.RowID == configID);
            //    var sender = new HttpClient(new Uri(config.WebAuthority));
            //    sender.Form["mark"] = "balance";
            //    sender.Form["username"] = config.UserName;
            //    sender.Form["password"] = config.Password;
            //    sender.Form["fstd"] = "5";
            //    string result = sender.GetResponse().GetResponseText();
            //    var alive = System.Text.RegularExpressions.Regex.Split(result, "&lt;br/>")[0].Split(':');
            //    if (alive.Length != 2)
            //    {
            //        return 0M;
            //    }
            //    return decimal.Parse(alive[1]);
            //}
            return decimal.MinusOne;
        }
        #endregion
        #endregion

        #region File
        public static string RootPath
        {
            get { return App.CombinePath(@"Storage\"); }
        }
        public static System.Net.IPAddress LocalIP
        {
            get
            {
                return SocketHelper.GetHostAddresses().First(); //局域网内IP4地址
            }
        }

        public bool ExistFile(QueryFileParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.FileStorages
                        where t.FileKey == param.FileKey
                        select t;
                return q.Any();
            }
        }

        private string GetVirtualPath(string physicalPath)
        {
            return physicalPath.Substring(RootPath.Length - 1).Replace(@"\", @"/");
        }

        public void SaveFile(string checksum, string fileName, string physicalPath)
        {
            using (var context = base.CreateContext())
            {
                var pObj = context.FileStorages.Where(t => t.FileKey == checksum).SingleOrDefault();
                if (pObj == null)
                {
                    pObj = new FileStorage();
                    pObj.CreateDate = DateTime.Now;
                    context.FileStorages.Add(pObj);
                }
                pObj.PhysicalPath = physicalPath;
                pObj.VirtualPath = this.GetVirtualPath(physicalPath);
                pObj.ServerAuthority = LocalIP.ToString();
                context.SaveChanges();
            }
        }

        public QueryFileResult QueryFile(QueryFileParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.FileStorages
                        where t.FileKey == param.FileKey
                        select new QueryFileResult
                        {
                            FileKey = t.FileKey,
                            FileName = t.FileName,
                            CreateDate = t.CreateDate
                        };
                return q.Single();
            }
        }

        public QueryFilePathResult QueryFilePath(QueryFileParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.FileStorages
                        where t.FileKey == param.FileKey
                        select new QueryFilePathResult
                        {
                            VirtualPath = t.VirtualPath,
                            PhysicalPath = t.PhysicalPath,
                            ServerAuthority = t.ServerAuthority
                        };
                return q.Single();
            }
        }
        #endregion
    }
}