using System;
using System.Web.UI;
using System.ComponentModel;
using System.IO;

namespace Rocky.Web.Controls
{
    /// <summary>
    /// 索引功能类
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class PageJump : IStateManager, ITextControl
    {
        #region 属性
        [Description("获取或设置是否显示跳转索引框。"), DefaultValue(true), NotifyParentProperty(true)]
        public Boolean Visible
        {
            get { return ViewState["Visible"] != null ? (Boolean)ViewState["Visible"] : true; }
            set { ViewState["Visible"] = value; }
        }

        [Description("获取或设置位于页索引框左边的文字。"), DefaultValue("转到第"), NotifyParentProperty(true)]
        public String BeginText
        {
            get { return ViewState["BeginText"] != null ? (String)ViewState["BeginText"] : "转到第"; }
            set { ViewState["BeginText"] = value; }
        }

        [Description("获取或设置索引框的类型。"), DefaultValue(PageJumpType.TextBox), NotifyParentProperty(true)]
        public PageJumpType JumpType
        {
            get { return ViewState["JumpType"] != null ? (PageJumpType)ViewState["JumpType"] : PageJumpType.TextBox; }
            set { ViewState["JumpType"] = value; }
        }

        [Description("获取或设置索引框的内容。"), DefaultValue("跳转"), NotifyParentProperty(true)]
        public String Text
        {
            get { return ViewState["Text"] != null ? (String)ViewState["Text"] : "跳转"; }
            set { ViewState["Text"] = value; }
        }

        [Description("获取或设置位于页索引框右边的文字。"), DefaultValue("页"), NotifyParentProperty(true)]
        public String EndText
        {
            get { return ViewState["EndText"] != null ? (String)ViewState["EndText"] : "页"; }
            set { ViewState["EndText"] = value; }
        }
        #endregion

        #region 自定义ViewState
        private Boolean isTracking;
        private StateBag viewState;

        protected StateBag ViewState
        {
            get
            {
                if (this.viewState == null)
                {
                    this.viewState = new StateBag(false);
                    if (this.isTracking)
                    {
                        ((IStateManager)this.viewState).TrackViewState();
                    }
                }
                return this.viewState;
            }
        }
        #endregion

        #region IStateManager 成员
        public bool IsTrackingViewState
        {
            get
            {
                return this.isTracking;
            }
        }

        public void LoadViewState(object savedState)
        {
            if (savedState != null)
            {
                ((IStateManager)this.ViewState).LoadViewState(savedState);
            }
        }

        public object SaveViewState()
        {
            return this.viewState != null ? ((IStateManager)this.viewState).SaveViewState() : null;
        }

        public void TrackViewState()
        {
            this.isTracking = true;
            if (this.viewState != null)
            {
                ((IStateManager)this.viewState).TrackViewState();
            }
        }
        #endregion
    }

    /// <summary>
    /// 索引功能外观
    /// </summary>
    public enum PageJumpType
    {
        TextBox, DropDownList
    }
}