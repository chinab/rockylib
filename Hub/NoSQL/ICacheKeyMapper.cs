using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace NoSQL
{
    public interface ICacheKeyMapper : ICacheKeyGenerator
    {
        string GenerateKey(Array value);
        //Array ResolveKey(string key, out MetaTable model);
    }
}