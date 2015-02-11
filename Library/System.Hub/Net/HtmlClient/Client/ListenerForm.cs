using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System.Net
{
    public partial class ListenerForm : Form
    {
        private class SessionStruct
        {
            internal readonly MessageLoopApartment MLA = new MessageLoopApartment();

            public int Key { get; private set; }
            public TcpClient Client { get; private set; }
            public ClientForm From { get; set; }

            public SessionStruct(TcpClient client)
            {
                this.Client = client;
                this.Key = ((IPEndPoint)client.Client.RemoteEndPoint).Port;
            }
        }

        internal const int Port = 81;
        private TcpListener _listener;
        private ConcurrentDictionary<int, SessionStruct> _session;

        public ListenerForm()
        {
            InitializeComponent();
            textBox1.ReadOnly = true;

            _session = new ConcurrentDictionary<int, SessionStruct>();
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            TaskHelper.Factory.StartNew(this.Accept);
        }

        private void Accept()
        {
        start:
            var client = _listener.AcceptTcpClient();
            var sess = new SessionStruct(client);
            if (!_session.TryAdd(sess.Key, sess))
            {
                throw new InvalidOperationException("Session.TryAdd");
            }
            TaskHelper.Factory.StartNew(this.OnReceive, sess);
            goto start;
        }

        private void OnReceive(object state)
        {
            var sess = (SessionStruct)state;
            this.Invoke((Action)(() =>
            {
                textBox1.Text = _session.Count.ToString();
            }));
            sess.MLA.Invoke(() =>
            {
                sess.From = new ClientForm();
                sess.From.Show();
            });
            try
            {
                while (sess.Client.Connected)
                {
                    CallPack pCall;
                    if (!sess.Client.Client.Receive(out pCall))
                    {
                        break;
                    }
                    var pReturn = new ReturnPack();
                    string methodName = pCall.Action.ToString();
                    var method = typeof(ClientForm).GetMethod(methodName);
                    if (method == null)
                    {
                        pReturn.Message = "Bad GetMethod";
                        goto done;
                    }
                    try
                    {
                        sess.MLA.Invoke(() =>
                        {
                            App.LogInfo("DoCall:{0} {1}", methodName, pCall.Parameters.Length);
                            pReturn.Value = method.Invoke(sess.From, pCall.Parameters);
                        });
                        var arg = (ScriptingContext)sess.From.Tag;
                        switch (methodName)
                        {
                            case "Navigate":
                                arg.WaitHandle.WaitOne();
                                break;
                            case "CurrentInvoke":
                                switch ((CurrentInvokeKind)pCall.Parameters[1])
                                {
                                    case CurrentInvokeKind.Repost:
                                        arg.WaitHandle.WaitOne();
                                        break;
                                    case CurrentInvokeKind.AjaxEvent:
                                        arg.WaitAjax(sess.From.SendReceiveTimeout, "CurrentInvoke");
                                        break;
                                }
                                break;
                        }
                        pReturn.Ok = true;
                    }
                    catch (Exception ex)
                    {
                        App.LogError(ex, "Bad Invoke:\t{0}", ex.Message);
                        pReturn.Message = string.Format("Bad Invoke:\t{0}", ex.Message);
                    }
                done:
                    sess.Client.Client.Send(pReturn);
                }
            }
            catch (SocketException ex)
            {
                App.LogError(ex, "Session");
            }
            catch (System.IO.IOException ex)
            {
                App.LogError(ex, "Session");
            }
            SessionStruct dump;
            _session.TryRemove(sess.Key, out dump);
            sess.MLA.Invoke(() =>
            {
                sess.From.Close();
            });
            sess.MLA.Dispose();
            this.Invoke((Action)(() =>
            {
                textBox1.Text = _session.Count.ToString();
            }));
        }
    }

    [Serializable]
    public class CallPack
    {
        public enum ActionName
        {
            SetProxy,
            Navigate,
            CurrentInvoke,
            CurrentGetHtml,
            CurrentSnapshot
        }

        public ActionName Action { get; set; }
        public object[] Parameters { get; set; }
    }
    [Serializable]
    public class ReturnPack
    {
        public bool Ok { get; set; }
        public string Message { get; set; }
        public object Value { get; set; }
    }
}