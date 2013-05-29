using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rocky.Net
{
    internal static class TunnelExceptionHandler
    {
        public static void Handle(Exception ex, string message)
        {
            var stateMissingEx = ex as TunnelStateMissingException;
            if (stateMissingEx != null)
            {
                Runtime.LogInfo("{0} SockHandle={1} {2}", message, stateMissingEx.Client.Handle, stateMissingEx.Message);
                return;
            }
            Runtime.LogError(ex, message);
        }

        public static bool Handle(WebException ex, string message, TextWriter output = null)
        {
            var msg = new StringBuilder();
            var res = (HttpWebResponse)ex.Response;
            if (res != null)
            {
                switch (res.StatusCode)
                {
                    case HttpStatusCode.Forbidden:
                        output.WriteLine("Server Rejected.");
                        return true;
                    case HttpStatusCode.BadGateway:
                        output.WriteLine("{0} Connect Failure, Please contact the administrator.", message);
                        return true;
                    case HttpStatusCode.GatewayTimeout:
                        output.WriteLine("{0} Connect Timeout.", message);
                        return true;
                }
                msg.AppendFormat("ResponseUri={0}, ContentType={1}, HttpStatus={2}, ", res.ResponseUri, res.ContentType, res.StatusDescription);
            }
            msg.AppendFormat("Status={0}\t", ex.Status);
            msg.Append(message);
            Runtime.LogError(ex, msg.ToString());
            return false;
        }

        public static void Handle(IOException ex, Socket client, string message)
        {
            var msg = new StringBuilder();
            var sockEx = ex.InnerException as SocketException;
            if (sockEx != null)
            {
                msg.AppendFormat("SockHandle={0}, ErrorCode={1}\t", client.Handle, sockEx.SocketErrorCode);
                msg.Append(message);
                switch (sockEx.SocketErrorCode)
                {
                    case SocketError.ConnectionReset:
                    case SocketError.ConnectionAborted:
                        msg.Insert(0, "Exception disconnect ");
                        Runtime.LogInfo(msg.ToString());
                        return;
                }
            }
            Runtime.LogError(ex, message);
        }

        public static void Handle(SocketException ex, Socket client, string message)
        {
            var msg = new StringBuilder();
            msg.AppendFormat("SockHandle={0}, ErrorCode={1}\t", client.Handle, ex.SocketErrorCode);
            msg.Append(message);
            //远程主机强制关闭
            switch (ex.SocketErrorCode)
            {
                case SocketError.ConnectionReset:
                //打洞错误
                case SocketError.ConnectionAborted:
                    msg.Insert(0, "Exception disconnect ");
                    Runtime.LogInfo(msg.ToString());
                    return;
            }
            Runtime.LogError(ex, msg.ToString());
        }
    }
}