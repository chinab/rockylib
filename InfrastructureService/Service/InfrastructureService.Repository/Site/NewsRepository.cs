using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using EntityFramework.Extensions;
using InfrastructureService.Model;
using InfrastructureService.Model.Site;
using InfrastructureService.Repository.DataAccess;
using System.Net.WCF;

namespace InfrastructureService.Repository.Site
{
    public class NewsRepository : RepositoryBase
    {
        #region Category
        #region Fields
        private static System.Reflection.MethodBase _QueryCategories = typeof(NewsRepository).GetMethod("QueryCategories");
        #endregion

        #region Methods
        internal List<int> GetDeepChildID(int categoryID)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.NewsCategories
                        where t.RowID == categoryID
                        select t.Path;
                string path = q.SingleOrDefault();
                if (path == null)
                {
                    throw new ArgumentException(string.Format("CategoryID {0} isn't exist.", categoryID));
                }
                path += ",";
                var q2 = from t in context.NewsCategories
                         where t.Path.StartsWith(path)
                         select t.RowID;
                var paramSet = q2.ToList();
                if (paramSet.Count == 0)
                {
                    paramSet.Add(categoryID);
                }
                return paramSet;
            }
        }

        internal List<int> GetDeepParentID(int categoryID, bool withMe)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.NewsCategories
                        where t.RowID == categoryID
                        select t.Path;
                string path = q.SingleOrDefault();
                if (path == null)
                {
                    throw new ArgumentException(string.Format("CategoryID {0} isn't exist.", categoryID));
                }
                var paramSet = path.Split(',').Select(t => int.Parse(t)).ToList();
                if (!withMe)
                {
                    paramSet.RemoveAt(paramSet.Count - 1);
                }
                return paramSet;
            }
        }
        #endregion

        #region Write
        public void SaveCategory(SaveCategoryParameter param)
        {
            using (var scope = DbScope.Create())
            using (var context = base.CreateContext())
            {
                scope.BeginTransaction();
                if (param.ParentID > 0 && context.News.Any(t => t.CategoryID == param.ParentID))
                {
                    throw new InvalidInvokeException("不能更新有新闻的父分类");
                }

                NewsCategory pObj;
                if (param.RowID > 0)
                {
                    pObj = context.NewsCategories.Single(t => t.RowID == param.RowID);
                    EntityMapper.Map<SaveCategoryParameter, NewsCategory>(param, pObj);
                }
                else
                {
                    context.NewsCategories.Add(pObj = new NewsCategory());
                    EntityMapper.Map<SaveCategoryParameter, NewsCategory>(param, pObj);
                    bool isTop = pObj.ParentID == 0;
                    if (isTop)
                    {
                        pObj.Path = string.Empty;
                    }
                    else
                    {
                        var paramSet = GetDeepParentID(pObj.ParentID, true);
                        pObj.Path = string.Join(",", paramSet);
                    }
                    if (!isTop)
                    {
                        pObj.Path += ",";
                    }
                    pObj.Path += pObj.RowID.ToString();
                    param.RowID = pObj.RowID;
                }
                context.SaveChanges();

                //开始执行Path迁移
                string sPath = string.Format(",{0},", param.RowID);
                var q = from t in context.NewsCategories
                        where t.AppID == param.AppID && t.Path.Contains(sPath)
                        select t;
                int level;
                var path = new StringBuilder();
                foreach (var item in q)
                {
                    if (item.ParentID == 0)
                    {
                        item.Path = item.RowID.ToString();
                    }
                    else
                    {
                        level = 0;
                        path.Length = 0;
                        var current = item;
                        do
                        {
                            level++;
                            path.Insert(0, "," + current.RowID.ToString());
                        }
                        while ((current = context.NewsCategories.Where(t => t.RowID == param.ParentID).Single()).ParentID != 0);
                        path.Insert(0, current.RowID);

                        item.Level = level;
                        item.Path = path.ToString();
                    }
                }
                context.SaveChanges();
                scope.Complete();
            }

            CacheInterceptorAttribute.ClearCache(_QueryCategories);
        }

        public void DeleteCategories(DeleteCategoriesParameter param)
        {
            using (var scope = DbScope.Create())
            using (var context = base.CreateContext())
            {
                scope.BeginTransaction();
                var q = context.NewsCategories.Where(t => param.RowIDSet.Contains(t.RowID));
                foreach (var item in q)
                {
                    if (context.News.Any(t => t.CategoryID == item.RowID))
                    {
                        throw new InvalidInvokeException("不能删除有新闻的分类");
                    }

                    //更新子类的父类为被删除类别的父类。
                    context.NewsCategories.Update(t => t.ParentID == item.RowID, t => new NewsCategory { ParentID = item.ParentID });
                }
                q.Delete();
                scope.Complete();
            }

            CacheInterceptorAttribute.ClearCache(_QueryCategories);
        }
        #endregion

        [CacheInterceptor(10D, true, AspectPriority = 1)]
        public QueryCategoriesResult.TResult[] QueryCategories(QueryCategoriesParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.NewsCategories
                        where t.AppID == param.AppID
                        && (string.IsNullOrEmpty(param.Name) || t.Name == param.Name)
                        orderby t.Sort
                        select new QueryCategoriesResult.TResult
                        {
                            RowID = t.RowID,
                            ParentID = t.ParentID,
                            Name = t.Name,
                            Sort = t.Sort,
                            Level = t.Level,
                            Path = t.Path,
                            SEO_Keyword = t.SEO_Keyword,
                            SEO_Description = t.SEO_Description
                        };
                if (param.ParentID != null)
                {
                    if (param.DeepQuery)
                    {
                        var childID = GetDeepChildID(param.ParentID.Value);
                        q = q.Where(t => childID.Contains(t.RowID));
                    }
                    else
                    {
                        q = q.Where(t => t.ParentID == param.ParentID);
                    }
                }

                return q.ToArray();
            }
        }
        #endregion

        #region News
        #region Write
        public void SaveNews(SaveNewsParameter param)
        {
            using (var context = base.CreateContext())
            {
                News pObj;
                if (param.RowID > 0)
                {
                    pObj = context.News.Single(t => t.RowID == param.RowID);
                    EntityMapper.Map<SaveNewsParameter, News>(param, pObj);
                }
                else
                {
                    context.News.Add(pObj = new News());
                    EntityMapper.Map<SaveNewsParameter, News>(param, pObj);
                    pObj.CreateDate = DateTime.Now;
                }
                context.SaveChanges();
            }
        }

        public void SetNewsStatus(SetNewsStatusParameter param)
        {
            using (var context = base.CreateContext())
            {
                context.News.Update(t => param.RowIDSet.Contains(t.RowID), t => new News { Status = (int)param.Status });
            }
        }
        #endregion

        public QueryNewsDetailResult QueryNewsDetail(QueryNewsDetailParameter param)
        {
            var result = new QueryNewsDetailResult();
            using (var context = base.CreateContext())
            {
                result.News = this.QueryNews(new QueryNewsParameter()
                {
                    AppID = param.AppID,
                    RowID = param.RowID
                }).ResultSet.Single();
                var q = from t in context.News
                        where t.RowID < param.RowID
                        orderby t.RowID descending
                        select new QueryNewsDetailResult.NewsSimpleResult
                        {
                            RowID = t.RowID,
                            Title = t.Title
                        };
                result.PreviousNews = q.FirstOrDefault();
                q = from t in context.News
                    where t.RowID > param.RowID
                    orderby t.RowID ascending
                    select new QueryNewsDetailResult.NewsSimpleResult
                    {
                        RowID = t.RowID,
                        Title = t.Title
                    };
                result.NextNews = q.FirstOrDefault();
                context.News.Update(t => t.RowID == param.RowID, t => new News() { ViewCount = t.ViewCount + 1 });
            }
            return result;
        }

        public QueryNewsResult QueryNews(QueryNewsParameter param)
        {
            using (var context = base.CreateContext())
            {
                var result = new QueryNewsResult();
                var childID = param.CategoryID == null ? new List<int>() : GetDeepChildID(param.CategoryID.Value);
                int blockedStatus = EnumToValue(StatusKind.Blocked);
                int? status = EnumToValue(param.Status),
                    flags = EnumToValue(param.Flags);
                var q = from t in context.News
                        join t2 in context.NewsCategories on t.CategoryID equals t2.RowID
                        where t.AppID == param.AppID
                        && (param.SkipStatus || (status == null ? t.Status != blockedStatus : t.Status == status))
                        && (param.RowID == null || t.RowID == param.RowID)
                        && (childID.Count == 0 || childID.Contains(t.CategoryID))
                        && (flags == null || (t.Flags & flags) == flags)
                        && (!param.HasImage || t.ImageFileKey != null)
                        && (string.IsNullOrEmpty(param.Keyword) || t.Title.Contains(param.Keyword))
                        orderby t.CreateDate descending
                        select new QueryNewsResult.TResult
                        {
                            CategoryID = t2.RowID,
                            CategoryName = t2.Name,
                            RowID = t.RowID,
                            Title = t.Title,
                            Content = t.Content,
                            Author = t.Author,
                            Origin = t.Origin,
                            ViewCount = t.ViewCount,
                            ImageFileKey = t.ImageFileKey,
                            AttachmentFileKey = t.AttachmentFileKey,
                            Tag = t.Tag,
                            Flags = (NewsFlags)t.Flags,
                            Status = (StatusKind)t.Status,
                            CreateDate = t.CreateDate
                        };
                result.PageResult(q, param);
                return result;
            }
        }

        /// <summary>
        /// 获取热门标签
        /// </summary>
        /// <param name="QueryTagsParameter"></param>
        /// <returns></returns>
        public string[] QueryTags(QueryTagsParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.News
                        where t.AppID == param.AppID
                        && t.Tag.Length > 0
                        orderby t.ViewCount descending
                        select t.Tag;
                var q2 = from t in q.Take(param.TakeCount).ToList()
                         from t2 in t.Split(',')
                         group t by t2 into g
                         orderby g.Count() descending
                         select g.Key;
                return q2.Take(param.TakeCount).ToArray();
            }
        }
        #endregion
    }
}