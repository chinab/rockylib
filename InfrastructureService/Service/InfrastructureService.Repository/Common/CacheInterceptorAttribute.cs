using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using PostSharp.Aspects;

namespace InfrastructureService.Repository
{
    [Serializable]
    public class CacheInterceptorAttribute : OnMethodBoundaryAspect
    {
        private static ObjectCache _cache;

        public static ObjectCache Cache
        {
            get { return _cache ?? MemoryCache.Default; }
            set { Interlocked.Exchange(ref _cache, value); }
        }

        public static void ClearCache(MethodBase method)
        {
            string key = method.DeclaringType.Name + "." + method.Name;
            foreach (KeyValuePair<string, object> pair in Cache)
            {
                if (pair.Key.StartsWith(key))
                {
                    Cache.Remove(pair.Key);
                }
            }
        }

        private double _minuteValue;
        private bool _isSlidingExpiration;

        public CacheInterceptorAttribute(double minuteValue = 10D, bool isSlidingExpiration = false)
        {
            _minuteValue = minuteValue;
            _isSlidingExpiration = isSlidingExpiration;
        }

        public override void OnEntry(MethodExecutionArgs args)
        {
            string hashKey = args.Arguments.Count > 0 ? CryptoManaged.MD5Hex(JsonConvert.SerializeObject(args.Arguments, Formatting.None)) : string.Empty;
            string key = string.Format("{0}.{1}{2}", args.Method.DeclaringType.Name, args.Method.Name, hashKey);
            object result = Cache[key];
            if (result != null)
            {
                args.FlowBehavior = FlowBehavior.Return;
                args.ReturnValue = result;
            }
            else
            {
                args.MethodExecutionTag = key;
            }
            base.OnEntry(args);
        }

        public override void OnExit(MethodExecutionArgs args)
        {
            string key = args.MethodExecutionTag.ToString();
            if (_isSlidingExpiration)
            {
                Cache.AddOrGetExisting(key, args.ReturnValue, new CacheItemPolicy()
                {
                    SlidingExpiration = TimeSpan.FromMinutes(_minuteValue)
                });
            }
            else
            {
                Cache.AddOrGetExisting(key, args.ReturnValue, new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(_minuteValue)
                });
            }
            base.OnExit(args);
        }
    }
}