using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using InfrastructureService.Model;
using InfrastructureService.Model.Site;

namespace InfrastructureService.Contract
{
    [ServiceContract]
    public partial interface ISiteService
    {
        [OperationContract]
        void SaveAdmin(AdminEntity param);
        [OperationContract]
        void SetAdminStatus(SetStatusParameter param);
        [OperationContract]
        SignInResult SignIn(SignInParameter param);
        [OperationContract]
        void ChangePassword(ChangePasswordParameter param);
        [OperationContract]
        QueryAdminsResult QueryAdmins(QueryAdminsParameter pager);

        [OperationContract]
        void SaveFeedback(FeedbackEntity param);
        [OperationContract]
        void SaveFeedbackStatus(SetStatusParameter param);
        [OperationContract]
        QueryFeedbacksResult QueryFeedbacks(QueryFeedbacksParameter pager);

        [OperationContract]
        void SaveFriendLink(FriendLinkEntity param);
        [OperationContract]
        void SetFriendLinkStatus(SetStatusParameter param);
        [OperationContract]
        QueryFriendLinksResult QueryFriendLinks(QueryFriendLinksParameter pager);

        [OperationContract]
        void SaveSiteAD(SiteADEntity param);
        [OperationContract]
        void SetSiteADStatus(SetStatusParameter param);
        [OperationContract]
        QuerySiteADsResult QuerySiteADs(QuerySiteADsParameter pager);
    }
}