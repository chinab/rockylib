using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Net;
using InfrastructureService.Repository.Basic;
using InfrastructureService.Model.Basic;

namespace InfrastructureService.Repository.User
{
    public static class Utility
    {
        private static string WebHostUrl = ConfigurationManager.AppSettings["WebHost"] + "/";

        public static void SendVerifyEmail(Guid appID, string userName, string recipient, Guid authCode)
        {
            string url = WebHostUrl + "Resources/Email/Active.htm";
            var client = new HttpClient(new Uri(url));
            StringBuilder body = new StringBuilder(client.GetResponse().GetResponseText());
            //TODO:替换变量
            body.Replace("{$UserName$}", userName);
            body.Replace("{$SiteUrl$}", WebHostUrl);
            body.Replace("{$FindUrl$}", WebHostUrl + "Account/VerifyEmail.aspx?authCode=" + authCode.ToString());
            var svc = new InfrastructureRepository();
            svc.SendEmail(new SendEmailParameter()
            {
                AppID = appID,
                Recipients = new string[] { recipient },
                Subject = "注册激活.",
                Body = body.ToString()
            });
        }

        public static void SendFindPwdEmail(Guid appID, string userName, string recipient, Guid authCode)
        {
            string url = WebHostUrl + "Resources/Email/FindPwd.htm";
            var client = new HttpClient(new Uri(url));
            StringBuilder body = new StringBuilder(client.GetResponse().GetResponseText());
            //TODO:替换变量
            body.Replace("{$UserName$}", userName);
            body.Replace("{$SiteUrl$}", WebHostUrl);
            body.Replace("{$FindUrl$}", WebHostUrl + "Account/ChangePwd.aspx?authCode=" + authCode.ToString());
            var svc = new InfrastructureRepository();
            svc.SendEmail(new SendEmailParameter()
            {
                AppID = appID,
                Recipients = new string[] { recipient },
                Subject = "找回密码帐户,请查收.",
                Body = body.ToString()
            });
        }

        public static void SendSignUpSMS(Guid appID, string receiveMobile, int smsCode)
        {
            string msg = "您的手机验证码是" + smsCode + "，请在页面填写验证码完成验证。如非本人操作，可不予理会，谢谢。";
            var svc = new InfrastructureRepository();
            svc.SendSMS(new SendSMSParameter()
            {
                AppID = appID,
                ReceiveMobile = receiveMobile,
                SendMessage = msg
            });
        }

        public static void SendFindPwdSMS(Guid appID, string receiveMobile, int smsCode)
        {
            string msg = "您的密码修改网址是 " + WebHostUrl + "?authCode=" + receiveMobile + "," + smsCode + " ，请浏览页面完成修改。如非本人操作，可不予理会，谢谢。";
            var svc = new InfrastructureRepository();
            svc.SendSMS(new SendSMSParameter()
            {
                AppID = appID,
                ReceiveMobile = receiveMobile,
                SendMessage = msg
            });
        }
    }
}