using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Collections.Specialized;
using System.Net;

namespace System.Net
{
    public sealed class HttpRequestContent
    {
        #region NestedTypes
        [Flags]
        public enum ContentKind
        {
            None = 0,
            Form = 1 << 0,
            Files = 1 << 1,
            All = Form | Files
        }
        #endregion

        #region Fields
        internal const string HeaderFormat = "{0}: {1}",
            FormFormat = "{0}={1}",
            Symbol_AndFirst = "?",
            Symbol_And = "&";

        private WebHeaderCollection _header;
        private CookieCollection _cookies;
        private NameValueCollection _form;
        private List<HttpFileContent> _files;
        #endregion

        #region Properties
        public WebHeaderCollection Headers
        {
            get
            {
                if (_header == null)
                {
                    _header = new WebHeaderCollection();
                }
                return _header;
            }
            set { _header = value; }
        }
        public CookieCollection Cookies
        {
            get
            {
                if (_cookies == null)
                {
                    _cookies = new CookieCollection();
                }
                return _cookies;
            }
        }
        public NameValueCollection Form
        {
            get
            {
                if (_form == null)
                {
                    _form = new NameValueCollection();
                }
                return _form;
            }
            set { _form = value; }
        }
        public List<HttpFileContent> Files
        {
            get
            {
                if (_files == null)
                {
                    _files = new List<HttpFileContent>();
                }
                return _files;
            }
        }
        public bool HasValue
        {
            get
            {
                return !_form.IsNullOrEmpty() || !_files.IsNullOrEmpty();
            }
        }
        #endregion

        #region Methods
        public void AppendHeadersTo(WebHeaderCollection header)
        {
            Contract.Requires(header != null);

            var srcHeader = this.Headers;
            for (int i = 0; i < srcHeader.Count; i++)
            {
                header[srcHeader.GetKey(i)] = srcHeader.Get(i);
            }
        }

        public string GetHeadersString(WebHeaderCollection header = null)
        {
            if (header == null)
            {
                header = _header;
            }
            if (header.IsNullOrEmpty())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendFormat(HeaderFormat, _header.GetKey(0), _header.Get(0));
            for (int i = 1; i < _header.Count; i++)
            {
                sb.Append(Environment.NewLine)
                    .AppendFormat(HeaderFormat, _header.GetKey(i), _header.Get(i));
            }
            return sb.ToString();
        }

        public string GetFormString(NameValueCollection form = null, bool? andFirst = null)
        {
            if (form == null)
            {
                form = _form;
            }
            if (form.IsNullOrEmpty())
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            if (andFirst.HasValue)
            {
                sb.Append(andFirst.Value ? Symbol_AndFirst : Symbol_And);
            }
            sb.AppendFormat(FormFormat, form.GetKey(0), form.Get(0));
            for (int i = 1; i < form.Count; i++)
            {
                sb.Append(Symbol_And).AppendFormat(FormFormat, form.GetKey(i), form.Get(i));
            }
            return sb.ToString();
        }

        public void Clear(ContentKind kind = ContentKind.All)
        {
            if (kind.HasFlag(ContentKind.Form) && _form != null)
            {
                _form.Clear();
            }
            if (kind.HasFlag(ContentKind.Files) && _files != null)
            {
                _files.Clear();
            }
        }
        #endregion
    }
}