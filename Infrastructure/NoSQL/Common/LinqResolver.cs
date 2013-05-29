using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Data.Linq;
using Rocky.Data;

namespace NoSQL
{
    internal class LinqResolver : System.Linq.Expressions.ExpressionVisitor
    {
        private Expression _updatedExp;
        private List<MetaTable> _queriedModels;
        private ReadOnlyCollection<MetaTable> _readOnlyQueriedModels;
        private LambdaExpression _selectExp;
        private Delegate _selectMethod;

        public Expression UpdatedExpression
        {
            get { return _updatedExp; }
        }
        public ReadOnlyCollection<MetaTable> QueriedModels
        {
            get
            {
                if (_readOnlyQueriedModels == null)
                {
                    _readOnlyQueriedModels = _queriedModels.AsReadOnly();
                }
                return _readOnlyQueriedModels;
            }
        }

        public LinqResolver(IQueryable query)
        {
            _queriedModels = new List<MetaTable>(1);
            _updatedExp = base.Visit(query.Expression);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            var expTable = node.Value as ITable;
            MetaTable model;
            if (expTable != null && !_queriedModels.Contains(model = MetaTable.GetTable(expTable.ElementType)))
            {
                _queriedModels.Add(model);
            }
            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                //case "Where":
                //    {
                //        ConstantExpression constExp = (ConstantExpression)node.Arguments[0];
                //        var expTable = (ITable)constExp.Value;
                //        var table = _queriedModels.SingleOrDefault(item => item.EntityType == expTable.ElementType);
                //        if (table == null)
                //        {
                //            break;
                //        }
                //        UnaryExpression unaryExp = (UnaryExpression)node.Arguments[1];
                //        LambdaExpression lambdaExp = (LambdaExpression)unaryExp.Operand;
                //        BinaryExpression binaryExp = (BinaryExpression)lambdaExp.Body;
                //        MemberExpression left = (MemberExpression)binaryExp.Left;
                //        ICacheIndex index;
                //        if (table.Indices.TryGetIndex((System.Reflection.PropertyInfo)left.Member, out index))
                //        {
                //            this.ResolvedIndices.Add(new ResolvedIndex()
                //            {
                //                Expression = binaryExp,
                //                Table = table,
                //                Index = index
                //            });
                //        }
                //    }
                //    break;
                case "Select":
                    {
                        UnaryExpression unaryExp = (UnaryExpression)node.Arguments.Last();
                        LambdaExpression lambdaExp = (LambdaExpression)unaryExp.Operand;
                        _selectExp = lambdaExp;

                        //MemberInitExpression bodyExp = (MemberInitExpression)lambdaExp.Body;
                        //MemberAssignment assign = (MemberAssignment)bodyExp.Bindings[0];
                    }
                    break;
            }
            return base.VisitMethodCall(node);
        }

        public object SelectAssign(params object[] args)
        {
            if (_selectMethod == null)
            {
                if (_selectExp == null)
                {
                    throw new InvalidOperationException("SelectLambdaExpression");
                }
                _selectMethod = _selectExp.Compile();
            }

            Type argType = _selectExp.Parameters[0].Type;
            if (argType.IsNotPublic)
            {
                var arg = Activator.CreateInstance(argType, args);
                return _selectMethod.DynamicInvoke(arg);
            }
            return _selectMethod.DynamicInvoke(args);
        }
    }
}