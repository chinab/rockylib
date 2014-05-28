using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;

namespace System.Net.WCF
{
    public abstract class ServiceBase : IErrorHandler, IServiceBehavior
    {
        public bool HandleError(Exception error)
        {
            App.LogError(error, "ServiceBase");
            return false;
        }

        public void ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            var invokeFault = new InvokeFaultDetail()
            {
                FaultLevel = InvokeFaultLevel.SystemUnusual,
                Exception = new ExceptionDetail(error)
            };
            var ex = error as InvalidInvokeException;
            if (ex != null)
            {
                invokeFault.FaultLevel = ex.FaultLevel;
            }
            var faultException = new FaultException<InvokeFaultDetail>(invokeFault, error.Message);
            var messageFault = faultException.CreateMessageFault();
            fault = Message.CreateMessage(version, messageFault, faultException.Action);
        }

        void IServiceBehavior.AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters)
        {
            //throw new NotImplementedException();
        }

        void IServiceBehavior.ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher dispatcher in serviceHostBase.ChannelDispatchers)
            {
                dispatcher.ErrorHandlers.Add(this);
            }
        }

        void IServiceBehavior.Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            //throw new NotImplementedException();
        }
    }
}