using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System.Net
{
    public partial class ClientForm : Form, IHttpClient
    {
        static ClientForm()
        {
            NativeMethods.SetBrowserFeatureControl();
        }
        public ClientForm()
        {
            InitializeComponent();
            var browser = webBrowser1;
            xInit(browser);
            browser.DocumentCompleted += browser_DocumentCompleted;
        }

        protected override void OnLoad(EventArgs e)
        {
            _lazyClient = new Lazy<IHttpClient>(() => new HttpClient(), false);
            _cookieContainer = new CookieContainer();
            this.SendReceiveTimeout = Timeout.Infinite;
            this.UseCookies = true;

            base.OnLoad(e);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_isSetProxy)
            {
                RestoreSystemProxy();
            }
            base.OnClosing(e);
        }

        #region xBrowser
        internal static void xInit(WebBrowser browser)
        {
            browser.ScriptErrorsSuppressed = true;
            browser.IsWebBrowserContextMenuEnabled = false;
            browser.NewWindow += browser_NewWindow;
            browser.Navigating += browser_Navigating;
        }
        private static void browser_NewWindow(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var browser = (WebBrowser)sender;
            var node = browser.Document.ActiveElement;
            string link;
            if (node != null && !string.IsNullOrEmpty(link = node.GetAttribute("href")))
            {
                e.Cancel = true;
                browser.Navigate(link);
            }
        }
        private static void browser_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            var browser = (WebBrowser)sender;
#if DEBUG
            App.LogInfo("browser_Navigating {0}@{1}", e.Url, browser.Url);
#endif
            var arg = (ScriptingContext)browser.ObjectForScripting;
            arg.DelayLazyLoad();
        }

        internal static string xGetHtml(WebBrowser browser)
        {
            return (string)browser.Document.InvokeScript("Soubiscbot");
        }

        internal static string xInvoke(WebBrowser browser, string js)
        {
            return browser.Document.InvokeScript("eval", new object[] { js }) as string;
        }

        /// <summary>
        /// 网页快照尺寸，Full Screenshot则设置Size.Empty
        /// </summary>
        /// <param name="size"></param>
        internal static void xSnapshot(WebBrowser browser, Size size, string saveFileDirectory, Guid? fileID = null)
        {
            browser.ScrollBarsEnabled = false;
            browser.Size = new Size(Screen.PrimaryScreen.WorkingArea.Width, 10240);
            var arg = (ScriptingContext)browser.ObjectForScripting;
            if (fileID == null)
            {
                fileID = CryptoManaged.MD5Hash(arg.RequestUrl.OriginalString);
            }
            string savePath = Path.Combine(saveFileDirectory, string.Format("{0}.png", fileID));
            try
            {
                var js = new StringBuilder();
                js.AppendFormat("document.body.setAttribute('_CurrentSnapshot', '{0}');", fileID);
                js.Append(@"    window.addEventListener('load', function () {
        window.scrollTo(0, document.documentElement.offsetHeight);
    });
");
                xInvoke(browser, js.ToString());
                browser.Size = size == Size.Empty ? browser.Document.Body.ScrollRectangle.Size : size;
                using (var img = new Bitmap(browser.Width, browser.Height))
                {
                    NativeMethods.DrawTo(browser.ActiveXInstance, img, Color.White);
                    img.Save(savePath, System.Drawing.Imaging.ImageFormat.Png);
                    App.LogInfo("xSnapshot {0} {1}", browser.Url, savePath);
                }
            }
            catch (Exception ex)
            {
                App.LogError(ex, "xSnapshot {0} {1}", browser.Url, savePath);
            }
        }
        #endregion

        #region Fields
        private volatile bool _isSetProxy, _isNavigated;
        private Lazy<IHttpClient> _lazyClient;
        private CookieContainer _cookieContainer;
        #endregion

        #region Properties
        public int SendReceiveTimeout { get; set; }
        public ushort? RetryCount { get; set; }
        public TimeSpan? RetryWaitDuration { get; set; }
        public bool UseCookies { get; set; }
        public CookieContainer CookieContainer
        {
            get { return _cookieContainer; }
        }
        public string SaveFileDirectory { get; set; }
        /// <summary>
        /// 供下载使用
        /// </summary>
        internal IHttpClient Client
        {
            get
            {
                var client = _lazyClient.Value;
                client.SendReceiveTimeout = this.SendReceiveTimeout;
                client.RetryCount = this.RetryCount;
                client.RetryWaitDuration = this.RetryWaitDuration;
                client.UseCookies = this.UseCookies;
                client.SaveFileDirectory = this.SaveFileDirectory;
                return client;
            }
        }
        #endregion

        #region Methods
        public void SetProxy(EndPoint address, NetworkCredential credential = null)
        {
            if (credential != null)
            {
                throw new NotSupportedException("credential");
            }

#if DEBUG
            App.LogInfo("browser SetProxy {0}", address);
#endif
            if (WinInetInterop.SetConnectionProxy(address.ToString()))
            {
                _isSetProxy = true;
                App.LogInfo("browser SetProxy {0} succeed", address);
            }
        }
        internal void RestoreSystemProxy()
        {
#if DEBUG
            App.LogInfo("browser RestoreSystemProxy");
#endif
            if (WinInetInterop.RestoreSystemProxy())
            {
                App.LogInfo("browser RestoreSystemProxy succeed");
            }
        }

        public string GetHtml(Uri requestUrl, HttpRequestContent content = null)
        {
            var browser = webBrowser1;
            this.Navigate(requestUrl, content);
            return CurrentGetHtml();
        }

        public Stream GetStream(Uri requestUrl, HttpRequestContent content = null)
        {
            return this.Client.GetStream(requestUrl, content);
        }

        public void DownloadFile(Uri fileUrl, out string fileName)
        {
            this.Client.DownloadFile(fileUrl, out fileName);
        }
        #endregion

        #region EntryMethods
        public void Navigate(Uri requestUrl, HttpRequestContent content = null)
        {
            var arg = new ScriptingContext(requestUrl, content);
            var browser = webBrowser1;
            browser.ObjectForScripting = arg;
            byte[] postData = null;
            string headers = null;
            if (arg.RequestContent != null)
            {
                if (this.UseCookies)
                {
                    if (arg.RequestContent.HasCookie)
                    {
                        _cookieContainer.Add(arg.RequestUrl, arg.RequestContent.Cookies);
                    }
                    string cookieHeader = arg.RequestContent.Headers[HttpRequestHeader.Cookie];
                    if (!string.IsNullOrEmpty(cookieHeader))
                    {
                        _cookieContainer.SetCookies(arg.RequestUrl, cookieHeader.Replace(';', ','));
                        arg.RequestContent.Headers.Remove(HttpRequestHeader.Cookie);
                    }
                    cookieHeader = _cookieContainer.GetCookieHeader(arg.RequestUrl);
                    if (cookieHeader.Length > 0)
                    {
                        arg.RequestContent.Headers[HttpRequestHeader.Cookie] = cookieHeader.Replace(',', ';');
                    }
                    //WinInetInterop.SaveCookies(_cookieContainer, absoluteUri);
                }
                else
                {
                    arg.RequestContent.Headers[HttpRequestHeader.Cookie] = string.Empty;
                    //WinInetInterop.DeleteCache(WinInetInterop.CacheKind.Cookies);
                }
                if (arg.RequestContent.HasBody)
                {
                    arg.RequestContent.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    postData = Encoding.UTF8.GetBytes(arg.RequestContent.GetFormString());
                }
                headers = arg.RequestContent.GetHeadersString();
            }
            browser.Navigate(arg.RequestUrl, "_self", postData, headers);

            TaskHelper.Factory.StartNew(this.STA_WaitSr, browser);
            this.Tag = arg;
        }

        private void browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var browser = (WebBrowser)sender;
