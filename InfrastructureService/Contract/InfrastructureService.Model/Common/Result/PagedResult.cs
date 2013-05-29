using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace InfrastructureService.Model
{
    [DataContract]
    public abstract class PagedResult<T> : ExecResult<T> where T : class
    {
        /// <summary>
        /// 结果集总数
        /// </summary>
        [DataMember]
        public virtual int RecordCount { get; set; }

        public virtual void PageResult(IQueryable<T> query, PagingParameter pager)
        {
            if (pager.PageIndex > 0)
            {
                this.RecordCount = query.Count();
                if (!pager.OnlyCount)
                {
                    query = query.Skip((pager.PageIndex - 1) * pager.PageSize).Take(pager.PageSize);
                    this.ResultSet = query.ToArray();
                }
            }
            else
            {
                if (pager.PageSize > 0)
                {
                    query = query.Take(pager.PageSize);
                }
                if (pager.OnlyCount)
                {
                    this.RecordCount = query.Count();
                }
                else
                {
                    this.ResultSet = query.ToArray();
                    this.RecordCount = this.ResultSet.Length;
                }
            }
        }
    }

    [DataContract]
    public abstract class ExecResult<T> where T : class
    {
        [DataMember]
        public virtual T[] ResultSet { get; set; }
    }
}