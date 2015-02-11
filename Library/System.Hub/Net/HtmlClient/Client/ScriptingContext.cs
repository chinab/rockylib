using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System.Net
{
    [ComVisible(true)]
    public class ScriptingContext : Disposable
    {
        #region StaticMethods
        /// <summary>
        /// 注入Script
        /// </summary>
        /// <param name="document"></param>
        /// <param name="js"></param>
        public static void InjectScript(HtmlDocument document, string js)
        {
            Contract.Requires(document != null);

            if (!CheckDocument(document.Url))
            {
                App.LogInfo("HttpBrowser InjectScript Cancel");
                return;
            }
            var head = document.GetElementsByTagName("head")[0];
            var script = document.CreateElement("script");
            script.SetAttribute("type", "text/javascript");
            script.SetAttribute("text", js);
            head.AppendChild(script);
        }
        internal static bool CheckDocument(Uri documentUrl)
        {
            if (documentUrl != null && documentUrl.OriginalString.StartsWith("res://ieframe.dll", StringComparison.OrdinalIgnoreCase))
            {
                App.LogInfo("CheckDocument {0}", documentUrl);
                return false;
            }
            return true;
        }

        public static void FillAjaxBlock(NameValueCollection form, AjaxBlockEntity[] set)
        {
            Contract.Requires(form != null);
            if (set.IsNullOrEmpty())
            {
                return;
            }

            form[AjaxBlockEntity.AjaxBlock] = Convert.ToBase64String(Serializer.Serialize(set).ToArray());
        }
        #endregion

        #region Fields
        private const int LazyDue = 2000;
        private readonly TimeSpan RaiserPeriod = TimeSpan.FromSeconds(2d);
        private System.Threading.Timer _lazyTimer;
        private AutoResetEvent _sendReceiveWaiter;
        private SynchronizedCollection<Tuple<HtmlElement, EventHandler>> _ajaxSet;
        private CountdownEvent _ajaxWaiter;
        private JobTimer _ajaxRaiser;
        public volatile bool IsRepost;
        #endregion

        #region Properties
        public Uri RequestUrl { get; internal set; }
        public HttpRequestContent RequestContent { get; private set; }
        internal AutoResetEvent WaitHandle { get; private set; }
        internal AutoResetEvent SendReceiveWaiter
        {
            get
            {
                if (_sendReceiveWaiter == null)
                {
                    _sendReceiveWaiter = new AutoResetEvent(false);
                }
                return _sendReceiveWaiter;
            }
        }
        internal AjaxBlockEntity[] AjaxBlocks { get; private set; }
        internal CountdownEvent AjaxWaiter
        {
            get
            {
                if (_ajaxWaiter == null)
                {
                    _ajaxWaiter = new CountdownEvent(1);
                }
                return _ajaxWaiter;
            }
        }
        #endregion

        #region Constructor
        public ScriptingContext(Uri url, HttpRequestContent content)
        {
            this.RequestUrl = url;
            this.RequestContent = content;
            string ablock;
            if (this.RequestContent != null && this.RequestContent.Form != null)
            {
                if (!string.IsNullOrEmpty(ablock = this.RequestContent.Form.Get(AjaxBlockEntity.AjaxBlock)))
                {
                    this.AjaxBlocks = (AjaxBlockEntity[])Serializer.Deserialize(new MemoryStream(Convert.FromBase64String(ablock)));
                    this.RequestContent.Form.Remove(AjaxBlockEntity.AjaxBlock);
                }
            }
            this.WaitHandle = new AutoResetEvent(false);
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                if (_lazyTimer != null)
                {
                    _lazyTimer.Dispose();
                    _lazyTimer = null;
                }
                DisposeObject(_sendReceiveWaiter);
                DisposeObject(_ajaxWaiter);
                DisposeObject(this.WaitHandle);
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// 注册生命周期
        /// </summary>
        /// <param name="func"></param>
        /// <param name="state"></param>
        internal void RegisterLazyLoad(Action<object> func, object state)
        {
            if (_lazyTimer == null)
            {
                _lazyTimer = new System.Threading.Timer(x => func(x), state, LazyDue, Timeout.Infinite);
            }
            else
            {
                DelayLazyLoad();
            }
        }

        /// <summary>
        /// 生命周期delay一次
        /// </summary>
        internal void DelayLazyLoad()
        {
            if (_lazyTimer == null)
            {
                return;
            }
            _lazyTimer.Change(LazyDue, Timeout.Infinite);
        }

        /// <summary>
        /// 注销生命周期
        /// </summary>
        internal void UnregisterLazyLoad()
        {
            if (_lazyTimer == null)
            {
                return;
            }
            _lazyTimer.Dispose();
            _lazyTimer = null;
        }
        #endregion

        #region Ajax
        /// <summary>
        /// 设置ajax参数
        /// </summary>
        /// <param name="browser"></param>
        /// <param name="flags"></param>
        internal void SetAjax(WebBrowser browser, AjaxBlockFlags flags, object logParam = null)
        {
            if (this.AjaxBlocks.IsNullOrEmpty())
            {
                return;
            }
            this.AjaxWaiter.Reset();
            foreach (var block in this.AjaxBlocks.Where(p => p.Flags.HasFlag(flags)))
            {
                var node = browser.Document.GetElementById(block.ID);
                if (node == null)
                {
                    continue;
                }
                this.AjaxWaiter.AddCount();
                this.AjaxMark(node, (sender, e) =>
                {
                    ushort step = 0;
                    var rNode = (HtmlElement)sender;
                    if (block.Text == null || (!block.Text.Equals(rNode.InnerText, StringComparison.OrdinalIgnoreCase)))
                    {
                        step++;
                        rNode.SetAttribute(AjaxBlockEntity.AjaxBlock, "1");
                        if (!string.IsNullOrEmpty(block.SuccessScript))
                        {
                            browser.Invoke((Action)(() =>
                            {
                                browser.Document.InvokeScript("eval", new object[] { block.SuccessScript });
                            }));
                        }
                        if (this.AjaxWaiter.CurrentCount > 0)
                        {
                            step++;
                            this.AjaxWaiter.Signal();
                        }
                    }
                    App.LogInfo("ScriptingContext SetAjax Step {0}\t{1}", step, logParam);
                });
            }
            this.AjaxWaiter.Signal();
            if (_ajaxRaiser != null)
            {
                _ajaxRaiser.Start();
            }
        }
        /// <summary>
        /// 等待ajax执行
        /// </summary>
        /// <param name="arg"></param>
        internal bool WaitAjax(int sendReceiveTimeout, object logParam = null)
        {
            if (this.AjaxBlocks.IsNullOrEmpty())
            {
                return false;
            }
            try
            {
                if (sendReceiveTimeout <= 0)
                {
                    sendReceiveTimeout = (int)TimeSpan.FromSeconds(30d).TotalMilliseconds;
                }
                if (!this.AjaxWaiter.Wait(sendReceiveTimeout))
                {
                    App.LogInfo("ScriptingContext WaitAjax Timeout {0}", logParam);
                    return false;
                }
                return true;
            }
            finally
            {
                if (_ajaxRaiser != null)
                {
                    _ajaxRaiser.Stop();
                }
            }
        }

        /// <summary>
        /// 注册Ajax节点
        /// </summary>
        /// <param name="node"></param>
        /// <param name="e"></param>
        private void AjaxMark(HtmlElement node, EventHandler e)
        {
            if (_ajaxSet == null)
            {
                _ajaxSet = new SynchronizedCollection<Tuple<HtmlElement, EventHandler>>();
                _ajaxRaiser = new JobTimer(state =>
                {
                    foreach (var item in _ajaxSet.Where(p => p.Item1.GetAttribute(AjaxBlockEntity.AjaxBlock) == "0").ToArray())
                    {
                        item.Item2(item.Item1, EventArgs.Empty);
                    }
                }, Timeout.InfiniteTimeSpan)
                {
                    AutoDispose = false
                };
            }
            node.SetAttribute(AjaxBlockEntity.AjaxBlock, "0");
            var q = from t in _ajaxSet
                    where t.Item1 == node
                    select t;
            var tuple = q.SingleOrDefault();
            if (tuple != null)
            {
                _ajaxSet.Remove(tuple);
            }
            _ajaxSet.Add(Tuple.Create(node, e));
        }
        #endregion
    }
}