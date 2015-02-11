using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    public interface IPageClient : IDisposable
    {
        void SetProxy(EndPoint address, NetworkCredential credential = null);
        void Navigate(Uri requestUrl, HttpRequestContent content = null);

        string CurrentInvoke(string js, CurrentInvokeKind kind = CurrentInvokeKind.None);
        string CurrentGetHtml();
        void CurrentSnapshot(Size size, Guid? fileID = null);
    }
}