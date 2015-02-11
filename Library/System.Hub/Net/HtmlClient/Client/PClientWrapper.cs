using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
    public sealed class PClientWrapper : Disposable, IPageClient
    {
        private TcpClient _client;

        public PClientWrapper()
        {
            _client = new TcpClient();
            _client.Connect(IPAddress.Loopback, ListenerForm.Port);
        }
        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                _client.Close();
            }
            _client = null;
        }

        private object Call(CallPack.ActionName action, object[] parameter)
        {
            base.CheckDisposed();

            _client.Client.Send(new CallPack()
            {
                Action = action,
                Parameters = parameter
            });
            ReturnPack rPack;
            if (!_client.Client.Receive(out rPack))
            {
                this.Dispose();
                return null;
            }
            if (!rPack.Ok)
            {
                throw new InvalidOperationException(rPack.Message);
            }
            return rPack.Value;
        }

        public void SetProxy(EndPoint address, NetworkCredential credential = null)
        {
            Call(CallPack.ActionName.SetProxy, new object[] { address, credential });
        }
        public void Navigate(Uri requestUrl, HttpRequestContent content = null)
        {
            Call(CallPack.ActionName.Navigate, new object[] { requestUrl, content });
        }

        public string CurrentInvoke(string js, CurrentInvokeKind kind = CurrentInvokeKind.None)
        {
            return (string)Call(CallPack.ActionName.CurrentInvoke, new object[] { js, kind });
        }
        public string CurrentGetHtml()
        {
            return (string)Call(CallPack.ActionName.CurrentGetHtml, new object[0]);
        }
        public void CurrentSnapshot(Size size, Guid? fileID = null)
        {
            Call(CallPack.ActionName.CurrentSnapshot, new object[] { size, fileID });
        }
    }
}