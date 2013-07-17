using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq;
using System.Caching;

namespace NoSQL
{
    public interface ICacheContext : IDisposable
    {
        DataContext Database { get; }
        DistributedCache Cache { get; }

        ICacheKeyMapper CreateKeyMapper<TEntity>();
        void Initialize<TEntity>(IQueryable<TEntity> query) where TEntity : class;
        IEnumerable<TEntity> ExecuteQuery<TEntity>(IQueryable<TEntity> query) where TEntity : class;
    }
}