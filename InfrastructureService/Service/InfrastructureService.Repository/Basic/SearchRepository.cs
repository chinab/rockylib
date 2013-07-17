using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using InfrastructureService.Common;
using InfrastructureService.Model.Basic;

namespace InfrastructureService.Repository.Basic
{
    public class SearchRepository : RepositoryBase
    {
        #region Fields
        private static readonly EntityStoredProc _spContext;
        #endregion

        #region Constructors
        static SearchRepository()
        {
            _spContext = new EntityStoredProc(DbFactory.GetFactory("spUser"));

            new JobTimer(t =>
            {
                var repository = new SearchRepository();
                repository.FillPinyin();
            }, TimeSpan.FromMinutes(1D)).Start();
        }
        #endregion

        #region Methods
        public QueryAutoCompleteResult.TResult[] QueryAutoComplete(QueryAutoCompleteParameter param, string[] segmentWords)
        {
            param.Keyword = param.Keyword.Replace("'", string.Empty);
            using (var context = base.CreateContext())
            {
                string componentName = param.ComponentKind.ToString();
                var q = from t in context.ComponentInfoes
                        where t.Name == componentName && t.spName != null
                        select t;
                var comInfo = q.Single();
                var rSet = _spContext.Query(comInfo.spName, new object[] { comInfo.RowID, param.Keyword, string.Join(" ", segmentWords) });
                return rSet.GetResult<QueryAutoCompleteResult.TResult>().ToArray();
            }
        }
        internal void FillPinyin()
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.SearchKeywords
                        where t.Pinyin == null
                        select t;
                foreach (var item in q)
                {
                    item.Pinyin = PinyinUtility.ConvertToPinyin(item.Keyword);
                    item.PinyinCaps = PinyinUtility.ConvertToPinyinCaps(item.Keyword);
                }
                context.SaveChanges();
            }
        }

        public string[] QueryHotKeywords(QueryHotKeywordsParameter param)
        {
            using (var context = base.CreateContext())
            {
                string componentName = param.ComponentKind.ToString();
                var q = from t in context.SearchKeywords
                        where t.ComponentInfo.Name == componentName && t.ComponentInfo.spName != null
                        orderby t.Count descending
                        select t.Keyword;
                return q.Take(param.TakeCount).ToArray();
            }
        }
        #endregion
    }
}