using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using EntityFramework.Extensions;
using InfrastructureService.Model;
using InfrastructureService.Model.Site;
using InfrastructureService.Repository.DataAccess;
using System.Data.Entity.Core.Objects;
using System.Net.WCF;

namespace InfrastructureService.Repository.Site
{
    public class AdminRepository : RepositoryBase
    {
        #region User
        internal string HexPassword(string password)
        {
            return CryptoManaged.MD5Hex(password);
        }

        public void SaveAdmin(AdminEntity param)
        {
            param.Password = HexPassword(param.Password);
            using (var context = base.CreateContext())
            {
                UserInfo pObj;
                if (param.RowID == Guid.Empty)
                {
                    context.UserInfoes.Add(pObj = new UserInfo());
                    EntityMapper.Map<AdminEntity, UserInfo>(param, pObj);
                    pObj.RowID = Guid.NewGuid();
                    pObj.LastSignInDate = DateTime.Now;
                    pObj.LastSignInIP = "127.0.0.1";
                }
                else
                {
                    pObj = context.UserInfoes.Single(t => t.RowID == param.RowID);
                    EntityMapper.Map<AdminEntity, UserInfo>(param, pObj);
                }
                context.SaveChanges();
                string roleIDSet = string.Join(",", param.RoleIDs);
                context.msp_Assign_User_Role(pObj.RowID, roleIDSet);
            }
        }

        public void SetAdminStatus(SetStatusParameter param)
        {
            using (var context = base.CreateContext())
            {
                int status = EnumToValue(param.Status);
                context.UserInfoes.Update(t => param.RowIDSet.Contains(t.RowID), t => new UserInfo() { Status = status });
            }
        }

        public SignInResult SignIn(SignInParameter param)
        {
            var result = new SignInResult();
            param.Password = HexPassword(param.Password);
            using (var context = base.CreateContext())
            {
                int status = EnumToValue(StatusKind.Blocked);
                var q = from t in context.UserInfoes
                        where t.Status != status
                        && t.UserName == param.UserName && t.Password == param.Password
                        select t;
                var entity = q.SingleOrDefault();
                if (entity != null)
                {
                    result.User = new AdminEntity();
                    EntityMapper.Map(entity, result.User);
                    entity.LastSignInDate = DateTime.Now;
                    entity.LastSignInIP = param.LastLoginIP;
                    context.SaveChanges();
                }
            }
            result.Success = result.User != null;
            return result;
        }

        public void ChangePassword(ChangePasswordParameter param)
        {
            param.OldPassword = HexPassword(param.OldPassword);
            param.NewPassword = HexPassword(param.NewPassword);
            using (var context = base.CreateContext())
            {
                var entrty = context.UserInfoes.Where(m => m.UserName == param.UserName && m.Password == param.OldPassword).FirstOrDefault();
                if (entrty == null)
                {
                    throw new InvalidInvokeException("原始密码错误");
                }

                entrty.Password = param.NewPassword;
                context.SaveChanges();
            }
        }

        public QueryAdminsResult QueryAdmins(QueryAdminsParameter param)
        {
            using (var context = base.CreateContext())
            {
                var result = new QueryAdminsResult();
                int status = EnumToValue(StatusKind.Blocked);
                var q = from t in context.UserInfoes
                        where t.AppID == param.AppID
                        && (param.SkipStatus || t.Status != status)
                        && (param.RowID == null || t.RowID == param.RowID)
                        && (param.UserName == null || t.UserName == param.UserName)
                        orderby t.LastSignInDate descending
                        select new AdminEntity
                        {
                            AppID = t.AppID,
                            RowID = t.RowID,
                            UserName = t.UserName,
                            Password = t.Password,
                            Status = (StatusKind)t.Status,
                            LastSignInDate = t.LastSignInDate,
                            LastSignInIP = t.LastSignInIP,
                            Email = t.Email,
                            Mobile = t.Mobile,
                            Remark = t.Remark,
                            RoleIDs = t.RoleInfoes.Select(t2 => t2.RowID).ToArray()
                        };
                result.PageResult(q, param);
                return result;
            }
        }

        public PermissionFlags GetUserPermission(Guid userID, string pathKey)
        {
            using (var context = base.CreateContext())
            {
                Guid? pUserID = userID;
                var permissionFlags = new ObjectParameter("permissionFlags", null);
                context.msp_GetUserPermission(pUserID, pathKey, permissionFlags);
                return (PermissionFlags)permissionFlags.Value;
            }
        }

