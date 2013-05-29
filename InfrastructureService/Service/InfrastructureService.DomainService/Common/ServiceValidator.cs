using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IdentityModel.Selectors;
using System.ServiceModel;

namespace InfrastructureService.DomainService
{
    public class ServiceValidator : UserNamePasswordValidator
    {
        /// <summary>
        /// WCF中调用OperationContext.Current.ServiceSecurityContext.PrimaryIdentity获取用户名
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        public override void Validate(string userName, string password)
        {
            if (userName != "azure" || password != "Timothy.net")
            {
                throw new FaultException("Unknown username or incorrect password");
            }
        }
    }
}