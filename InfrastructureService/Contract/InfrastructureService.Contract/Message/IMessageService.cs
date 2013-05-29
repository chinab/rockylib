using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using InfrastructureService.Model.Message;

namespace InfrastructureService.Contract
{
    [ServiceContract]
    public interface IMessageService
    {
        [OperationContract]
        void SendEmail(SendEmailParameter param);
        [OperationContract]
        void SendSMS(SendSMSParameter param);
    }
}