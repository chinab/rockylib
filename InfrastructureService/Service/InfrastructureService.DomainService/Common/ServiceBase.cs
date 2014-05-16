using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using InfrastructureService.Common;

namespace InfrastructureService.DomainService
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
            var appFault = new AppFaultDetail()
            {
                ErrorCode = -1,
                Exception = new ExceptionDetail(error)
            };
            var domainEx = error as DomainException;
            if (domainEx != null)
            {
                appFault.ErrorCode = domainEx.ErrorCode;
                appFault.ThrowFault = domainEx.ExceptionLevel == DomainExceptionLevel.OperationException;
            }
            var faultException = new FaultException<AppFaultDetail>(appFault, error.Message);
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