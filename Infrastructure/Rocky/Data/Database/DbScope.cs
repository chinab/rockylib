using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Data.Common;
using System.Collections;
using System.Collections.Specialized;

namespace Rocky.Data
{
    public sealed class DbScope : Disposable
    {
        #region StaticMembers
        [ThreadStatic]
        private static DbScope _current;

        public static DbScope Current
        {
            get { return _current; }
        }

        public static DbScope Create()
        {
            if (_current == null)
            {
                return new DbScope();
            }
            else
            {
                _current._nestingLevel++;
                return _current;
            }
        }
        #endregion

        #region Fields
        private int _nestingLevel;
        private IDictionary _contextData;
        private TransactionScope _transactionScope;
        #endregion

        #region Properties
        public string Message
        {
            get { return (string)_contextData["_Message"]; }
            set { _contextData["_Message"] = value; }
        }
        public int NestingLevel
        {
            get { return _nestingLevel; }
        }
        public IDictionary ContextData
        {
            get { return _contextData; }
        }
        public bool BeganTransaction
        {
            get { return _transactionScope != null; }
        }
        #endregion

        #region Constructors
        private DbScope()
        {
            //!Important
            _current = this;
            _contextData = new ListDictionary();
        }

        protected override void DisposeInternal(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    foreach (DictionaryEntry entry in _contextData)
                    {
                        if (entry.Key is DbFactory)
                        {
                            var cmd = entry.Value as DbCommand;
                            if (cmd != null)
                            {
                                cmd.Connection.Close();
                                cmd.Dispose();
                            }
                        }
                    }
                    if (_transactionScope != null)
                    {
                        _transactionScope.Dispose();
                    }
                }
                // Indicate that the instance has been disposed.
                _contextData = null;
            }
            finally
            {
                //!Important
                _current = null;
            }
        }

        public new void Dispose()
        {
            if (_nestingLevel == 0)
            {
                base.Dispose();
            }
            else
            {
                _nestingLevel--;
            }
        }
        #endregion

        #region Methods
        public DbCommand PrepareCommand(IRequiresFactory model)
        {
            base.CheckDisposed();
            var cmd = (DbCommand)_contextData[model.Factory];
            if (cmd == null)
            {
                cmd = model.Factory.CreateConnection().CreateCommand();
                _contextData.Add(model.Factory, cmd);
                cmd.Connection.Open();
            }
            cmd.Parameters.Clear();
            return cmd;
        }

        public void BeginTransaction()
        {
            base.CheckDisposed();
            if (_transactionScope == null)
            {
                _transactionScope = new TransactionScope(TransactionScopeOption.Required);
            }
        }
        public void BeginTransaction(IsolationLevel level, TimeSpan timeout)
        {
            base.CheckDisposed();
            if (_transactionScope == null)
            {
                _transactionScope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions()
                {
                    IsolationLevel = level,
                    Timeout = timeout
                });
            }
        }

        public void Complete()
        {
            base.CheckDisposed();
            if (_nestingLevel == 0 && _transactionScope != null)
            {
                _transactionScope.Complete();
                _transactionScope.Dispose();
                _transactionScope = null;
            }
        }
        #endregion
    }
}