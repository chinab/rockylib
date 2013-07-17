using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace NoSQL
{
    public class CacheKeyGenerator : ICacheKeyGenerator
    {
        public virtual bool AutoHash { get; set; }

        public void HashKey(ref string longKey)
        {
            if (longKey.Length > 32)
            {
                longKey = CryptoManaged.MD5Hash(longKey);
            }
        }

        public virtual string GenerateKey(object value)
        {
            string key = CryptoManaged.MD5Hash(Serializer.Serialize(value));
            if (this.AutoHash)
            {
                this.HashKey(ref key);
            }
            return key;
        }
    }
}