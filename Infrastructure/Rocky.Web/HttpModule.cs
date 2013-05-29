using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;

namespace Rocky.Web
{
    public class HttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += new EventHandler(context_BeginRequest);
        }

        void context_BeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = (HttpApplication)sender;
            if (IsUploadRequest(app.Context))
            {
                return;
            }

            #region Rewriter
            RewriterConfiguration config = RewriterConfiguration.GetConfig();
            if (config != null)
            {
                bool is404 = app.Request.Path.EndsWith(RewriterConfiguration.ExecuteExtension, StringComparison.OrdinalIgnoreCase);
                RewriterRuleCollection rules = config.Rules;
                for (int i = 0; i < rules.Count; i++)
                {
                    string requestedPath = null, lookFor = null;
                    if (rules[i].IsCrossdomain)
                    {
                        //二级域名重写
                        requestedPath = app.Request.Url.AbsoluteUri;
                        lookFor = String.Concat("^", rules[i].LookFor, "$");
                    }
                    else
                    {
                        //一般重写
                        requestedPath = app.Request.Path;
                        lookFor = String.Concat("^", RewriterUtils.ResolveUrl(app.Context.Request.ApplicationPath, rules[i].LookFor), "$");
                    }
                    Regex reg = new Regex(lookFor, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (reg.IsMatch(requestedPath))
                    {
                        is404 = false;
                        string sendToUrl = RewriterUtils.ResolveUrl(app.Context.Request.ApplicationPath, reg.Replace(requestedPath, rules[i].SendTo));
#if DEBUG
                        app.Context.Trace.Write("ModuleRewriter", String.Concat("Rewriting URL to ", sendToUrl));
#endif
                        RewriterUtils.RewriteUrl(app.Context, sendToUrl);
                        break;
                    }
                }
                if (is404)
                {
                    app.Response.StatusCode = 404;
                    app.Response.SuppressContent = true;
                    app.Response.End();
                }
            }
            #endregion
        }

        private bool IsUploadRequest(HttpContext context)
        {
            return context.Request.ContentType.IndexOf("multipart/form-data", StringComparison.OrdinalIgnoreCase) == 0;
        }

        public void Dispose()
        {

        }
    }
}