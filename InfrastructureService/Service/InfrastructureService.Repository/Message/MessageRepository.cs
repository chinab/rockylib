using System;
using System.Collections.Generic;
using System.Data.Objects.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using InfrastructureService.Common;
using InfrastructureService.Model.Message;
using InfrastructureService.Repository.DataAccess;
using Rocky;
using Rocky.Net;

namespace InfrastructureService.Repository.Message
{
    public class MessageRepository : RepositoryBase
    {
        #region Fields
        private static JobTimer _job;
        private MessageConfig _config;
        #endregion

        #region Constructors
        public MessageRepository(MessageConfig config = null)
        {
            _config = config ?? MessageConfig.Default;

            if (_config.ResendFailEmail)
            {
                if (_job == null)
                {
                    _job = new JobTimer(t =>
                    {
                        var repository = new MessageRepository();
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
                    Runtime.LogError(e.Error, "SendEmail");
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
            client.Form["mobile"] = param.ReceiveMobile;
            client.Form["content"] = param.SendMessage;
            client.Form["sendtime"] = string.Empty;
            client.Form["fstd"] = "5";
            using (var context = base.CreateContext())
            {
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
                var client = new HttpClient(new Uri(config.WebAuthority));
                client.Form["mark"] = "send";
                client.Form["username"] = config.UserName;
                client.Form["password"] = config.Password;
                if (!string.IsNullOrEmpty(config.Sign))
                {
                    header.SendMessage += "【" + config.Sign + "】";
                }
                return client;
            }
        }

        public decimal GetSMSBalance(Guid configID)
        {
            using (var context = base.CreateContext())
            {
                var config = context.SMSConfigs.First(t => t.RowID == configID);
                var sender = new HttpClient(new Uri(config.WebAuthority));
                sender.Form["mark"] = "balance";
                sender.Form["username"] = config.UserName;
                sender.Form["password"] = config.Password;
                sender.Form["fstd"] = "5";
                string result = sender.GetResponse().GetResponseText();
                var alive = System.Text.RegularExpressions.Regex.Split(result, "&lt;br/>")[0].Split(':');
                if (alive.Length != 2)
                {
                    return 0M;
                }
                return decimal.Parse(alive[1]);
            }
        }
        #endregion
    }
}