        public TermControlResult[] QueryTermControls(Guid userID)
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.UserControlMaps
                        where t.UserID == userID
                        select new TermControlResult
                        {
                            ControlID = t.ControlID,
                            ControlName = t.ControlInfo.Name,
                            Permission = (PermissionFlags)t.PermissionFlags,
                            BeginDate = t.BeginDate,
                            EndDate = t.EndDate
                        };
                return q.ToArray();
            }
        }

        //public List<msp_GetUserComponentResult> GetUserComponent(Guid userID)
        //{
        //    return _context.msp_GetUserComponent(userID).ToList();
        //}
        //public List<msp_GetUserControlResult> GetUserControl(Guid userID, Guid componentID)
        //{
        //    return _context.msp_GetUserControl(userID, componentID).ToList();
        //}
        #endregion

        #region Role
        public bool Create(CreateRoleParameter param)
        {
            using (var context = base.CreateContext())
            {
                return context.msp_InsertRole(param.Name, param.Description, string.Join(",", param.ControlIDs), string.Join(",", param.Permissions.Select(t => (int)t))) == 0;
            }
        }

        public bool BlockRole(Guid roleID)
        {
            using (var context = base.CreateContext())
            {
                return context.msp_BlockRole(roleID) == 0;
            }
        }

        public QueryRoleResult QueryRole(Guid? roleID)
        {
            var ret = new QueryRoleResult();
            using (var context = base.CreateContext())
            {
                //using (var result = context.msp_QueryRoleDetailMultiResult(roleID))
                //{
                //    ret.Role = MapperManager.GetMapper<msp_QueryRoleDetailResult, RoleInfoEntity>()
                //        .Map(result.GetResult<msp_QueryRoleDetailResult>().SingleOrDefault());

                //    var one = MapperManager.GetMapper<QueryRoleDetail_ComponentResult, ComponentResult>()
                //        .MapEnum(result.GetResult<QueryRoleDetail_ComponentResult>()).ToList();
                //    var many = new List<List<ControlResult>>();
                //    var mapper = MapperManager.GetMapper<QueryRoleDetail_ControlResult, ControlResult>();
                //    for (int i = 0; i < one.Count; i++)
                //    {
                //        many.Add(mapper.MapEnum(result.GetResult<QueryRoleDetail_ControlResult>()).ToList());
                //    }
                //    ret.ComponentSet = new OneToManyResult<ComponentResult, ControlResult>(one, many);
                //}
            }
            return ret;
        }

        public RoleEntity[] QueryRoles()
        {
            using (var context = base.CreateContext())
            {
                var q = from t in context.RoleInfoes
                        select new RoleEntity
                        {
                            RowID = t.RowID,
                            Name = t.Name,
                            Description = t.Description,
                            AssignUserCount = t.UserInfoes.Count
                        };
                return q.ToArray();
            }
        }
        #endregion

        #region ServiceInfos
        //public void Save(ComponentInfoEntity entity)
        //{
        //    if (entity.RowID == Guid.Empty)
        //    {
        //        entity.RowID = Guid.NewGuid();
        //        base.Create(entity);
        //    }
        //    else
        //    {
        //        base.Alter(entity);
        //    }
        //    base.Submit();
        //}

        //public void Save(ControlInfoEntity entity)
        //{
        //    if (entity.RowID == Guid.Empty)
        //    {
        //        entity.RowID = Guid.NewGuid();
        //        base.Create(entity);
        //    }
        //    else
        //    {
        //        base.Alter(entity);
        //    }
        //    base.Submit();
        //}

        //public void Save(Auth_User_ControlEntity entity)
        //{
        //    bool isEdit = _context.Auth_User_Controls.Any(t => t.UserID == entity.UserID && t.ControlID == entity.ControlID);
        //    if (isEdit)
        //    {
        //        base.Alter(entity);
        //    }
        //    else
        //    {
        //        base.Create(entity);
        //    }
        //    base.Submit();
        //}

        //public IQueryable<ServiceInfoEntity> QueryServiceInfos()
        //{
        //    var q = from t in base.Queryable<ServiceInfoEntity>()
        //            orderby t.Sort ascending
        //            select t;
        //    return q;
        //}

        //public IQueryable<ComponentInfoEntity> QueryComponentInfos(QueryComponentInfosParameter param)
        //{
        //    var q = from t in base.Queryable<ComponentInfoEntity>()
        //            join t2 in _context.ServiceInfos on t.ServiceID equals t2.RowID
        //            where t2.Status >= 0
        //            && (param.IncludeBlock ? true : t.Status >= 0)
        //            && (param.ServiceID == null ? true : t.ServiceID == param.ServiceID)
        //            orderby t2.Sort, t.Sort ascending
        //            select t;
        //    return q;
        //}

        //public IQueryable<ControlInfoEntity> QueryControlInfos(QueryControlInfosParameter param)
        //{
        //    var q = from t in base.Queryable<ControlInfoEntity>()
        //            join t2 in _context.ComponentInfos on t.ComponentID equals t2.RowID
        //            where t2.Status >= 0
        //            && (param.IncludeBlock ? true : t.Status >= 0)
        //            && (param.ComponentID == null ? true : t.ComponentID == param.ComponentID)
        //            orderby t2.Sort, t.Sort ascending
        //            select t;
        //    return q;
        //}
        #endregion
    }
}