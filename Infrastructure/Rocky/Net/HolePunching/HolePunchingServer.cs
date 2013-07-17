using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace System.Net
{
    public class HolePunchingServer : Disposable
    {
        #region Fields
        private Socket _listener;
        private ConcurrentDictionary<IPEndPoint, Socket> _registered;
        #endregion

        #region Constructors
        public HolePunchingServer(ushort listenPort, ushort maxClient = 100)
        {
            _registered = new ConcurrentDictionary<IPEndPoint, Socket>();

            _listener = SocketHelper.CreateListener(new IPEndPoint(IPAddress.Any, listenPort), maxClient);
            _listener.BeginAccept(this.AcceptCallback, null);
        }

        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                SocketHelper.DisposeListener(ref _listener);
            }
            _listener = null;
        }
        #endregion

        #region Methods
        private void AcceptCallback(IAsyncResult ar)
        {
            if (_listener == null)
            {
                return;
            }
            _listener.BeginAccept(this.AcceptCallback, null);

            var client = _listener.EndAccept(ar);
            TaskHelper.Factory.StartNew(this.ProcessRequest, client);
        }

        /// <summary>
        /// ToDo: handle the exception that is thrown in the server when the client close the socket and the server is still reading this socket.
        /// Possible solution: Make every part of the switch (register, request, unregister) as functions,
        /// then call the unregister function when a SocketException (is this the exception?) is thrown. 
        /// </summary>
        /// <param name="state"></param>
        private void ProcessRequest(object state)
        {
            var client = (Socket)state;
            var remoteIpe = (IPEndPoint)client.RemoteEndPoint;
#if DEBUG
            Hub.LogInfo("Registration for: " + remoteIpe);
#endif
            _registered.TryAdd(remoteIpe, client);
            try
            {
                while (client.Connected)
                {
                    HolePunchingModel model;
                    client.Receive(out model);
                    if (!client.Connected)
                    {
                        break;
                    }
                    switch (model.Command)
                    {
                        case HolePunchingCommand.RequestClient:
                            var pair = _registered.Where(t => t.Key.Address == model.RequestIP).FirstOrDefault();
                            var requestedIpe = pair.Key;
                            var requestedClient = pair.Value;
                            if (requestedIpe == null || !requestedClient.Connected)
                            {
                                //对方已断线
                                return;
                            }
#if DEBUG
                            Hub.LogInfo(remoteIpe + " requested parameters of: " + requestedIpe);
#endif
                            model.Command = HolePunchingCommand.ConnectClient;

                            model.RemoteEndPoint = requestedIpe;
                            client.Send(model);

                            model.RemoteEndPoint = remoteIpe;
                            requestedClient.Send(model);
                            break;
                    }
                }
            }
            catch (SocketException ex)
            {
                TunnelExceptionHandler.Handle(ex, client, "HolePunchingServer");
            }
            finally
            {
#if DEBUG
                Hub.LogInfo("Unregistration for: " + remoteIpe);
#endif
                Socket dummy;
                _registered.TryRemove(remoteIpe, out dummy);
            }
        }
        #endregion
    }
}