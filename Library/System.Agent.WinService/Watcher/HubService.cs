using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace System.Agent.WinService
{
    partial class HubService : ServiceBase
    {
        private volatile bool _isStop;
        private string[] _filePaths;

        public HubService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // TODO: 在此处添加代码以启动服务。
            var q = from p in (ConfigurationManager.AppSettings["ProcessPath"] ?? string.Empty).Split(',')
                    where File.Exists(p)
                    select p;
            _filePaths = q.ToArray();
            ScanProcess();
        }

        private void ScanProcess()
        {
            var q = from p in Process.GetProcesses()
                    where !p.ProcessName.Contains("System")
                    select p;
            foreach (string exec in _filePaths)
            {
                try
                {
                    var proc = q.Where(p => p.MainModule.FileName.Equals(exec, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (proc == null)
                    {
                        proc = Process.Start(exec);
                    }
                    TaskHelper.Factory.StartNew(() =>
                    {
                        try
                        {
                            while (!_isStop)
                            {
                                proc.WaitForExit();
                                proc.Close();
                                proc.StartInfo.FileName = exec;
                                proc.Start();
                            }
                        }
                        catch (Exception ex)
                        {
                            Hub.LogError(ex, "WatchProcess");
                        }
                    });
                }
                catch (Exception ex)
                {
                    Hub.LogError(ex, "ScanProcess");
                }
            }
        }

        protected override void OnStop()
        {
            // TODO: 在此处添加代码以执行停止服务所需的关闭操作。
            _isStop = true;
        }
    }
}