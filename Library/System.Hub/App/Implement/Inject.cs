using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    partial class App
    {
        private class InjectItem
        {
            private ConcurrentDictionary<Guid, Assembly> _mapper;

            public AppDomain Domain { get; private set; }
            public ConcurrentDictionary<Guid, Assembly> Mapper
            {
                get { return _mapper; }
            }

            public InjectItem(AppDomain domain)
            {
                this.Domain = domain;
                _mapper = new ConcurrentDictionary<Guid, Assembly>();
            }
        }

        private static SynchronizedCollection<InjectItem> _injectSet = new SynchronizedCollection<InjectItem>();

        private static InjectItem CallInject(string domainName)
        {
            if (_injectSet == null)
            {
                var set = new SynchronizedCollection<InjectItem>();
                set.Add(new InjectItem(AppDomain.CurrentDomain));
                Interlocked.CompareExchange(ref _injectSet, set, null);
            }
            if (string.IsNullOrEmpty(domainName))
            {
                return _injectSet.First();
            }

            domainName = string.Format("_{0}", domainName);
            var q = from t in _injectSet
                    where t.Domain.FriendlyName == domainName
                    select t;
            var item = q.SingleOrDefault();
            if (item == null)
            {
                _injectSet.Add(item = new InjectItem(AppDomain.CreateDomain(domainName)));
            }
            return item;
        }

        public static Assembly Inject(Guid checksum, object arg, Stream rawStream = null, string domainName = null)
        {
            Contract.Requires(checksum != Guid.Empty);

            var item = CallInject(domainName);
            if (rawStream != null)
            {
                var raw = new MemoryStream();
                rawStream.FixedCopyTo(raw);
                raw.Position = 0L;
                Guid checksumNew = CryptoManaged.MD5Hash(raw);
                if (checksum != checksumNew)
                {
                    throw new InvalidOperationException("checksum");
                }
                return item.Mapper.GetOrAdd(checksum, k => item.Domain.Load(raw.ToArray()));
            }
            Assembly ass;
            if (!item.Mapper.TryGetValue(checksum, out ass))
            {
                throw new InvalidOperationException("checksum");
            }
            Type entryType = ass.GetType(string.Format("{0}.Program", ass.FullName), false);
            if (entryType == null)
            {
                var eType = typeof(IAppEntry);
                var q = from t in ass.GetTypes()
                        where t.IsSubclassOf(eType)
                        select t;
                entryType = q.FirstOrDefault();
            }
            if (entryType != null)
            {
                var creator = entryType.GetConstructor(Type.EmptyTypes);
                if (creator != null)
                {
                    var entry = (IAppEntry)creator.Invoke(null);
                    try
                    {
                        entry.Main(arg);
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "Inject");
#if DEBUG
                        throw;              
#endif
                    }
                }
            }
            return ass;
        }

        public static void Uninject(string domainName)
        {
            Contract.Requires(!string.IsNullOrEmpty(domainName));

            var item = CallInject(domainName);
            _injectSet.Remove(item);
            try
            {
                AppDomain.Unload(item.Domain);
            }
            catch (Exception ex)
            {
                LogError(ex, "Uninject");
#if DEBUG
                throw;
#endif
            }
        }
    }
}