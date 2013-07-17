using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using InfrastructureService.Common;
using InfrastructureService.Model.Basic;
using InfrastructureService.Repository.DataAccess;

namespace InfrastructureService.Repository.Basic
{
    public class StorageRepository : RepositoryBase
    {
        #region Methods
        public static string RootPath
        {
            get { return Hub.CombinePath(@"Storage\"); }
        }
        public static System.Net.IPAddress LocalIP
        {
            get
            {
                return SocketHelper.GetHostAddresses().First(); //局域网内IP4地址
            }
        }

        public bool ExistFile(QueryFileParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.FileStorages
                        where t.FileKey == param.FileKey
                        select t;
                return q.Any();
            }
        }

        private string GetVirtualPath(string physicalPath)
        {
            return physicalPath.Substring(RootPath.Length - 1).Replace(@"\", @"/");
        }

        public void SaveFile(string checksum, string fileName, string physicalPath)
        {
            using (var context = base.CreateContext())
            {
                var pObj = context.FileStorages.Where(t => t.FileKey == checksum).SingleOrDefault();
                if (pObj == null)
                {
                    pObj = new FileStorage();
                    pObj.CreateDate = DateTime.Now;
                    context.FileStorages.Add(pObj);
                }
                pObj.PhysicalPath = physicalPath;
                pObj.VirtualPath = this.GetVirtualPath(physicalPath);
                pObj.ServerAuthority = LocalIP.ToString();
                context.SaveChanges();
            }
        }

        public QueryFileResult QueryFile(QueryFileParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.FileStorages
                        where t.FileKey == param.FileKey
                        select new QueryFileResult
                        {
                            FileKey = t.FileKey,
                            FileName = t.FileName,
                            CreateDate = t.CreateDate
                        };
                return q.Single();
            }
        }

        public QueryFilePathResult QueryFilePath(QueryFileParameter param)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.FileStorages
                        where t.FileKey == param.FileKey
                        select new QueryFilePathResult
                        {
                            VirtualPath = t.VirtualPath,
                            PhysicalPath = t.PhysicalPath,
                            ServerAuthority = t.ServerAuthority
                        };
                return q.Single();
            }
        }
        #endregion
    }
}