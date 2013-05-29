using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using InfrastructureService.Model.Site;

namespace InfrastructureService.Contract
{
    public partial interface ISiteService
    {
        [OperationContract]
        void SaveCategory(SaveCategoryParameter param);
        [OperationContract]
        void DeleteCategories(DeleteCategoriesParameter param);
        [OperationContract]
        QueryCategoriesResult QueryCategories(QueryCategoriesParameter param);

        [OperationContract]
        void SaveNews(SaveNewsParameter param);
        [OperationContract]
        void SetNewsStatus(SetNewsStatusParameter param);
        [OperationContract]
        QueryNewsDetailResult QueryNewsDetail(QueryNewsDetailParameter param);
        [OperationContract]
        QueryNewsResult QueryNews(QueryNewsParameter pager);
        [OperationContract]
        string[] QueryTags(QueryTagsParameter param);
    }
}