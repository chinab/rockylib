using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfrastructureService.Common;
using InfrastructureService.Model;
using InfrastructureService.Repository.DataAccess;

namespace InfrastructureService.Repository
{
    public abstract class RepositoryBase
    {
        #region Static
        static RepositoryBase()
        {
            var _hack = typeof(System.Data.Entity.SqlServer.SqlProviderServices);
        }
        internal static int EnumToValue<T>(T value) where T : struct
        {
            return Convert.ToInt32(value);
        }
        internal static int? EnumToValue<T>(T? value) where T : struct
        {
            return value.HasValue ? Convert.ToInt32(value.Value) : (int?)null;
        }
        #endregion

        #region Methods
        protected InfrastructureServiceEntities CreateContext()
        {
            return new InfrastructureServiceEntities();
        }
        protected InfrastructureService_UserEntities CreateUserContext()
        {
            return new InfrastructureService_UserEntities();
        }

        internal void VerifyHeader(HeaderEntity header)
        {
            using (var context = this.CreateContext())
            {
                int status = EnumToValue(StatusKind.Blocked);
                if (!context.AppInfoes.Any(t => t.AppID == header.AppID && t.Status != status))
                {
                    throw new DomainException("The App was blocked.");
                }
            }
        }

        public string QueryAppName(HeaderEntity header)
        {
            using (var context = this.CreateContext())
            {
                var q = from t in context.AppInfoes
                        where t.AppID == header.AppID
                        select t.AppName;
                return q.Single();
            }
        }
        #endregion
    }
}