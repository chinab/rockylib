using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rocky.Net
{
    public static class xHttpServer
    {
        private const string ProcessName = "CassiniDev4-console.exe";

        static xHttpServer()
        {
            string resourceName = string.Format("Rocky.Resource.{0}", ProcessName),
                filePath = Runtime.CombinePath(ProcessName);
            Runtime.CreateFileFromResource(resourceName, filePath);
            Runtime.CreateFileFromResource(resourceName + ".config", filePath + ".config");
        }

        /// <summary>
        /// IIS经典管道
        /// </summary>
        /// <param name="applicationPath"></param>
        /// <param name="serverUrl"></param>
        public static void Start(string applicationPath, out Uri serverUrl)
        {
            string filePath = Runtime.CombinePath(ProcessName),
                args = string.Format("/a:{0} /im:Any /pm:Specific /p:80 /t:0", applicationPath);
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
    }
}