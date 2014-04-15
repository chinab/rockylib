using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EntityFramework.Extensions;
using InfrastructureService.Common;
using InfrastructureService.Model;
using InfrastructureService.Model.Site;
using InfrastructureService.Repository.DataAccess;

namespace InfrastructureService.Repository.Site
{
    public class SiteRepository : RepositoryBase
    {
        #region Feedback
        public void SaveFeedback(FeedbackEntity param)
        {
            using (var context = base.CreateContext())
            {
                Feedback pObj;
                if (param.RowID == Guid.Empty)
                {
                    context.Feedbacks.Add(pObj = new Feedback());
                    EntityMapper.Map<FeedbackEntity, Feedback>(param, pObj);
                    pObj.RowID = Guid.NewGuid();
                    pObj.CreateDate = DateTime.Now;
                }
                else
                {
                    pObj = context.Feedbacks.Single(t => t.RowID == param.RowID);
                    EntityMapper.Map<FeedbackEntity, Feedback>(param, pObj);
                }
                context.SaveChanges();
            }
        }

        public void SaveFeedbackStatus(SetStatusParameter param)
        {
            using (var context = base.CreateContext())
            {
                context.Feedbacks.Update(t => param.RowIDSet.Contains(t.RowID), t => new Feedback() { Status = (int)param.Status });
            }
        }

        public QueryFeedbacksResult QueryFeedbacks(QueryFeedbacksParameter param)
        {
            using (var context = base.CreateContext())
            {
                var result = new QueryFeedbacksResult();
                int status = EnumToValue(StatusKind.Blocked);
                var q = from t in context.Feedbacks
                        where t.AppID == param.AppID
                        && (param.SkipStatus || t.Status != status)
                        && (param.RowID == null || t.RowID == param.RowID)
                        select new FeedbackEntity
                        {
                            AppID = t.AppID,
                            RowID = t.RowID,
                            Name = t.Name,
                            Email = t.Email,
                            Phone = t.Phone,
                            Kind = (FeedbackKind)t.Kind,
                            Content = t.Content,
                            CreateDate = t.CreateDate,
                            Status = (StatusKind)t.Status
                        };
                result.PageResult(q, param);
                return result;
            }
        }
        #endregion

        #region FriendLink
        public void SaveFriendLink(FriendLinkEntity param)
        {
            using (var context = base.CreateContext())
            {
                FriendLink pObj;
                if (param.RowID == Guid.Empty)
                {
                    context.FriendLinks.Add(pObj = new FriendLink());
                    EntityMapper.Map<FriendLinkEntity, FriendLink>(param, pObj);
                    pObj.RowID = Guid.NewGuid();
                    pObj.CreateDate = DateTime.Now;
                }
                else
                {
                    pObj = context.FriendLinks.Single(t => t.RowID == param.RowID);
                    EntityMapper.Map<FriendLinkEntity, FriendLink>(param, pObj);
                }
                context.SaveChanges();
            }
        }

        public void SetFriendLinkStatus(SetStatusParameter param)
        {
            using (var context = base.CreateContext())
            {
                context.FriendLinks.Update(t => param.RowIDSet.Contains(t.RowID), t => new FriendLink() { Status = (int)param.Status });
            }
        }

        public QueryFriendLinksResult QueryFriendLinks(QueryFriendLinksParameter param)
        {
            using (var context = base.CreateContext())
            {
                var result = new QueryFriendLinksResult();
                int status = EnumToValue(StatusKind.Blocked);
                var q = from t in context.FriendLinks
                        where t.AppID == param.AppID
                        && (param.SkipStatus || t.Status != status)
                        && (param.RowID == null || t.RowID == param.RowID)
                        orderby t.Sort ascending
                        select new FriendLinkEntity
                        {
                            AppID = t.AppID,
                            RowID = t.RowID,
                            CreateDate = t.CreateDate,
                            LinkFileKey = t.LinkFileKey,
                            LinkText = t.LinkText,
                            RenderKind = (RenderKind)t.RenderKind,
                            SiteName = t.SiteName,
                            SiteUrl = t.SiteUrl,
                            Sort = t.Sort,
                            Status = (StatusKind)t.Status
                        };
                result.PageResult(q, param);
                return result;
            }
        }
        #endregion

        #region SiteAD
        public void SaveSiteAD(SiteADEntity param)
        {
            using (var context = base.CreateContext())
            {
                SiteAD pObj;
                if (param.RowID == Guid.Empty)
                {
                    context.SiteADs.Add(pObj = new SiteAD());
                    EntityMapper.Map<SiteADEntity, SiteAD>(param, pObj);
                    pObj.RowID = Guid.NewGuid();
                }
                else
                {
                    pObj = context.SiteADs.Single(t => t.RowID == param.RowID);
                    EntityMapper.Map<SiteADEntity, SiteAD>(param, pObj);
                }
                context.SaveChanges();
            }
        }

        public void SetSiteADStatus(SetStatusParameter param)
        {
            using (var context = base.CreateContext())
            {
                context.SiteADs.Update(t => param.RowIDSet.Contains(t.RowID), t => new SiteAD() { Status = (int)param.Status });
            }
        }

        public IQueryable<SiteADEntity> QuerySiteADs(QuerySiteADsParameter param)
        {
            using (var context = new InfrastructureServiceEntities())
            {
                int status = EnumToValue(StatusKind.Blocked);
                var q = from t in context.SiteADs
                        where t.AppID == param.AppID
                        && (param.SkipStatus || t.Status != status)
                        && (param.RowID == null || t.RowID == param.RowID)
                        orderby t.Sort ascending
                        select new SiteADEntity
                        {
                            AppID = t.AppID,
                            RowID = t.RowID,
                            BeginDate = t.BeginDate,
                            EndDate = t.EndDate,
                            FileKey = t.FileKey,
                            Name = t.Name,
                            RenderHeight = t.RenderHeight,
                            RenderWidth = t.RenderWidth,
                            Sort = t.Sort,
                            Status = (StatusKind)t.Status,
                            Url = t.Url
                        };
                return q;
            }
        }
        #endregion
    }
}