using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using InfrastructureService.Model.Basic;

namespace InfrastructureService.Contract
{
    [ServiceContract]
    public interface IInfrastructureService
    {
        #region Message
        [OperationContract]
        void SendEmail(SendEmailParameter param);

        [OperationContract]
        void SendSMS(SendSMSParameter param);
        #endregion

        #region Proxy

        #endregion

        #region File
        [OperationContract]
        StorageConfig GetConfig();

        [OperationContract]
        void SaveFile(SaveFileParameter param);

        [OperationContract]
        QueryFileResult QueryFile(QueryFileParameter param);

        [OperationContract]
        string GetFileUrl(GetFileUrlParameter param);
        #endregion
    }
}