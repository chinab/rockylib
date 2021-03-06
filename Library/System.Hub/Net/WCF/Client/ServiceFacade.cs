﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;

namespace System.Net.WCF
{
    public static class ServiceFacade
    {
        public static event EventHandler<InvokeFaultEventArgs> HandleFault;

        private static void OnHandleFault(ICommunicationObject sender, InvokeFaultEventArgs e)
        {
            if (HandleFault != null)
            {
                HandleFault(sender, e);
            }
        }

        public static void Invoke<TClient>(this TClient proxy, Action<TClient> action) where TClient : class, ICommunicationObject
        {
            try
            {
                action(proxy);
            }
            catch (FaultException ex)
            {
                proxy.Abort();
                var e = new InvokeFaultEventArgs(ex);
                OnHandleFault(proxy, e);
                if (e.Throw)
                {
                    throw;
                }
            }
            catch (SystemException)
            {
                proxy.Abort();
                throw;
            }
        }
        public static TReturn Invoke<TClient, TReturn>(this TClient proxy, Func<TClient, TReturn> func) where TClient : class, ICommunicationObject
        {
            TReturn returnValue = default(TReturn);
            try
            {
                returnValue = func(proxy);
            }
            catch (FaultException ex)
            {
                proxy.Abort();
                var e = new InvokeFaultEventArgs(ex);
                OnHandleFault(proxy, e);
                if (e.Throw)
                {
                    throw;
                }
            }
            catch (SystemException)
            {
                proxy.Abort();
                throw;
            }
            return returnValue;
        }

        public static ICommunicationObject CreateProxy(Type contractType)
        {
            string proxyName;
            if (contractType.IsInterface)
            {
                proxyName = string.Concat(contractType.Namespace, ".", contractType.Name.Remove(0, 1), "Client");
            }
            else
            {
                proxyName = string.Concat(contractType.Namespace, ".", contractType.Name);
            }
            return (ICommunicationObject)Activator.CreateInstance(contractType.Assembly.GetType(proxyName, true));
        }
    }

    public class InvokeFaultEventArgs : EventArgs
    {
        public bool Throw { get; set; }
        public FaultException UnknownFault { get; private set; }

        public InvokeFaultEventArgs(FaultException unknownFault)
        {
            this.Throw = true;
            this.UnknownFault = unknownFault;
        }

        public bool TryGetDetail(out InvokeFaultDetail detail)
        {
            detail = null;
            var messageFault = this.UnknownFault.CreateMessageFault();
            if (!messageFault.HasDetail)
            {
                return false;
            }
            detail = messageFault.GetDetail<InvokeFaultDetail>();
            return true;
        }
    }
}