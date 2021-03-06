﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;

namespace System.Agent.WinService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        protected override void OnAfterInstall(IDictionary savedState)
        {
            base.OnAfterInstall(savedState);
            try
            {
                string command = String.Format("sc config {0} type= own type= interact", this.serviceInstaller1.ServiceName);
                var processInfo = new ProcessStartInfo()
                {
                    //Shell Command
                    FileName = "cmd",
                    //pass command as argument./c means carries 
                    //out the task and then terminate the shell command
                    Arguments = "/c" + command,
                    //To redirect The Shell command output to process stanadrd output
                    RedirectStandardOutput = true,
                    // Now Need to create command window. 
                    //Also we want to mimic this call as normal .net call
                    UseShellExecute = false,
                    // Do not show command window
                    CreateNoWindow = true
                };
                var process = Process.Start(processInfo);
                var output = process.StandardOutput.ReadToEnd();
                App.LogInfo(output);
            }
            catch (Exception ex)
            {
                App.LogError(ex, "设置服务允许与桌面交互");
            }
        }

        protected override void OnCommitted(IDictionary savedState)
        {
            base.OnCommitted(savedState);
            //Auot start service after the installation is completed
            var sc = new ServiceController(this.serviceInstaller1.ServiceName);
            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                sc.Start();
            }
        }
    }
}