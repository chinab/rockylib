using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace System.Net
{
    public class HolePunchingClient : Disposable
    {
        private Socket _client;
        private Socket _xlistener, _xConnector, _holePunchedSocket;

        public HolePunchingClient(ushort localPort, IPEndPoint serverIpe)
        {
            _client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _client.ReuseAddress(new IPEndPoint(IPAddress.Any, localPort));
            _client.Connect(serverIpe);
            TaskHelper.Factory.StartNew(this.ProcessRequest, null);
        }
        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                _client.Disconnect();
                _holePunchedSocket.Close();
            }
            _client.Close();
            _holePunchedSocket = null;
        }

        private void ProcessRequest(object state)
        {
            while (_client.Connected)
            {
                try
                {
                    HolePunchingModel model;
                    _client.Receive(out model);
                    if (!_client.Connected)
                    {
                        break;
                    }
                    switch (model.Command)
                    {
                        case HolePunchingCommand.ConnectClient:
                            Console.WriteLine("HP will be done towards this address: " + model.RemoteEndPoint);

                            _xConnector = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            _xConnector.ReuseAddress((IPEndPoint)_client.LocalEndPoint);
                            _xlistener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            _xlistener.ReuseAddress((IPEndPoint)_client.LocalEndPoint);

                            TaskHelper.Factory.StartNew(this.ProcessConnect, model.RemoteEndPoint);
                            TaskHelper.Factory.StartNew(this.ProcessListen, null);
                            break;
                    }
                }
                catch (SocketException ex)
                {
                    TunnelExceptionHandler.Handle(ex, _client, "HolePunchingClient");
                }
            }
        }

        private void ProcessConnect(object state)
        {
            IPEndPoint ipe = (IPEndPoint)state;
            try
            {
                _xConnector.Connect(ipe);
                _xlistener.Close();
                _holePunchedSocket = _xConnector;
                Console.WriteLine("Connect to: " + ipe);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("In ProcessClientRequest: " + ex.Message);
            }
        }

        private void ProcessListen(object state)
        {
            try
            {
                Socket client = _xlistener.Accept();
                _xConnector.Close();
                _holePunchedSocket = client;
                Console.WriteLine("Accept from: " + client.RemoteEndPoint);
            }
            catch (SocketException ex)
            {
                Console.WriteLine("In ProcessServerRequest: " + ex.Message);
            }
        }

        public Socket HolePunch(IPAddress otherIP)
        {
            var model = new HolePunchingModel();
            model.RequestIP = otherIP;
            _client.Send(model);
            while (_holePunchedSocket == null)
            {
                Thread.Sleep(2000);
            }
            return _holePunchedSocket;
        }
    }
}