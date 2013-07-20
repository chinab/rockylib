using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;

namespace System.Data
{
    /// <summary>
    /// MultipleActiveResultSets=True;
    /// </summary>
    [Serializable]
    public sealed class ConnectionStringBuilder : DbConnectionStringBuilder
    {
        #region Properties
        public bool IsEncrypt
        {
            get { return base.ContainsKey("KEY"); }
        }
        public string[] CryptoKeys
        {
            set
            {
                if (value.Length < 2)
                {
                    throw new ArgumentException("value.Length < 2");
                }
                base["KEY"] = string.Join(DbUtility.Separator, value);
            }
            get
            {
                object value;
                if (base.TryGetValue("KEY", out value))
                {
                    string[] args = value.ToString().Split(DbUtility.Separator[0]);
                    if (args.Length >= 2)
                    {
                        return args;
                    }
                }
                return new string[2];
            }
        }
        #endregion

        #region Methods
        public ConnectionStringBuilder()
        {

        }
        public ConnectionStringBuilder(string connectionString)
        {
            base.ConnectionString = connectionString;
        }

        internal string GetDecrypt()
        {
            if (this.IsEncrypt)
            {
                string[] args = this.CryptoKeys;
                using (CryptoManaged coder = new CryptoManaged(args[0], args[1]))
                {
                    return coder.Decrypt(base["ENTITY"].ToString());
                }
            }
            return base.ConnectionString;
        }
        public override string ToString()
        {
            if (this.IsEncrypt)
            {
                string[] args = this.CryptoKeys;
                using (CryptoManaged coder = new CryptoManaged(args[0], args[1]))
                {
                    base.Remove("KEY");
                    string oldString = base.ConnectionString;
                    base.Clear();
                    base["ENTITY"] = coder.Encrypt(oldString);
                    this.CryptoKeys = args;
                    string newString = base.ToString().Replace("\"", string.Empty);
                    base.ConnectionString = oldString;
                    this.CryptoKeys = args;
                    return newString;
                }
            }
            return base.ToString();
        }
        #endregion
    }
}