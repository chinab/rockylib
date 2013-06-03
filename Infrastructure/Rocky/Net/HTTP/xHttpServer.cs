using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Web;

namespace Rocky.Net
{
    public class xHttpServer : IHttpHandler
    {
        #region Fields
        private const string ProcessName = "CassiniDev4-console.exe";
        private static readonly string[] ServerList;
        #endregion

        #region Constructors
        static xHttpServer()
        {
            var q = from t in (ConfigurationManager.AppSettings["Agent-Server"] ?? string.Empty).Split(',')
                    where !string.IsNullOrEmpty(t)
                    select t;
            ServerList = q.ToArray();
        }
        #endregion

        #region Methods
        internal static object GetRandom(Array set)
        {
            var rnd = new Random();
            int which = rnd.Next(0, set.Length);
            return set.GetValue(which);
        }

        /// <summary>
        /// IIS经典管道
        /// </summary>
        /// <param name="applicationPath"></param>
        /// <param name="serverUrl"></param>
        public static void Start(string applicationPath, out Uri serverUrl)
        {
            string resourceName = string.Format("Rocky.Resource.{0}", ProcessName),
                filePath = Runtime.CombinePath(ProcessName);
            Runtime.CreateFileFromResource(resourceName, filePath);
            Runtime.CreateFileFromResource(resourceName + ".config", filePath + ".config");

            string args = string.Format("/a:{0} /im:Any /pm:Specific /p:80 /t:0", applicationPath);
            var proc = Process.Start(new ProcessStartInfo(filePath, args)
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            string result = proc.StandardOutput.ReadLine();
            // 监听80端口失败
            if (!result.StartsWith("started:"))
            {
                proc.Kill();
                proc.Close();
                proc = Process.Start(new ProcessStartInfo(filePath, string.Format(@"/a:{0} /im:Any /t:0", applicationPath))
                {
                    CreateNoWindow = false,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });
                //started: http://localhost:32768/
                result = proc.StandardOutput.ReadLine();
                if (!result.StartsWith("started:"))
                {
                    throw new InvalidOperationException(result);
                }
            }
            serverUrl = new Uri(result.Substring(8));
            Runtime.DisposeService.Register(typeof(xHttpServer), proc);
        }
        #endregion

        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            if (ServerList.Length == 0)
            {
                context.Response.Close();
                return;
            }

            context.Response.Write(GetRandom(ServerList));
        }
    }
}