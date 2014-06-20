using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// http://stackoverflow.com/questions/7044971/createinstanceandunwrap-and-domain
    /// http://blogs.microsoft.co.il/sasha/2008/07/19/appdomains-and-remoting-life-time-service/
    /// </summary>
    public class Sandboxer : MarshalByRefObject
    {
        #region Fields
        public readonly AppDomain _Domain;
        private ConcurrentDictionary<Guid, Assembly> _mapper;
        private string _assemblyPath;
        #endregion

        #region Properties
        public string Name { get; private set; }
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
            Contract.Requires(domain != null && !domain.IsFinalizingForUnload());

            _Domain = domain;
            this.Name = _Domain.FriendlyName;
        }

        //[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            ILease lease = (ILease)base.InitializeLifetimeService();
            if (lease.CurrentState == LeaseState.Initial)
            {
                lease.InitialLeaseTime = TimeSpan.FromMinutes(8d);
                lease.SponsorshipTimeout = TimeSpan.FromMinutes(2d);
                lease.RenewOnCallTime = TimeSpan.FromMinutes(2d);
            }
            return lease;
        }
        #endregion

        #region Methods
        public object Execute(string assemblyType, object arg)
        {
            Contract.Requires(!string.IsNullOrEmpty(assemblyType));

            string assemblyName, typeName;
            Rename(assemblyType, out assemblyName, out typeName);
            var target = (IAppEntry)_Domain.CreateInstanceAndUnwrap(assemblyName, typeName);
            try
            {
                return target.DoEntry(arg);
            }
            catch (SecurityException ex)
            {
                (new PermissionSet(PermissionState.Unrestricted)).Assert();
                App.LogError(ex, "ExecuteEntry");
                CodeAccessPermission.RevertAssert();
            }
            return null;
        }
        public object Execute(string assemblyType, string entryPoint, object[] args)
        {
            Contract.Requires(!string.IsNullOrEmpty(assemblyType));

            string assemblyName, typeName;
            Rename(assemblyType, out assemblyName, out typeName);
            var target = Assembly.Load(assemblyName).GetType(typeName).GetMethod(entryPoint);
            try
            {
                return target.Invoke(null, args);
            }
            catch (SecurityException ex)
            {
                (new PermissionSet(PermissionState.Unrestricted)).Assert();
                App.LogError(ex, "ExecuteEntry");
                CodeAccessPermission.RevertAssert();
            }
            return null;
        }
        private void Rename(string assemblyType, out string assemblyName, out string typeName)
        {
            string[] set = assemblyType.Split(new string[] { ", " }, 2, StringSplitOptions.None);
            if (set.Length != 2)
            {
                throw new ArgumentException("assemblyType");
            }
            assemblyName = set[1];
            typeName = set[0];
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
            catch (SecurityException ex)
            {
                //When we print informations from a SecurityException extra information can be printed 
                //if we are calling it with a full-trust stack.
                (new PermissionSet(PermissionState.Unrestricted)).Assert();
                App.LogError(ex, "ExecuteAssembly");
                CodeAccessPermission.RevertAssert();
            }
        }
        #endregion

        #region AssemblyReflection
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
                return this.Mapper.GetOrAdd(checksum, k => _Domain.Load(raw.ToArray()));
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
                    entry.DoEntry(arg);
                }
            }
            return assembly;
        }

        public void LoadAssembly(string assemblyPath)
        {
            try
            {
                _assemblyPath = assemblyPath;
                Assembly.ReflectionOnlyLoadFrom(assemblyPath);
            }
            catch (FileNotFoundException)
            {
                // Continue loading assemblies even if an assembly can not be loaded in the new AppDomain.
            }
        }
        public TResult Reflect<TResult>(Func<Assembly, TResult> func)
        {
            DirectoryInfo directory = new FileInfo(_assemblyPath).Directory;
            ResolveEventHandler resolveEventHandler = (sender, e) =>
            {
                return OnReflectionOnlyResolve(e, directory);
            };

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolveEventHandler;
            try
            {
                var assembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(asm => asm.Location.CompareTo(_assemblyPath) == 0);
                return func(assembly);
            }
            finally
            {
                AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolveEventHandler;
            }
        }
        private Assembly OnReflectionOnlyResolve(ResolveEventArgs args, DirectoryInfo directory)
        {
            Assembly loadedAssembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(asm => string.Equals(asm.FullName, args.Name, StringComparison.OrdinalIgnoreCase));
            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            AssemblyName assemblyName = new AssemblyName(args.Name);
            string dependentAssemblyFilename = Path.Combine(directory.FullName, assemblyName.Name + ".dll");
            if (File.Exists(dependentAssemblyFilename))
            {
                return Assembly.ReflectionOnlyLoadFrom(dependentAssemblyFilename);
            }
            return Assembly.ReflectionOnlyLoad(args.Name);
        }
        #endregion
    }
}