using System;
using System.Web;
using System.Configuration;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

namespace Rocky.Web
{
    public class RewriterConfigSerializerSectionHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, XmlNode section)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(RewriterConfiguration));
            return serializer.Deserialize(new XmlNodeReader(section));
        }
    }

    [Serializable, XmlRoot("RewriterConfig")]//这里可以随便定义，主要通过这个名字的节点来获取正则匹配。
    public class RewriterConfiguration
    {
        public static readonly string ExecuteExtension;
        private static RewriterConfiguration instance;

        static RewriterConfiguration()
        {
            ExecuteExtension = ConfigurationManager.AppSettings["RewriteExtension"];
            if (string.IsNullOrEmpty(ExecuteExtension))
            {
                ExecuteExtension = ".htm";
            }
            instance = (RewriterConfiguration)ConfigurationManager.GetSection("RewriterConfig");
        }

        public static RewriterConfiguration GetConfig()
        {
            return instance;
        }

        public RewriterRuleCollection Rules { get; set; }
    }

    [Serializable]
    public class RewriterRuleCollection : CollectionBase
    {
        public RewriterRule this[int index]
        {
            get { return (RewriterRule)base.InnerList[index]; }
            set { base.InnerList[index] = value; }
        }

        public virtual void Add(RewriterRule rule)
        {
            base.InnerList.Add(rule);
        }
    }

    [Serializable]
    public class RewriterRule
    {
        internal const string Http = "http://";

        /// <summary>
        /// 是否是2级重写
        /// <example>http://*.domain.cn</example>
        /// </summary>
        public bool IsCrossdomain
        {
            get { return LookFor.StartsWith(Http); }
        }
        public string LookFor { get; set; }
        public string SendTo { get; set; }
    }

    #region RewriterUtils
    internal class RewriterUtils
    {
        public static string ResolveUrl(string appPath, string url)
        {
            if ((url.Length == 0) || (url[0] != '~'))
            {
                return url;
            }
            if (url.Length == 1)
            {
                return appPath;
            }
            if ((url[1] == '/') || (url[1] == '\\'))
            {
                if (appPath.Length > 1)
                {
                    return (appPath + "/" + url.Substring(2));
                }
                return ("/" + url.Substring(2));
            }
            if (appPath.Length > 1)
            {
                return (appPath + "/" + url.Substring(1));
            }
            return (appPath + url.Substring(1));
        }

        public static void RewriteUrl(HttpContext context, string sendToUrl)
        {
            string qstring, path;
            RewriteUrl(context, sendToUrl, out qstring, out path);
        }

        public static void RewriteUrl(HttpContext context, string sendToUrl, out string sendToUrlLessQString, out string filePath)
        {
            if (context.Request.QueryString.Count > 0)
            {
                if (sendToUrl.IndexOf('?') != -1)
                {
                    sendToUrl = sendToUrl + "&" + context.Request.QueryString.ToString();
                }
                else
                {
                    sendToUrl = sendToUrl + "?" + context.Request.QueryString.ToString();
                }
            }
            string queryString = string.Empty;
            sendToUrlLessQString = sendToUrl;
            if (sendToUrl.IndexOf('?') > 0)
            {
                sendToUrlLessQString = sendToUrl.Substring(0, sendToUrl.IndexOf('?'));
                queryString = sendToUrl.Substring(sendToUrl.IndexOf('?') + 1);
            }
            filePath = context.Server.MapPath(sendToUrlLessQString);
            context.RewritePath(sendToUrlLessQString, string.Empty, queryString);
        }
    }
    #endregion
}