#if DEBUG
            App.LogInfo("browser_DocumentCompleted {0}@{1}", e.Url, browser.Url);
#endif
            var arg = (ScriptingContext)browser.ObjectForScripting;
            try
            {
                //e.Url不会变res:// 
                if (!ScriptingContext.CheckDocument(browser.Url))
                {
                    App.LogInfo("browser_DocumentCompleted Cancel {0}", browser.Url);
                    return;
                }
                if (browser.ReadyState != WebBrowserReadyState.Complete)
                {
                    return;
                }

                //发生redirect或iframe load
                if (browser.Url != e.Url)
                {
                    App.LogInfo("browser_DocumentCompleted Redirect {0} to {1}", arg.RequestUrl, e.Url);
                }
                if (this.UseCookies)
                {
                    WinInetInterop.LoadCookies(_cookieContainer, browser.Document.Url);
                }
                ScriptingContext.InjectScript(browser.Document, @"if (typeof ($) == 'undefined') {
            var script = document.createElement('script');
            script.src = 'http://libs.baidu.com/jquery/1.9.0/jquery.js';
            document.getElementsByTagName('head')[0].appendChild(script);
        }
        function Soubiscbot(kind) {
            switch (kind) {
                case 0:
                    var set = [];
                    $(arguments[1]).each(function (i, o) {
                        var me = $(o);
                        var id = me.attr('id');
                        if (!id) {
                            id = Math.random();
                            me.attr('id', id);
                        }
                        set[i] = id;
                    });
                    return set.toString();
                    break;
                case 1:
                    try {
                        return arguments[1]();
                    }
                    catch (ex) {
                        return ex.toString();
                    }
                    break;
                default:
                    return document.documentElement.outerHTML;
                    break;
            }
        }");

                if (this.SendReceiveTimeout != Timeout.Infinite)
                {
                    arg.SendReceiveWaiter.Set();
                }
                arg.SetAjax(browser, AjaxBlockFlags.Block, arg.RequestUrl);
                arg.RegisterLazyLoad(x =>
                {
                    bool isSet = arg.WaitAjax(this.SendReceiveTimeout, arg.RequestUrl);
                    _isNavigated = true;
                    arg.WaitHandle.Set();
                }, browser);
            }
            catch (Exception ex)
            {
                App.LogError(ex, "browser_DocumentCompleted RequestUrl={0} BrowserUrl={1}", arg.RequestUrl, browser.Url);
            }
        }

        private void STA_WaitSr(object state)
        {
            var browser = (WebBrowser)state;
#if DEBUG
            App.LogInfo("STA_WaitSr {0}", browser.Url);
#endif
            var arg = (ScriptingContext)browser.ObjectForScripting;
            try
            {
                int timeout = this.SendReceiveTimeout;
                if (timeout != Timeout.Infinite && !arg.SendReceiveWaiter.WaitOne(timeout))
                {
                    //请求超时
                    browser.Invoke((Action)(() =>
                    {
                        if (browser.ReadyState != WebBrowserReadyState.Complete)
                        {
                            App.LogInfo("browser SendReceive Timeout {0}", arg.RequestUrl);
                            browser.Stop();
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                App.LogError(ex, "browser STA_WaitSr {0}", arg.RequestUrl);
            }
        }
        #endregion

        #region CurrentMethods
        private void CheckNavigated()
        {
            if (!_isNavigated)
            {
                throw new InvalidOperationException("Is not Navigated");
            }
        }

        private void CurrentRepost(Uri location = null)
        {
            this.CheckNavigated();

            var browser = webBrowser1;
            var arg = (ScriptingContext)browser.ObjectForScripting;
            arg.UnregisterLazyLoad();
            if (location != null)
            {
                arg.RequestUrl = location;
            }

            TaskHelper.Factory.StartNew(this.STA_WaitSr, browser);
        }

        public string CurrentInvoke(string js, CurrentInvokeKind kind = CurrentInvokeKind.None)
        {
            this.CheckNavigated();

            var browser = webBrowser1;
            var arg = (ScriptingContext)browser.ObjectForScripting;
            if (kind == CurrentInvokeKind.Repost)
            {
                CurrentRepost();
            }
            string callback = xInvoke(browser, js);
            if (kind == CurrentInvokeKind.AjaxEvent)
            {
                arg.SetAjax(browser, AjaxBlockFlags.Event, "CurrentInvoke");
            }
            return callback;
        }

        public string CurrentGetHtml()
        {
            this.CheckNavigated();

            var browser = webBrowser1;
            return xGetHtml(browser);
        }

        public void CurrentSnapshot(Size size, Guid? fileID = null)
        {
            this.CheckNavigated();

            var browser = webBrowser1;
            xSnapshot(browser, size, this.SaveFileDirectory, fileID);
        }
        #endregion
    }

    public enum CurrentInvokeKind
    {
        None = 0,
        Repost = 1,
        AjaxEvent = 2
    }
}