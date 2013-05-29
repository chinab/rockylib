using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using InfrastructureService.Model.User;

namespace InfrastructureService.Contract
{
    [ServiceContract]
    public interface IUserService
    {
        [OperationContract]
        bool IsUserNameExists(IsUserNameExistsParameter param);
        [OperationContract]
        void SignUp(SignUpParameter param);
        [OperationContract]
        SSOIdentity SignIn(SignInParameter param);
        [OperationContract]
        SSOIdentity OAuth(OAuthParameter param);
        [OperationContract]
        SSOIdentity GetIdentity(GetIdentityParameter param);
        [OperationContract]
        void SignOut(string token);
        [OperationContract]
        GetServerConfigResult GetServerConfig();

        [OperationContract]
        void SendAuthEmail(SendAuthEmailParameter param);
        [OperationContract]
        void VerifyEmail(Guid authCode);
        [OperationContract]
        void SendAuthMobile(SendAuthMobileParameter param);
        [OperationContract]
        void VerifyMobile(VerifyMobileParameter param);

        [OperationContract]
        void SendFindPwdCode(SendFindPwdCodeParameter param);
        [OperationContract]
        void ChangePassword(ChangePasswordParameter param);
        [OperationContract]
        QueryUsersResult QueryUsers(QueryUsersParameter pager);
        [OperationContract]
        QuerySignInLogsResult QuerySignInLogs(QuerySignInLogsParameter pager);
    }
}