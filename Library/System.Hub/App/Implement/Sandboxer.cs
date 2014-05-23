using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// http://stackoverflow.com/questions/7044971/createinstanceandunwrap-and-domain
    /// </summary>
    public sealed class Sandboxer : MarshalByRefObject, IDisposable
    {
        #region Static
        private static SynchronizedCollection<Sandboxer> _boxes;

        static Sandboxer()
        {
            _boxes = new SynchronizedCollection<Sandboxer>();
            _boxes.Add(new Sandboxer(AppDomain.CurrentDomain));
        }

        private static AppDomain CreateDomain(string dName, bool isTrusted)
        {
            if (isTrusted)
            {
                return AppDomain.CreateDomain(dName);
            }
            else
            {
                var adSetup = new AppDomainSetup()
                {
                    ApplicationBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                };
                //Setting the permissions for the AppDomain. We give the permission to execute and to 
                //read/discover the location where the untrusted code is loaded.
                var permSet = new PermissionSet(PermissionState.None);
                permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                //We want the sandboxer assembly's strong name, so that we can add it to the full trust list.
                var fullTrustAssembly = typeof(Sandboxer).Assembly.Evidence.GetHostEvidence<StrongName>();
                return AppDomain.CreateDomain(dName, null, adSetup, permSet, fullTrustAssembly);
            }
        }

        public static Sandboxer Create(string name, bool isTrusted = false)
        {
            if (name == null)
            {
                return _boxes[0];
            }

            name = string.Format("Sandbox_{0}", name);
            var q = from t in _boxes
                    where t.Name == name
                    select t;
            var box = q.SingleOrDefault();
            if (box == null)
            {
                var domain = CreateDomain(name, isTrusted);
                //Use CreateInstanceFrom to load an instance of the Sandboxer class into the AppDomain. 
                var handle = Activator.CreateInstanceFrom(domain, typeof(Sandboxer).Assembly.ManifestModule.FullyQualifiedName, typeof(Sandboxer).FullName,
                    true, BindingFlags.CreateInstance, null, new object[] { domain }, null, null);
                //Unwrap the new domain instance into a reference in this domain and use it to execute the code.
                _boxes.Add(box = (Sandboxer)handle.Unwrap());
            }
            return box;
        }
        #endregion

        #region Fields
        private AppDomain _domain;
        private ConcurrentDictionary<Guid, Assembly> _mapper;
        #endregion

        #region Properties
        public string Name
        {
            get { return _domain.FriendlyName; }
        }
        private ConcurrentDictionary<Guid, Assembly> Mapper
        {
            get
            {
                if (_mapper == null)
                {
                    _mapper = new ConcurrentDictionary<Guid, Assembly>();
                }
                return _mapper;
            }
        }
        #endregion

        #region Constructor
        public Sandboxer(AppDomain domain)
        {
            Contract.Requires(domain != null);

            _domain = domain;
        }

        void IDisposable.Dispose()
        {
            try
            {
                _boxes.Remove(this);
                AppDomain.Unload(_domain);
            }
            catch (Exception ex)
            {
                App.LogError(ex, "Sandboxer");
#if DEBUG
                throw;
#endif
            }
        }
        #endregion

        #region Methods
        public Assembly Inject(Guid checksum, object arg, Stream rawStream = null)
        {
            Contract.Requires(checksum != Guid.Empty);

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
                return this.Mapper.GetOrAdd(checksum, k => _domain.Load(raw.ToArray()));
            }
            Assembly assembly;
            if (!this.Mapper.TryGetValue(checksum, out assembly))
            {
                throw new InvalidOperationException("checksum");
            }
            var eType = typeof(IAppEntry);
            var q = from t in assembly.GetTypes()
                    where t.IsSubclassOf(eType)
                    select t;
            var entryType = q.FirstOrDefault();
            if (entryType != null)
            {
                var creator = entryType.GetConstructor(Type.EmptyTypes);
                if (creator != null)
                {
                    var entry = (IAppEntry)creator.Invoke(null);
                    entry.Main(arg);
                }
            }
            return assembly;
        }

        public void Execute(string assemblyName, string entryType, object arg)
        {
            var assembly = _domain.Load(assemblyName);
            var type = assembly.GetType(entryType, true);
            var target = (IAppEntry)Activator.CreateInstance(type);
            target.Main(arg);
        }

        public void ExecuteAssembly(string assemblyName, string[] args)
        {
            var assembly = Assembly.Load(assemblyName);
            if (assembly.EntryPoint == null)
            {
                throw new InvalidOperationException("EntryPoint");
            }
            try
            {
                assembly.EntryPoint.Invoke(null, args);
            }
            catch (Exception ex)
            {
                //When we print informations from a SecurityException extra information can be printed 
                //if we are calling it with a full-trust stack.
                (new PermissionSet(PermissionState.Unrestricted)).Assert();
                App.LogError(ex, "ExecuteCode");
                CodeAccessPermission.RevertAssert();
            }
        }
        #endregion
    }
}