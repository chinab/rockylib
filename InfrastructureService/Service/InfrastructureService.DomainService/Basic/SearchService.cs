using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfrastructureService.Contract;
using InfrastructureService.Model.Basic;
using InfrastructureService.Repository.Basic;
using System.Net.WCF;

namespace InfrastructureService.DomainService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的类名“SearchService”。
    public class SearchService : ServiceBase, ISearchService
    {
        private static readonly string DictPath = App.CombinePath(@"App_Data\SharpICTCLAS\");

        public SegmentWordResult SegmentWord(SegmentWordParameter param)
        {
            var result = new SegmentWordResult();
            if (param.Keyword == null)
            {
                throw new InvalidInvokeException("参数为空");
            }
            param.Keyword = param.Keyword.Replace("'", string.Empty);
            if (param.Keyword.Length == 0 || param.Keyword.Length > 50)
            {
                throw new InvalidInvokeException("参数错误");
            }
            var sample = new WordSegmentApp(DictPath, 2);
            char c = ' ';
            if (param.Keyword.Contains(c))
            {
                var keywords = param.Keyword.Split(c);
                var list = new List<string>(keywords.Length);
                for (int i = 0; i < keywords.Length; i++)
                {
                    list.AddRange(sample.Segment(keywords[i]));
                }
                result.Words = list.ToArray();
            }
            else
            {
                result.Words = sample.Segment(param.Keyword);
            }
            return result;
        }

        public QueryAutoCompleteResult AutoComplete(QueryAutoCompleteParameter param)
        {
            var result = new QueryAutoCompleteResult();
            var segmentResult = SegmentWord(param);
            result.SegmentWords = segmentResult.Words;
            var repository = new SearchRepository();
            result.ResultSet = repository.QueryAutoComplete(param, result.SegmentWords);
            return result;
        }

        public QueryHotKeywordsResult QueryHotKeywords(QueryHotKeywordsParameter param)
        {
            var result = new QueryHotKeywordsResult();
            var repository = new SearchRepository();
            result.Keywords = repository.QueryHotKeywords(param);
            return result;
        }
    }
}