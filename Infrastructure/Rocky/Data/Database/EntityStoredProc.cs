using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace Rocky.Data
{
    /// <summary>
    /// 作   者：wangxiaoming
    /// 时   间：2011-1-5 14:42:00
    /// 公   司：上海盛大网络发展有限公司
    /// 说   明：本代码版权归上海盛大网络发展有限公司所有
    /// 标   识：54e2c17a-e412-4144-a243-82f88aa33da3
    /// CLR版本：2.0.50727.3615
    /// </summary>
    public class EntityStoredProc : Database
    {
        #region Methods
        public EntityStoredProc(DbFactory factory)
            : base(factory, 32)
        {

        }

        private void DeriveAssignParametersByEntity(DbCommand cmd, object entity)
        {
            DbParameter[] discoveredParameters = base.GetDeriveParameters(cmd);
            if (cmd.Parameters.Count == 0)
            {
                cmd.Parameters.Add(((ICloneable)discoveredParameters[0]).Clone());
            }
            string spName = cmd.CommandText;
            IEnumerable<PropertyAccess> enumerable = EntityUtility.GetEnumerable(entity.GetType());
            for (int i = 1; i < discoveredParameters.Length; i++)
            {
                DbParameter discoveredParameter = discoveredParameters[i];
                foreach (PropertyAccess property in enumerable)
                {
                    var attr = (System.Data.Linq.Mapping.ColumnAttribute)Attribute.GetCustomAttribute(property.EntityProperty, typeof(System.Data.Linq.Mapping.ColumnAttribute));
                    string name;
                    if (attr == null)
                    {
                        name = base.Factory.ParameterNamePrefix + property.MappedName;
                    }
                    else
                    {
                        name = base.Factory.ParameterNamePrefix + (string.IsNullOrEmpty(attr.Name) ? property.MappedName : attr.Name);
                    }
                    if (name == discoveredParameter.ParameterName)
                    {
                        object value = property.GetValue(entity) ?? DBNull.Value;
                        int index = cmd.Parameters.IndexOf(discoveredParameter.ParameterName);
                        if (index == -1)
                        {
                            object cloned = ((ICloneable)discoveredParameter).Clone();
                            ((DbParameter)cloned).Value = value;
                            cmd.Parameters.Add(cloned);
                        }
                        else
                        {
                            cmd.Parameters[index].Value = value;
                        }
                        break;
                    }
                }
            }
        }

        private List<PropertyConverter> GetProperties(string spName, Type entityType)
        {
            var list = new List<PropertyConverter>();
            foreach (PropertyInfo property in entityType.GetProperties(PropertyAccess.PropertyBinding))
            {
                // public int MyProperty { get; private set; }
                if (!property.CanWrite || property.GetSetMethod() == null)
                {
                    continue;
                }
                var attr = (System.Data.Linq.Mapping.ColumnAttribute)Attribute.GetCustomAttribute(property, typeof(System.Data.Linq.Mapping.ColumnAttribute));
                if (attr != null)
                {
                    list.Add(new PropertyConverter(string.IsNullOrEmpty(attr.Name) ? property.Name : attr.Name, attr.CanBeNull, property));
                }
                else
                {
                    list.Add(new PropertyConverter(property));
                }
            }
            return list;
        }

        internal Converter<DbDataReader, T> GetConverter<T>(string spName, DbDataReader schema, int resultDepth)
        {
            Type entityType = typeof(T);
            string key = String.Concat(spName, entityType.Name, schema.FieldCount.ToString(), resultDepth.ToString());
            var converter = (Converter<DbDataReader, T>)Cache[key];
            if (converter == null)
            {
                TypeCode code = Type.GetTypeCode(entityType);
                if (code == TypeCode.Object && entityType != typeof(Guid))
                {
                    var convertibles = GetProperties(spName, entityType);
                    if (schema.FieldCount <= convertibles.Count)
                    {
                        converter = EntityConverter<T>.CreateReadSingle(convertibles.ToArray());
                    }
                }
                if (converter == null)
                {
                    converter = EntityUtility.CreateConverter<T>();
                }
                Cache[key] = converter;
            }
            return converter;
        }
        #endregion

        #region ExecuteResult
        public ExecuteResult Execute(string spName)
        {
            DbCommand cmd = base.PrepareCommand(spName, CommandType.StoredProcedure);
            return new ExecuteResult(this, cmd, ExecuteNonQuery(cmd));
        }
        public ExecuteResult Execute(string spName, object[] values)
        {
            DbCommand cmd = base.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParameters(cmd, values);
            return new ExecuteResult(this, cmd, ExecuteNonQuery(cmd));
        }
        public ExecuteResult ExecuteByEntity(string spName, object entity)
        {
            DbCommand cmd = base.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParametersByEntity(cmd, entity);
            return new ExecuteResult(this, cmd, ExecuteNonQuery(cmd));
        }

        public MultipleResults Query(string spName)
        {
            DbCommand cmd = base.PrepareCommand(spName, CommandType.StoredProcedure);
            return new MultipleResults(this, cmd, ExecuteReader(cmd));
        }
        public MultipleResults Query(string spName, object[] values)
        {
            DbCommand cmd = base.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParameters(cmd, values);
            return new MultipleResults(this, cmd, ExecuteReader(cmd));
        }
        public MultipleResults QueryByEntity(string spName, object entity)
        {
            DbCommand cmd = base.PrepareCommand(spName, CommandType.StoredProcedure);
            DeriveAssignParametersByEntity(cmd, entity);
            return new MultipleResults(this, cmd, ExecuteReader(cmd));
        }
        #endregion
    }

    #region Result
    public sealed class MultipleResults : BaseResult, System.Data.Linq.IMultipleResults
    {
        private DbDataReader dr;
        private int resultDepth;

        public override int RecordsAffected
        {
            get { return dr.RecordsAffected; }
        }
        public override int ReturnValue
        {
            get
            {
                CheckClose();
                return base.ReturnValue;
            }
        }

        internal MultipleResults(EntityStoredProc owner, DbCommand cmd, DbDataReader dr)
            : base(owner, cmd)
        {
            this.dr = dr;
        }
        private void CheckClose()
        {
            if (!dr.IsClosed)
            {
                dr.Close();
            }
        }

        public IEnumerable<TElement> GetResult<TElement>()
        {
            if (resultDepth > 0 && !dr.NextResult())
            {
                yield break;
            }

            var convert = owner.GetConverter<TElement>(cmd.CommandText, dr, resultDepth);
            while (dr.Read())
            {
                yield return convert(dr);
            }
            resultDepth++;
        }

        public OneToManyResult<T> GetOneToManyResult<T>()
        {
            var one = GetResult<T>().ToList();
            var many = new List<List<T>>(one.Count);
            for (int i = 0; i < one.Count; i++)
            {
                many.Add(GetResult<T>().ToList());
            }
            return new OneToManyResult<T>(one, many);
        }
        public OneToManyResult<TOne, TMany> GetOneToManyResult<TOne, TMany>()
        {
            var one = GetResult<TOne>().ToList();
            var many = new List<List<TMany>>(one.Count);
            for (int i = 0; i < one.Count; i++)
            {
                many.Add(GetResult<TMany>().ToList());
            }
            return new OneToManyResult<TOne, TMany>(one, many);
        }

        public override T GetParameterValue<T>(int index)
        {
            CheckClose();
            return base.GetParameterValue<T>(index);
        }
        public override T GetParameterValue<T>(string name)
        {
            CheckClose();
            return base.GetParameterValue<T>(name);
        }

        public void Dispose()
        {
            dr.Dispose();
            cmd.Dispose();
        }

        object System.Data.Linq.IFunctionResult.ReturnValue
        {
            get { return this.ReturnValue; }
        }
    }
    public class OneToManyResult<T>
    {
        public List<T> One { private set; get; }
        public List<List<T>> Many { private set; get; }

        public OneToManyResult(List<T> one, List<List<T>> many)
        {
            this.One = one;
            this.Many = many;
        }
    }
    public class OneToManyResult<TOne, TMany>
    {
        public List<TOne> One { private set; get; }
        public List<List<TMany>> Many { private set; get; }

        public OneToManyResult(List<TOne> one, List<List<TMany>> many)
        {
            this.One = one;
            this.Many = many;
        }
    }

    public sealed class ExecuteResult : BaseResult, System.Data.Linq.IExecuteResult
    {
        private int recordsAffected;

        public override int RecordsAffected
        {
            get { return recordsAffected; }
        }

        internal ExecuteResult(EntityStoredProc owner, DbCommand cmd, int recordsAffected)
            : base(owner, cmd)
        {
            this.recordsAffected = recordsAffected;
        }

        public void Dispose()
        {
            cmd.Dispose();
        }

        object System.Data.Linq.IExecuteResult.GetParameterValue(int parameterIndex)
        {
            return base.GetParameterValue<object>(parameterIndex);
        }

        object System.Data.Linq.IExecuteResult.ReturnValue
        {
            get { return this.ReturnValue; }
        }
    }

    public abstract class BaseResult
    {
        protected EntityStoredProc owner;
        protected DbCommand cmd;

        public abstract int RecordsAffected { get; }
        public virtual int ReturnValue
        {
            get { return Convert.ToInt32(owner.GetParameterReturnValue(cmd)); }
        }

        protected internal BaseResult(EntityStoredProc owner, DbCommand cmd)
        {
            this.owner = owner;
            this.cmd = cmd;
        }

        public virtual T GetParameterValue<T>(int index)
        {
            object value = owner.GetParameterValue(cmd, index + 1);
            return DbUtility.IsNullOrDBNull(value) ? default(T) : (T)value;
        }
        public virtual T GetParameterValue<T>(string name)
        {
            object value = owner.GetParameterValue(cmd, name);
            return DbUtility.IsNullOrDBNull(value) ? default(T) : (T)value;
        }
    }
    #endregion
}