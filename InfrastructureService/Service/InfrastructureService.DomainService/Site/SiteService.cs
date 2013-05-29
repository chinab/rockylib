using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using InfrastructureService.Contract;
using InfrastructureService.Model;
using InfrastructureService.Model.Site;
using InfrastructureService.Repository.Site;

namespace InfrastructureService.DomainService
{
    public class SiteService : ServiceBase, ISiteService
    {
        #region Admin
        public void SaveAdmin(AdminEntity param)
        {
            var repository = new AdminRepository();
            repository.SaveAdmin(param);
        }

        public void SetAdminStatus(SetStatusParameter param)
        {
            var repository = new AdminRepository();
            repository.SetAdminStatus(param);
        }

        public SignInResult SignIn(SignInParameter param)
        {
            var repository = new AdminRepository();
            return repository.SignIn(param);
        }

        public void ChangePassword(ChangePasswordParameter param)
        {
            var repository = new AdminRepository();
            repository.ChangePassword(param);
        }

        public QueryAdminsResult QueryAdmins(QueryAdminsParameter param)
        {
            var repository = new AdminRepository();
            return repository.QueryAdmins(param);
        }
        #endregion

        #region Feedback
        public void SaveFeedback(FeedbackEntity param)
        {
            var repository = new SiteRepository();
            repository.SaveFeedback(param);
        }

        public void SaveFeedbackStatus(SetStatusParameter param)
        {
            var repository = new SiteRepository();
            repository.SaveFeedbackStatus(param);
        }

        public QueryFeedbacksResult QueryFeedbacks(QueryFeedbacksParameter param)
        {
            var repository = new SiteRepository();
            return repository.QueryFeedbacks(param);
        }
        #endregion

        #region FriendLink
        public void SaveFriendLink(FriendLinkEntity param)
        {
            var repository = new SiteRepository();
            repository.SaveFriendLink(param);
        }

        public void SetFriendLinkStatus(SetStatusParameter param)
        {
            var repository = new SiteRepository();
            repository.SetFriendLinkStatus(param);
        }

        public QueryFriendLinksResult QueryFriendLinks(QueryFriendLinksParameter param)
        {
            var repository = new SiteRepository();
            return repository.QueryFriendLinks(param);
        }
        #endregion

        #region SiteAD
        public void SaveSiteAD(SiteADEntity param)
        {
            var repository = new SiteRepository();
            repository.SaveSiteAD(param);
        }

        public void SetSiteADStatus(SetStatusParameter param)
        {
            var repository = new SiteRepository();
            repository.SetSiteADStatus(param);
        }

        public QuerySiteADsResult QuerySiteADs(QuerySiteADsParameter param)
        {
            var result = new QuerySiteADsResult();
            var repository = new SiteRepository();
            result.PageResult(repository.QuerySiteADs(param), param);
            return result;
        }
        #endregion

        #region News
        public void SaveCategory(SaveCategoryParameter param)
        {
            var repository = new NewsRepository();
            repository.SaveCategory(param);
        }

        public void DeleteCategories(DeleteCategoriesParameter param)
        {
            var repository = new NewsRepository();
            repository.DeleteCategories(param);
        }

        public QueryCategoriesResult QueryCategories(QueryCategoriesParameter param)
        {
            var result = new QueryCategoriesResult();
            var repository = new NewsRepository();
            result.ResultSet = repository.QueryCategories(param);
            return result;
        }

        public void SaveNews(SaveNewsParameter param)
        {
            var repository = new NewsRepository();
            repository.SaveNews(param);
        }

        public void SetNewsStatus(SetNewsStatusParameter param)
        {
            var repository = new NewsRepository();
            repository.SetNewsStatus(param);
        }

        public QueryNewsDetailResult QueryNewsDetail(QueryNewsDetailParameter param)
        {
            var repository = new NewsRepository();
            return repository.QueryNewsDetail(param);
        }

        public QueryNewsResult QueryNews(QueryNewsParameter param)
        {
            var repository = new NewsRepository();
            return repository.QueryNews(param);
        }

        public string[] QueryTags(QueryTagsParameter param)
        {
            var repository = new NewsRepository();
            return repository.QueryTags(param);
        }
        #endregion
    }
}