using System;
using System.Web.SessionState;
using System.Net;
using System.IO;
using System.Drawing;

namespace System.Web
{
    public class ValidateHandler : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable
        {
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            int start = 16, end = context.Request.Path.IndexOf(".ashx");
            string id = context.Request.Path.Substring(start, end - start);
            int length = int.Parse(context.Request.QueryString["len"]);
            Image bg = null;
            string bgUrl = context.Request.QueryString["bg"];
            if (!string.IsNullOrEmpty(bgUrl))
            {
                using (WebClient client = new WebClient())
                {
                    bg = Image.FromStream(new MemoryStream(client.DownloadData(bgUrl)));
                }
            }

            context.Response.ClearContent();
            context.Response.ContentType = "image/gif";
            Image result = ValidateGenerator.Generator.Generate(id, length, bg);
            result.Save(context.Response.OutputStream, System.Drawing.Imaging.ImageFormat.Gif);
            context.Response.End();
        }
    }
}