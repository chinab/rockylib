using System;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rocky.Web.Controls
{
    [ParseChildren(true), PersistChildren(false), ToolboxData("<{0}:WebPager runat='server'></{0}:WebPager>"), Description("WebPager控件")]
    public partial class WebPager : CompositeControl
    {
        #region 字段
        private LinkButton[] lbDigits;
        private LinkButton lbFirst, lbPrevious, lbPrevBatch, lbNextBatch, lbNext, lbLast;
        private TextBox tbJumpIndex;
        private LinkButton btnJump;
        private DropDownList ddlJumpIndex;

        private int urlPageIndex;
        /// <summary>
        /// 是否启用URL分页
        /// </summary>
        private bool urlPaging;
        /// <summary>
        /// 获取或设置查询字符串集合。（当有多个查询字符串时使用，指定的分页子串必须放在最后，格式如：name=xx&pageIndex）
        /// </summary>
        private string queryString;
        #endregion

        #region 属性
        protected override HtmlTextWriterTag TagKey
        {
            get { return HtmlTextWriterTag.Div; }
        }

        public ButtonFlags VisibleFlags
        {
            get { return ViewState["VisibleFlags"] != null ? (ButtonFlags)ViewState["VisibleFlags"] : ButtonFlags.All; }
            set { ViewState["VisibleFlags"] = value; }
        }

        [Browsable(true), Description("获取或设置首页文字。"), Category("分页外观"), DefaultValue("<font face='webdings'>9</font>")]
        public String FirstText
        {
            get { return ViewState["FirstText"] != null ? (String)ViewState["FirstText"] : "<font face='webdings'>9</font>"; }
            set { ViewState["FirstText"] = value; }
        }
        [Browsable(true), Description("获取或设置前翻文字。"), Category("分页外观"), DefaultValue("...")]
        public String ForwardText
        {
            get { return ViewState["ForwardText"] != null ? (String)ViewState["ForwardText"] : "..."; }
            set { ViewState["ForwardText"] = value; }
        }
        [Browsable(true), Description("获取或设置上一页字。"), Category("分页外观"), DefaultValue("<font face='webdings'>3</font>")]
        public String PreviousText
        {
            get { return ViewState["PreviousText"] != null ? (String)ViewState["PreviousText"] : "<font face='webdings'>3</font>"; }
            set { ViewState["PreviousText"] = value; }
        }
        [Browsable(true), Description("获取或设置下一页文字。"), Category("分页外观"), DefaultValue("<font face='webdings'>4</font>")]
        public String NextText
        {
            get { return ViewState["NextText"] != null ? (String)ViewState["NextText"] : "<font face='webdings'>4</font>"; }
            set { ViewState["NextText"] = value; }
        }
        [Browsable(true), Description("获取或设置后翻文字。"), Category("分页外观"), DefaultValue("...")]
        public String BackText
        {
            get { return ViewState["BackText"] != null ? (String)ViewState["BackText"] : "..."; }
            set { ViewState["BackText"] = value; }
        }
        [Browsable(true), Description("获取或设置尾页文字。"), Category("分页外观"), DefaultValue("<font face='webdings'>:</font>")]
        public String LastText
        {
            get { return ViewState["LastText"] != null ? (String)ViewState["LastText"] : "<font face='webdings'>:</font>"; }
            set { ViewState["LastText"] = value; }
        }
        [Browsable(true), Description("获取或设置没有启用页码的样式。"), Category("分页外观"), DefaultValue("")]
        public String DisabledCss
        {
            get { return ViewState["DisabledCss"] != null ? (String)ViewState["DisabledCss"] : String.Empty; }
            set { ViewState["DisabledCss"] = value; }
        }
        [Browsable(true), Description("获取或设置当前页码的样式。"), Category("分页外观"), DefaultValue("")]
        public String CurrentCss
        {
            get { return ViewState["CurrentCss"] != null ? (String)ViewState["CurrentCss"] : String.Empty; }
            set { ViewState["CurrentCss"] = value; }
        }
        private PageJump pageJump;
        [Browsable(true), Description("获取或设置跳转索引框的样式。"), Category("分页外观")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content), TypeConverter(typeof(ExpandableObjectConverter)), PersistenceMode(PersistenceMode.InnerProperty)]
        public PageJump PageJump
        {
            get
            {
                if (pageJump == null)
                {
                    pageJump = new PageJump();
                    // IsTrackingViewState获取一个值，用于指示服务器控件是否会将更改保存到其视图状态中
                    if (IsTrackingViewState)
                    {
                        ((IStateManager)pageJump).TrackViewState();
                    }
                }
                return pageJump;
            }
        }

        [Browsable(true), Description("获取或设置数据记录总数。"), Category("分页数据"), DefaultValue(0)]
        public Int32 RecordCount
        {
            get { return ViewState["RecordCount"] != null ? (Int32)ViewState["RecordCount"] : 0; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
#endif
                ViewState["RecordCount"] = value;
            }
        }
        [Browsable(true), Description("获取或设置每页显示的记录数量。"), Category("分页数据"), DefaultValue(10)]
        public Int32 PageSize
        {
            get { return ViewState["PageSize"] != null ? (Int32)ViewState["PageSize"] : 10; }
            set
            {
#if DEBUG
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException();
                }
#endif
                ViewState["PageSize"] = value;
            }
        }
        [Description("获取总页数。"), DefaultValue(0)]
        public Int32 PageCount
        {
            get { return this.PageSize > 0 ? (this.RecordCount + this.PageSize - 1) / this.PageSize : 0; }
        }
        [Browsable(true), Description("获取或设置当前页码。"), Category("分页数据"), DefaultValue(1)]
        public Int32 PageIndex
        {
            get
            {
                return urlPaging ? urlPageIndex : (ViewState["PageIndex"] != null ? (Int32)ViewState["PageIndex"] : 1);
            }
            set
            {
#if DEBUG
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException();
                }
#endif
                ViewState["PageIndex"] = value;
            }
        }
        [Browsable(true), Description("获取或设置每次显示的页码数量。"), Category("分页数据"), DefaultValue(10)]
        public Int32 PageNumberCount
        {
            get { return ViewState["PageNumberCount"] != null ? (Int32)ViewState["PageNumberCount"] : 10; }
            set
            {
#if DEBUG
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException();
                }
#endif
                ViewState["PageNumberCount"] = value;
            }
        }
        #endregion

        #region 重写方法
        protected override void OnInit(EventArgs e)
        {
            base.Font.Name = "Verdana";
            base.Font.Size = FontUnit.Parse("12px");
            base.OnInit(e);
        }

        /// <summary>
        /// 重写ICompositeControlDesignerAccessor接口的RecreateChildContrls方法
        /// </summary>
        protected override void RecreateChildControls()
        {
            if (!urlPaging)
            {
                EnsureChildControls();
            }
        }

        /// <summary>
        /// 重写Control基类的CreateChildControls方法
        /// </summary>
        protected override void CreateChildControls()
        {
            if (!urlPaging)
            {
                //清除所有子控件
                Controls.Clear();

                var flags = this.VisibleFlags;

                lbFirst = new LinkButton();
                lbFirst.ID = "lbFirst";
                lbFirst.CommandName = "LinkJump";
                lbFirst.Text = this.FirstText;
                lbFirst.Visible = (flags & ButtonFlags.First) == ButtonFlags.First;
                Controls.Add(lbFirst);

                lbPrevious = new LinkButton();
                lbPrevious.ID = "lbPrevious";
                lbPrevious.CommandName = "LinkJump";
                lbPrevious.Text = this.PreviousText;
                lbPrevious.Visible = (flags & ButtonFlags.Previous) == ButtonFlags.Previous;
                Controls.Add(lbPrevious);

                int pageIndex = this.PageIndex, pageNumberCount = this.PageNumberCount, pageCount = this.PageCount;

                if (pageIndex <= 1)
                {
                    lbPrevious.CssClass = lbFirst.CssClass = this.DisabledCss;
                    lbFirst.Attributes["rel"] = "change";
                    lbPrevious.Attributes["rel"] = "change";
                }
                int numIndex = (pageIndex / pageNumberCount) * pageNumberCount;
                int firtNum = (pageIndex == numIndex) ? numIndex - pageNumberCount + 1 : numIndex + 1;
                int lastNum = firtNum + pageNumberCount - 1;
                if (lastNum > pageCount)
                {
                    lastNum = pageCount;
                }
                lbPrevBatch = new LinkButton();
                lbPrevBatch.ID = "lbPrevBatch";
                lbPrevBatch.CommandName = "LinkJump";
                lbPrevBatch.Text = this.ForwardText;
                lbPrevBatch.Visible = (flags & ButtonFlags.PreviousBatch) == ButtonFlags.PreviousBatch;
                Controls.Add(lbPrevBatch);
                if (firtNum <= 0 || pageIndex <= pageNumberCount)
                {
                    lbPrevBatch.CssClass = this.DisabledCss;
                    lbPrevBatch.Attributes["rel"] = "change";
                }
                //设置数字翻页控件数组
                lbDigits = new LinkButton[lastNum - firtNum + 1];
                int j = 0;
                for (int i = firtNum; i <= lastNum; i++)
                {
                    lbDigits[j] = new LinkButton();
                    lbDigits[j].ID = "lb" + i.ToString();
                    lbDigits[j].Text = i.ToString();
                    lbDigits[j].CommandName = "LinkJump";
                    Controls.Add(lbDigits[j]);
                    if (i == this.PageIndex)
                    {
                        lbDigits[j].Font.Bold = true;
                        lbDigits[j].CssClass = this.CurrentCss;
                        lbDigits[j].Attributes["rel"] = "change";
                    }
                    j++;
                }
                lbNextBatch = new LinkButton();
                lbNextBatch.ID = "lbNextBatch";
                lbNextBatch.CommandName = "LinkJump";
                lbNextBatch.Text = this.BackText;
                lbNextBatch.Visible = (flags & ButtonFlags.NextBatch) == ButtonFlags.NextBatch;
                Controls.Add(lbNextBatch);
                if (lastNum >= pageCount || pageCount < (pageIndex + pageNumberCount))
                {
                    lbNextBatch.CssClass = this.DisabledCss;
                    lbNextBatch.Attributes["rel"] = "change";
                }

                lbNext = new LinkButton();
                lbNext.ID = "lbNext";
                lbNext.CommandName = "LinkJump";
                lbNext.Text = this.NextText;
                lbNext.Visible = (flags & ButtonFlags.Next) == ButtonFlags.Next;
                Controls.Add(lbNext);

                lbLast = new LinkButton();
                lbLast.ID = "lbLast";
                lbLast.CommandName = "LinkJump";
                lbLast.Text = this.LastText;
                lbLast.Visible = (flags & ButtonFlags.Last) == ButtonFlags.Last;
                Controls.Add(lbLast);

                if (pageIndex >= pageCount)
                {
                    lbLast.CssClass = lbNext.CssClass = this.DisabledCss;
                    lbNext.Attributes["rel"] = "change";
                    lbLast.Attributes["rel"] = "change";
                }

                if (pageJump != null && this.PageJump.Visible)
                {
                    if (this.PageJump.JumpType == PageJumpType.TextBox)
                    {
                        tbJumpIndex = new TextBox();
                        tbJumpIndex.ID = "tbJumpIndex";
                        tbJumpIndex.Text = pageIndex.ToString();
                        tbJumpIndex.Attributes.Add("onkeyup", @"this.value=this.value.replace(/[^\d]/g,'');if(this.value==''||this.value>" + pageCount + ")this.value=1;");
                        tbJumpIndex.Attributes.Add("onbeforepaste", @"clipboardData.setData('text',clipboardData.getData('text').replace(/[^\d]/g,''));");
                        Controls.Add(tbJumpIndex);
                        btnJump = new LinkButton();
                        btnJump.ID = "btnJump";
                        btnJump.CommandName = "PageJump";
                        btnJump.Text = this.PageJump.Text;
                        Controls.Add(btnJump);
                    }
                    else
                    {
                        ddlJumpIndex = new DropDownList();
                        ddlJumpIndex.ID = "ddlJumpIndex";
                        for (int i = 1; i <= pageCount; i++)
                        {
                            ddlJumpIndex.Items.Add(new ListItem(i.ToString(), i.ToString()));
                        }
                        ddlJumpIndex.SelectedValue = pageIndex.ToString();
                        ddlJumpIndex.EnableViewState = false;
                        ddlJumpIndex.AutoPostBack = true;
                        ddlJumpIndex.SelectedIndexChanged += (sender, e) =>
                        {
                            int toJump = int.Parse(((DropDownList)sender).SelectedValue);
                            if (toJump >= 1 && toJump <= this.PageCount)
                            {
                                this.PageIndex = toJump;
                            }
                            this.OnPageIndexChanged(e);
                        };
                        Controls.Add(ddlJumpIndex);
                    }
                }
            }
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (!urlPaging)
            {
                this.CreateChildControls();

                lbFirst.RenderControl(writer);
                writer.Write("&nbsp;&nbsp;");

                lbPrevious.RenderControl(writer);
                writer.Write("&nbsp;&nbsp;");

                lbPrevBatch.RenderControl(writer);
                writer.Write("&nbsp;&nbsp;");

                // 呈现页码部分
                foreach (LinkButton lb in lbDigits)
                {
                    lb.RenderControl(writer);
                    writer.Write("&nbsp;&nbsp;");
                }

                lbNextBatch.RenderControl(writer);
                writer.Write("&nbsp;&nbsp;");

                lbNext.RenderControl(writer);
                writer.Write("&nbsp;&nbsp;");

                lbLast.RenderControl(writer);

                // 呈现索引跳转   
                if (pageJump != null)
                {
                    writer.Write("&nbsp;&nbsp;");

                    writer.Write("共" + this.RecordCount + "条记录，当前第" + this.PageIndex + "/" + this.PageCount + "页&nbsp;&nbsp;");

                    switch (this.PageJump.JumpType)
                    {
                        //使用输入框输入页码跳转
                        case PageJumpType.TextBox:
                            if (tbJumpIndex != null && btnJump != null)
                            {
                                writer.Write(this.PageJump.BeginText);
                                tbJumpIndex.MaxLength = 9;
                                tbJumpIndex.Style.Add("width", "24px");
                                tbJumpIndex.Style.Add("text-align", "center");
                                tbJumpIndex.RenderControl(writer);
                                writer.Write(this.PageJump.EndText);
                                btnJump.RenderControl(writer);
                            }
                            break;
                        //下拉列表框跳转
                        case PageJumpType.DropDownList:
                            if (ddlJumpIndex != null)
                            {
                                writer.Write(pageJump.BeginText);
                                ddlJumpIndex.RenderControl(writer);
                                writer.Write(pageJump.EndText);
                            }
                            break;
                    }
                }

                writer.Write("<script>(function() {$(\"a[rel='change']\").each(function(i, o) {var a = $(o), span = document.createElement('span');span.className = a.attr('class');span.innerHTML = a.html();$(span).insertAfter(a);a.remove();});})();</script>");
            }
            else
            {
                // 上翻页码
                int pageUpIndex = GetPageUpIndex(this.PageIndex, this.PageNumberCount);
                // 下翻页码
                int pageDownIndex = GetPageDownIndex(this.PageIndex, this.PageNumberCount);

                // 首页
                if (!String.IsNullOrEmpty(this.FirstText))
                {
                    if (this.PageIndex != 1)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, String.Format("?{0}=1", queryString));
                    }
                    else
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, this.DisabledCss);
                    }
                    writer.RenderBeginTag(HtmlTextWriterTag.A);
                    writer.Write(this.FirstText);
                    writer.RenderEndTag();
                    writer.Write("&nbsp;&nbsp;");
                }

                // 前翻
                if (!String.IsNullOrEmpty(this.PreviousText))
                {
                    if (this.PageIndex > this.PageNumberCount)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, String.Format("?{0}={1}", queryString, pageUpIndex));
                    }
                    else
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, this.DisabledCss);
                    }
                    writer.RenderBeginTag(HtmlTextWriterTag.A);
                    writer.Write(this.PreviousText);
                    writer.RenderEndTag();
                    writer.Write("&nbsp;&nbsp;");
                }

                // 页码
                RenderPageNumber(writer, this);

                // 后翻
                if (!String.IsNullOrEmpty(this.NextText))
                {
                    if (pageDownIndex <= this.PageCount)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, String.Format("?{0}={1}", queryString, pageDownIndex));
                    }
                    else
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, this.DisabledCss);
                    }
                    writer.RenderBeginTag(HtmlTextWriterTag.A);
                    writer.Write(this.NextText);
                    writer.RenderEndTag();
                    writer.Write("&nbsp;&nbsp;");
                }

                // 尾页
                if (!String.IsNullOrEmpty(this.LastText))
                {
                    if (this.PageIndex != this.PageCount)
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Href, String.Format("?{0}={1}", queryString, this.PageCount));
                    }
                    else
                    {
                        writer.AddAttribute(HtmlTextWriterAttribute.Disabled, "true");
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, this.DisabledCss);
                    }
                    writer.RenderBeginTag(HtmlTextWriterTag.A);
                    writer.Write(this.LastText);
                    writer.RenderEndTag();
                }

                if (this.pageJump != null)
                {
                    // 状态页
                    writer.Write("&nbsp;&nbsp;");
                    writer.Write("共" + this.RecordCount + "条记录，当前第" + this.PageIndex + "/" + this.PageCount + "页&nbsp;&nbsp;");

                    // 索引页
                    RenderPageIndexContent(writer, this);
                }
            }
        }

        protected override void Render(HtmlTextWriter writer)
        {
            if (this.RecordCount == 0 || this.PageIndex < 1 || this.PageIndex > this.RecordCount)
            {
                return;
            }
            base.Render(writer);
        }
        #endregion

        #region 自定义ViewState
        protected override object SaveViewState()
        {
            object baseState = base.SaveViewState();
            object thisState = null;

            if (pageJump != null)
            {
                thisState = ((IStateManager)pageJump).SaveViewState();
            }

            if (thisState != null)
            {
                return new Pair(baseState, thisState);
            }
            else
            {
                return baseState;
            }
        }

        protected override void TrackViewState()
        {
            if (pageJump != null)
            {
                ((IStateManager)pageJump).TrackViewState();
            }
            base.TrackViewState();
        }
        #endregion

        #region PageIndexChanged事件
        private static readonly object EventKey = new object();

        /// <summary>
        /// 添加数据绑定事件
        /// </summary>
        public event EventHandler PageIndexChanged
        {
            add { Events.AddHandler(EventKey, value); }
            remove { Events.RemoveHandler(EventKey, value); }
        }

        /// <summary>
        /// 实现数据绑定
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPageIndexChanged(EventArgs e)
        {
            EventHandler dataBindHandler = (EventHandler)Events[EventKey];
            if (dataBindHandler != null)
            {
                dataBindHandler(this, e);
            }
        }

        /// <summary>
        /// 重写OnBubbleEvent方法，执行事件冒泡，使用事件参数的CommandName成员确定是否需要引发事件处理程序OnPageIndexChanged，并返回true
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        protected override bool OnBubbleEvent(object source, EventArgs e)
        {
            bool handled = false;
            if (e is CommandEventArgs)
            {
                CommandEventArgs ce = (CommandEventArgs)e;
                //设置LinkButton的跳转
                if (ce.CommandName == "LinkJump")
                {
                    LinkButton lb = (LinkButton)source;
                    switch (lb.ID)
                    {
                        //首页跳转
                        case "lbFirst":
                            this.PageIndex = 1;
                            break;
                        //上一页跳转
                        case "lbPrevious":
                            if (this.PageIndex > 1)
                            {
                                this.PageIndex -= 1;
                            }
                            break;
                        //前翻跳转
                        case "lbPrevBatch":
                            if ((this.PageIndex - this.PageNumberCount) > 0)
                            {
                                this.PageIndex -= this.PageNumberCount;
                            }
                            break;
                        //后翻跳转
                        case "lbNextBatch":
                            if ((this.PageIndex + this.PageNumberCount) < this.PageCount)
                            {
                                this.PageIndex += this.PageNumberCount;
                            }
                            break;
                        //下一页跳转
                        case "lbNext":
                            if (this.PageCount > this.PageIndex)
                            {
                                this.PageIndex += 1;
                            }
                            break;
                        //尾页跳转
                        case "lbLast":
                            this.PageIndex = this.PageCount;
                            break;
                        //数字页跳转
                        default:
                            int numToJump = int.Parse(lb.ID.Substring(2, lb.ID.Length - 2));
                            if (numToJump == this.PageIndex)
                            {
                                return false;
                            }
                            this.PageIndex = numToJump;
                            break;
                    }
                    //响应跳页码变更事件，在此事件中绑定数据
                    OnPageIndexChanged(EventArgs.Empty);
                    handled = true;
                }
                //设置用户输入数字的跳转
                else if (ce.CommandName == "PageJump")
                {
                    int toJump = int.Parse(tbJumpIndex.Text.Trim());
                    if ((toJump < 1) || (toJump > this.PageCount))
                    {
                        return false;
                    }
                    this.PageIndex = toJump;
                    OnPageIndexChanged(EventArgs.Empty);
                    handled = true;
                }
            }
            return handled;
        }
        #endregion
    }

    #region UrlMethods
    public partial class WebPager
    {
        public void UrlToProperties(string urlPagingKeyWord, string urlQueryString)
        {
            if (int.TryParse(base.Page.Request.QueryString[urlPagingKeyWord], out urlPageIndex))
            {
                urlPaging = true;
                queryString = urlQueryString;
            }
        }

        /// <summary>
        /// 得到前翻页码（动态，前置条件：当前页码）
        /// </summary>
        /// <param name="pageIndex">当前选中的页码</param>
        /// <param name="pageNumberCount">每页显示的分页页码数量</param>
        /// <returns></returns>
        private static Int32 GetPageUpIndex(Int32 pageIndex, Int32 pageNumberCount)
        {
            if (pageIndex % pageNumberCount == 0)
            {
                return (pageIndex / pageNumberCount - 2) * pageNumberCount + pageNumberCount;
            }
            else
            {
                return (pageIndex / pageNumberCount - 1) * pageNumberCount + pageNumberCount;
            }
        }

        /// <summary>
        /// 得到后翻页码（动态，前置条件：当前页码）
        /// </summary>
        /// <param name="pageIndex">当前选中的页码</param>
        /// <param name="pageNumberCount">每页显示的分页页码数量</param>
        /// <returns></returns>
        private static Int32 GetPageDownIndex(Int32 pageIndex, Int32 pageNumberCount)
        {
            if (pageIndex % pageNumberCount == 0)
            {
                return (pageIndex / pageNumberCount) * pageNumberCount + 1;
            }
            else
            {
                return (pageIndex / pageNumberCount + 1) * pageNumberCount + 1;
            }
        }

        /// <summary>
        /// 呈现页码部分
        /// </summary>
        /// <param name="output">HtmlTextWriter</param>
        /// <param name="currentPageIndex">当前页码</param>
        /// <param name="showPageNumberCount">显示的页码数量</param>
        private static void RenderPageNumber(HtmlTextWriter output, WebPager pager)
        {
            Int32 firstIndex = 0, endIndex = 0;
            // 如果当前页不能整除 PageNumberCount 页
            if (pager.PageIndex % pager.PageNumberCount != 0)
            {
                firstIndex = pager.PageIndex / pager.PageNumberCount * pager.PageNumberCount + 1;
                endIndex = firstIndex + pager.PageNumberCount;
            }
            else
            {
                firstIndex = (pager.PageIndex / pager.PageNumberCount - 1) * pager.PageNumberCount + 1;
                endIndex = firstIndex + pager.PageNumberCount;
            }

            for (Int32 i = firstIndex; i < endIndex && i <= pager.PageCount; i++)
            {
                // 当前选中页用特殊颜色标记
                if (i != pager.PageIndex)
                {
                    output.AddAttribute(HtmlTextWriterAttribute.Href, String.Format("?{0}={1}", pager.queryString, i));
                }
                else
                {
                    output.AddStyleAttribute(HtmlTextWriterStyle.FontWeight, "blod");
                    output.AddAttribute(HtmlTextWriterAttribute.Class, pager.CurrentCss);
                }
                output.RenderBeginTag(HtmlTextWriterTag.A);
                output.Write(i);
                output.RenderEndTag();
                output.Write("&nbsp;&nbsp;");
            }
        }

        /// <summary>
        /// 呈现索引部分
        /// </summary>
        /// <param name="output">HtmlTextWriter</param>
        /// <param name="sumPageCount">总页数</param>
        /// <param name="queryString">查询字符串</param>
        /// <param name="selectedPageNumberColor">当前选中页码的颜色</param>
        public static void RenderPageIndexContent(HtmlTextWriter output, WebPager pager)
        {
            if (pager.pageJump.Visible)
            {
                String uniqueId = String.Empty;
                switch (pager.pageJump.JumpType)
                {
                    case PageJumpType.TextBox:
                        uniqueId = "txt" + pager.UniqueID;
                        output.Write(pager.PageJump.BeginText);
                        output.Write("<input id='" + uniqueId + "' type='text' maxlength='9' value='" + pager.PageIndex + "' style='width:24px;text-align:center;' onkeypress=\"if(event.keyCode==13){document.getElementById('" + uniqueId.Replace("txt", "btn") + "').onclick();event.returnValue=false;}\" />");
                        output.Write(pager.PageJump.EndText);
                        output.Write("<a id='" + uniqueId.Replace("txt", "btn") + "' ");
                        // 验证输入的页码是否为正整数
                        output.Write("onclick=\"var str=document.getElementById('");
                        output.Write(uniqueId);
                        output.Write("').value;var reg=");
                        output.Write(@"/^\+?[1-9][0-9]*$/");
                        output.Write(";if(str.length==0||!reg.test(str)){alert('请输入正确的页码！');return;};var pageIndex=parseInt(str);if(pageIndex==");
                        output.Write(pager.PageIndex);
                        output.Write("){return;}if(pageIndex>");
                        // 总页数
                        output.Write(pager.PageCount);
                        output.Write("||pageIndex<=0){alert('索引超出范围，请重新输入页码！');}else{window.location.href='?");
                        // 查询字符串
                        output.Write(pager.queryString);
                        output.Write("='+pageIndex+'';}\">" + pager.PageJump.Text + "</a>");
                        break;
                    case PageJumpType.DropDownList:
                        uniqueId = "opt" + pager.UniqueID;
                        // 左边文字
                        output.Write(pager.PageJump.BeginText);
                        // <select />
                        output.Write("<select id=\"");
                        output.Write(uniqueId);
                        output.Write("\" ");
                        output.Write("onchange=\"window.location.href='?");
                        // 查询字符串
                        output.Write(pager.queryString);
                        output.Write("='+document.getElementById('");
                        output.Write(uniqueId);
                        output.Write("').value+''\"></select>");
                        output.Write("<script>(function (optWebPager){optWebPager.style.display='none';for(var i=0;i<");
                        // 总页数
                        output.Write(pager.PageCount);
                        output.Write(";i++){optWebPager.options[i]=new Option('第'+(i+1)+'页',i+1);}optWebPager[");
                        output.Write(pager.PageIndex);
                        output.Write("-1].selected='selected';optWebPager.style.display='';})(document.getElementById('");
                        output.Write(uniqueId);
                        output.Write("'));</script>");
                        // 右边文字
                        output.Write(pager.PageJump.EndText);
                        break;
                }
            }
        }
    }
    #endregion

    [Flags]
    public enum ButtonFlags
    {
        None = 0,
        First = 1 << 0,
        Previous = 1 << 1,
        PreviousBatch = 1 << 2,
        NextBatch = 1 << 3,
        Next = 1 << 4,
        Last = 1 << 5,
        All = First | Previous | PreviousBatch | NextBatch | Next | Last
    }
}