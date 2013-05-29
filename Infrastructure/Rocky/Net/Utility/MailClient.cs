using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.IO;

namespace Rocky.Net
{
    public class MailClient : IDisposable
    {
        #region NestedTypes
        public enum SystemMail
        {
            Gmail,
            Yahoo
        }
        #endregion

        #region Fields
        private SmtpClient _client;
        private NetworkCredential _credential;
        private MailMessage _message;
        #endregion

        #region Properties
        public event SendCompletedEventHandler SendCompleted
        {
            add
            {
                _client.SendCompleted += value;
            }
            remove
            {
                _client.SendCompleted -= value;
            }
        }

        public MailAddress From
        {
            get { return _message.From; }
        }
        #endregion

        #region Constructors
        public MailClient()
        {
            _client = new SmtpClient();
            _client.UseDefaultCredentials = false;
            _client.Credentials = _credential = new NetworkCredential();
            _message = new MailMessage();
            _message.IsBodyHtml = true;
        }
        #endregion

        #region Methods
        public void Config(SystemMail which, string pwd)
        {
            switch (which)
            {
                case SystemMail.Gmail:
                    this.Config("smtp.gmail.com", 587, true, "ilovehaley.kid@gmail.com", pwd);
                    this.SetFrom("ilovehaley.kid@gmail.com", "TGrid");
                    break;
                case SystemMail.Yahoo:
                    this.Config("smtp.mail.yahoo.com", 25, false, "wxm395115323", pwd);
                    this.SetFrom("wxm395115323@yahoo.com.cn", "TGrid");
                    break;
            }
        }
        public void Config(string host, int port, bool enableSsl, string userName, string password)
        {
            _client.Host = host;
            _client.Port = port;
            _client.EnableSsl = enableSsl;
            _credential.UserName = userName;
            _credential.Password = password;
        }

        public void SetFrom(string fromAddress, string fromDisplayName = null)
        {
            _message.From = new MailAddress(fromAddress, fromDisplayName);
        }

        public void SetBody(string subject, string body, string[] attachmentPaths = null)
        {
            _message.Subject = subject;
            _message.Body = body;
            if (!attachmentPaths.IsNullOrEmpty())
            {
                foreach (string path in attachmentPaths)
                {
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException(path);
                    }

                    _message.Attachments.Add(new Attachment(path, MediaTypeNames.Application.Octet));
                }
            }
        }
        public void AddLogo(string imagePath)
        {
            string htmlview = "<html><body><table border=2><tr width=100%><td><img src=cid:Logo alt=companyname /></td></tr></table><hr/></body></html>";
            AlternateView view = AlternateView.CreateAlternateViewFromString(htmlview, null, MediaTypeNames.Text.Html);
            LinkedResource logo = new LinkedResource(imagePath);
            logo.ContentId = "Logo";
            view.LinkedResources.Add(logo);
            _message.AlternateViews.Add(view);
            _message.IsBodyHtml = true;
            _message.DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure;
        }

        public void AddTo(string recipients, string recipientsDisplayName = null)
        {
            _message.To.Add(new MailAddress(recipients, recipientsDisplayName));
        }

        public void Send()
        {
            _client.Send(_message);
        }
        public void SendAsync(object userToken = null)
        {
            _client.SendAsync(_message, userToken);
        }

        public void Dispose()
        {
            _client.Dispose();
            _message.Dispose();
        }
        #endregion
    }
}