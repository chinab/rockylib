using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using InfrastructureService.Model.Basic;

namespace InfrastructureService.Contract
{
    [ServiceContract]
    public interface ISearchService
    {
        [OperationContract]
        SegmentWordResult SegmentWord(SegmentWordParameter param);

        [OperationContract]
        QueryAutoCompleteResult AutoComplete(QueryAutoCompleteParameter param);

        [OperationContract]
        QueryHotKeywordsResult QueryHotKeywords(QueryHotKeywordsParameter param);
    }
}