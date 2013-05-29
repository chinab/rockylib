using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model
{
    [DataContract]
    public abstract class PagingParameter : HeaderEntity
    {
        /// <summary>
        /// PageIndex>0，则分页
        /// </summary>
        [DataMember]
        public virtual int PageIndex { get; set; }
        /// <summary>
        /// PageIndex>0，则分页；否则取前PageSize条记录
        /// </summary>
        [DataMember]
        public virtual int PageSize { get; set; }
        /// <summary>
        /// SkipStatus = true，则获取全部记录(包括已经删除的数据)；否则默认获取未被删除的数据
        /// </summary>
        [DataMember]
        public virtual bool SkipStatus { get; set; }
        /// <summary>
        /// OnlyCount = true，则只返回结果集总数，不返回结果集
        /// </summary>
        [DataMember]
        public virtual bool OnlyCount { get; set; }
    }
}