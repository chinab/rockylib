using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoSQL;
using Rocky.Caching;
using System.Configuration;
using Rocky.Data;
using System.Threading;

namespace Rocky.UnitTesting.Caching
{
    [TestClass]
    public class NoSQLTest
    {
        public class UserBizObj
        {
            public int UserID { get; set; }
            public string UserFullName { get; set; }
        }
        public class UserBizObj2 : UserBizObj
        {
            public string OrderName { get; set; }
            public DateTime CreateDate { get; set; }
        }

        private const string DataAccessFormat = "NoSQL.Test.{0}, NoSQL.Test";

        /// <summary>
        /// CacheProvider Demo
        /// </summary>
        [TestMethod]
        public void TestProvider()
        {
            var providerName = CacheProviderName.Redis;
            var cache = NoSQLHelper.CreateCache(string.Empty, providerName);
            cache.ConnectTimeout = cache.SendReceiveTimeout = 1000 * 30;
            string key = providerName.ToString();
            cache.Add(key, "Hello World!", DateTimeOffset.Now.AddSeconds(30D).DateTime);
            Assert.AreEqual(cache.Get(key), "Hello World!");

            cache.Set("int", 100, DistributedCache.CreatePolicy());
            Assert.AreEqual(cache.Get("int"), 100);

            cache.Set("entity", new UserTableEntity()
            {
                RowID = 1,
                UserName = "Rocky",
                CreateDate = DateTime.Now
            }, DistributedCache.CreatePolicy());
            var ret = (UserTableEntity)cache.Get("entity");
            Assert.AreEqual(ret.UserName, "Rocky");
        }

        #region SQL Server Change Tracking应用
        public void SqlChangeMonitor()
        {
            var connstr = ConfigurationManager.ConnectionStrings["Rocky.UnitTesting.Properties.Settings.DevDbConnectionString"].ConnectionString;
            var monitor = new SqlRowChangeMonitor(connstr, new Type[] { typeof(UserTable) }, -1L, TimeSpan.FromSeconds(1D));
            monitor.Inserted += new EventHandler<SqlRowChangedEventArgs>(monitor_Inserted);
            monitor.Updated += new EventHandler<SqlRowChangedEventArgs>(monitor_Updated);
            monitor.Deleted += new EventHandler<SqlRowChangedEventArgs>(monitor_Deleted);
            Console.WriteLine("ChangeMonitor Start...");
            Console.Read();
        }
        void monitor_Deleted(object sender, SqlRowChangedEventArgs e)
        {
            var cols = e.GetPrimaryKey().Values;
            Console.WriteLine("Deleted PrimaryKey:" + string.Join(",", cols));
        }
        void monitor_Updated(object sender, SqlRowChangedEventArgs e)
        {
            var entity = e.GetRow<UserTable>();
            Console.WriteLine("Updated:" + entity.UserName);

            var cols = e.GetChangeColumns().Keys.Select(item => item.MappedName);
            Console.WriteLine("Updated ChangeColumns:" + string.Join(",", cols));
        }
        void monitor_Inserted(object sender, SqlRowChangedEventArgs e)
        {
            var entity = e.GetRow<UserTable>();
            Console.WriteLine("Inserted:" + entity.UserName);
        }
        #endregion

        #region SQL Server Change Tracking集成DistributedCache
        public void Linq2CacheWithSqlChangeMonitor()
        {
            // 服务端开启数据监控，为测试方便都写在客户端了
            var connstr = ConfigurationManager.ConnectionStrings["NoSQL.Test.Properties.Settings.DevDbConnectionString"].ConnectionString;
            NoSQLHelper.RegisterChangeMonitor<UserTable>(connstr, TimeSpan.FromSeconds(1D));

            using (var context = new CacheContext<NoSQLDataContext>(DataAccessFormat))
            {
                var query = from row in context.Linq.UserTables
                            where row.RowID == 10
                            select row;
                var entity = context.ExecuteQuery(query).Single();
                Console.WriteLine(string.Format("查询ID为10的用户名为：{0}", entity.UserName));
                // 2秒内修改数据
                Thread.Sleep(2000);

                // 如果修改数据后，会对应修改缓存内容
                var keyMapper = context.CreateKeyMapper<UserTable>();
                var memKey = keyMapper.GenerateKey(entity);
                ICacheContext client = (ICacheContext)context;
                var memEntity = (UserTable)client.Cache.Get(memKey);
                Console.WriteLine(string.Format("缓存ID为10的用户名为：{0}", memEntity.UserName));
            }
        }
        #endregion

        #region Linq to SQL 与 Linq to Cache 性能测试
        /// <summary>
        /// Linq to SQL 与 Linq to Cache 性能测试
        /// </summary>
        //public void Linq2Cache()
        //{
        //    using (var context = new CacheContext<NoSQLDataContext>(DataAccessFormat))
        //    {
        //        context.FlushAll();

        //        var q = (from t in context.Linq.UserTables
        //                 where t.RowID > 1 && t.RowID < 500
        //                 orderby t.RowID descending
        //                 orderby t.UserName ascending
        //                 select t).Skip(1).Take(10);
        //        var q2 = (from t in context.Linq.UserTables
        //                  where t.RowID > 1 && t.RowID < 500
        //                  orderby t.RowID descending
        //                  orderby t.UserName ascending
        //                  select new UserBizObj
        //                  {
        //                      UserID = t.RowID,
        //                      UserFullName = t.UserName + "_" + t.Age
        //                  }).Skip(1).Take(10);

        //        var q3 = (from t in context.Linq.UserTables
        //                  join t2 in context.Linq.UserOrders on t.RowID equals t2.UserID
        //                  where t.RowID > 1 && t.RowID < 500
        //                  orderby t.RowID descending
        //                  orderby t.UserName ascending
        //                  select t).Skip(1).Take(10);
        //        var q4 = (from t in context.Linq.UserTables
        //                  join t2 in context.Linq.UserOrders on t.RowID equals t2.UserID
        //                  where t.RowID > 1 && t.RowID < 500
        //                  orderby t.RowID descending
        //                  orderby t.UserName ascending
        //                  select new UserBizObj2
        //                  {
        //                      UserID = t.RowID,
        //                      UserFullName = t.UserName + "_" + t.Age,
        //                      OrderName = t2.OrderName,
        //                      CreateDate = t2.CreateDate
        //                  }).Skip(1).Take(10);

        //        var execQuery = q2;
        //        CodeTimer.VistaAbove_Time("执行 DbQuery 1000次", 1000, () =>
        //        {
        //            var result = execQuery.ToList();
        //        });
        //        CodeTimer.VistaAbove_Time("执行 Linq2Cache 1000次", 1000, () =>
        //        {
        //            var result = context.ExecuteQuery(execQuery);
        //        });
        //    }
        //}
        #endregion
    }
}