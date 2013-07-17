﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using InfrastructureService.DomainService;

namespace InfrastructureService.AppService
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
#if !DEBUG
            StorageService.Create();
#endif
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            Hub.LogError(ex, "Global");
            Server.ClearError();
            var app = (HttpApplication)sender;
            app.Response.Clear();
            app.Response.StatusCode = 403;
            app.Response.End();
        }

        protected void Application_End(object sender, EventArgs e)
        {
#if !DEBUG
            StorageService.Create(true);
#endif
        }
    }
}