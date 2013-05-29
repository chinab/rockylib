using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using InfrastructureService.Contract;
using InfrastructureService.Model.Message;
using InfrastructureService.Repository.Message;

namespace InfrastructureService.DomainService
{
    public class MessageService : ServiceBase, IMessageService
    {
        public void SendEmail(SendEmailParameter param)
        {
            var repository = new MessageRepository();
            repository.SendEmail(param);
        }

        public void SendSMS(SendSMSParameter param)
        {
            var repository = new MessageRepository();
            repository.SendSMS(param);
        }

        public decimal GetSMSBalance(Guid configID)
        {
            var repository = new MessageRepository();
            return repository.GetSMSBalance(configID);
        }
    }
}