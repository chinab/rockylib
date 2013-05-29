using System;
using System.Web;
using System.Web.UI;
using System.Diagnostics;

namespace Rocky.Web.Controls
{
    [ToolboxData("<{0}:ArticlePager runat=\"server\"></{0}:ArticlePager>")]
    public class ArticlePager : WebPager
    {
        /// <summary>
        /// 分隔符
        /// </summary>
        public string Deal
        {
            set { ViewState["Deal"] = value; }
            get { return ViewState["Deal"] != null ? ViewState["Deal"].ToString() : "[page]"; }
        }
        public string[] InnerContent
        {
            get { return (string[])ViewState["Content"]; }
        }
        public string Content
        {
            set { ViewState["Content"] = value.Split(new string[] { this.Deal }, StringSplitOptions.RemoveEmptyEntries); }
            get { return this.InnerContent[base.PageIndex - 1]; }
        }
        public new int PageCount
        {
            get { return this.InnerContent.Length; }
        }
        public new int PageIndex
        {
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                else if (value > this.PageCount)
                {
                    value = this.PageCount;
                }
                base.PageIndex = value;
            }
            get { return base.PageIndex; }
        }

        public ArticlePager()
        {
            base.PageIndex = 1;
            base.PageSize = 1;
        }

        public new string DataBind()
        {
            if (this.PageCount == 0)
            {
                return String.Empty;
            }
            base.RecordCount = this.PageCount;
            return this.Content;
        }
    }
}