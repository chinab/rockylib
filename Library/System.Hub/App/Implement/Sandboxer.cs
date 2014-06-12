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
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>
    /// http://stackoverflow.com/questions/7044971/createinstanceandunwrap-and-domain
    /// </summary>
    public class Sandboxer : MarshalByRefObject
    {
        #region Static
        private static SynchronizedCollection<Sandboxer> _boxes;

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
            Contract.Requires(!string.IsNullOrEmpty(name));

            var current = AppDomain.CurrentDomain;
            if (name == current.FriendlyName)
            {
                return new Sandboxer(current);
            }

            if (_boxes == null)
            {
                Interlocked.CompareExchange(ref _boxes, new SynchronizedCollection<Sandboxer>(), null);
            }
            name = string.Format("Sandbox_{0}", name);
            var q = from t in _boxes
                    where t.Name == name
                    select t;
            var box = q.SingleOrDefault();
            if (box == null)
            {
                var _Domain = CreateDomain(name, isTrusted);
                //Use CreateInstanceFrom to load an instance of the Sandboxer class into the AppDomain. 
                var handle = Activator.CreateInstanceFrom(_Domain, typeof(Sandboxer).Assembly.ManifestModule.FullyQualifiedName, typeof(Sandboxer).FullName,
                    true, BindingFlags.CreateInstance, null, new object[] { _Domain }, null, null);
                //Unwrap the new domain instance into a reference in this domain and use it to execute the code.
                _boxes.Add(box = (Sandboxer)handle.Unwrap());
            }
            return box;
        }

        public static void Unload(Sandboxer box)
        {
            if (box == null || box._Domain.IsDefaultAppDomain())
            {
                return;
            }

            if (_boxes != null)
            {
                bool ok = _boxes.Remove(box);
#if DEBUG
                App.LogInfo("Unload {0} {1}", box.Name, ok);
#endif
            }
            //AppDomain不能在IDisposable中卸载？
            //延迟卸载确保AppDomain内的执行完毕否则会引发AppDomainUnloadedException
            new JobTimer(state =>
            {
                var b = (Sandboxer)state;
                try
                {
#if DEBUG
                    App.LogInfo("{0} unload", b._Domain.FriendlyName);
#endif
                    AppDomain.Unload(b._Domain);
                }
                catch (Exception ex)
                {
                    App.LogError(ex, "Sandboxer Unload");
#if DEBUG
                    throw;
#endif
                }
            }, DateTime.Now.AddSeconds(16d)).Start(box);
        }
        #endregion

        #region Fields
        public readonly AppDomain _Domain;
        private ConcurrentDictionary<Guid, Assembly> _mapper;
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
            Contract.Requires(domain != null);

            _Domain = domain;
            this.Name = _Domain.FriendlyName;
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
    }

    public class AssemblyReflectionProxy : MarshalByRefObject
    {
        private string _assemblyPath;

        public void LoadAssembly(String assemblyPath)
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
            ResolveEventHandler resolveEventHandler =
                (s, e) =>
                {
                    return OnReflectionOnlyResolve(
                        e, directory);
                };

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += resolveEventHandler;

            var assembly = AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies().FirstOrDefault(a => a.Location.CompareTo(_assemblyPath) == 0);

            var result = func(assembly);

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= resolveEventHandler;

            return result;
        }

        private Assembly OnReflectionOnlyResolve(ResolveEventArgs args, DirectoryInfo directory)
        {
            Assembly loadedAssembly =
                AppDomain.CurrentDomain.ReflectionOnlyGetAssemblies()
                    .FirstOrDefault(
                      asm => string.Equals(asm.FullName, args.Name,
                          StringComparison.OrdinalIgnoreCase));

            if (loadedAssembly != null)
            {
                return loadedAssembly;
            }

            AssemblyName assemblyName =
                new AssemblyName(args.Name);
            string dependentAssemblyFilename =
                Path.Combine(directory.FullName,
                assemblyName.Name + ".dll");

            if (File.Exists(dependentAssemblyFilename))
            {
                return Assembly.ReflectionOnlyLoadFrom(
                    dependentAssemblyFilename);
            }
            return Assembly.ReflectionOnlyLoad(args.Name);
        }
    }
    public class AssemblyReflectionManager : IDisposable
    {
        Dictionary<string, AppDomain> _mapDomains = new Dictionary<string, AppDomain>();
        Dictionary<string, AppDomain> _loadedAssemblies = new Dictionary<string, AppDomain>();
        Dictionary<string, AssemblyReflectionProxy> _proxies = new Dictionary<string, AssemblyReflectionProxy>();

        public bool LoadAssembly(string assemblyPath, string domainName)
        {
            // if the assembly file does not exist then fail
            if (!File.Exists(assemblyPath))
                return false;

            // if the assembly was already loaded then fail
            if (_loadedAssemblies.ContainsKey(assemblyPath))
            {
                return false;
            }

            // check if the appdomain exists, and if not create a new one
            AppDomain appDomain = null;
            if (_mapDomains.ContainsKey(domainName))
            {
                appDomain = _mapDomains[domainName];
            }
            else
            {
                appDomain = CreateChildDomain(AppDomain.CurrentDomain, domainName);
                _mapDomains[domainName] = appDomain;
            }

            // load the assembly in the specified app domain
            //try
            //{
            Type proxyType = typeof(AssemblyReflectionProxy);
            if (proxyType.Assembly != null)
            {
                var proxy =
                    (AssemblyReflectionProxy)appDomain.
                        CreateInstanceFrom(
                        proxyType.Assembly.Location,
                        proxyType.FullName).Unwrap();

                proxy.LoadAssembly(assemblyPath);

                _loadedAssemblies[assemblyPath] = appDomain;
                _proxies[assemblyPath] = proxy;

                return true;
            }
            //}
            //catch
            //{ }

            return false;
        }

        public bool UnloadAssembly(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
                return false;

            // check if the assembly is found in the internal dictionaries
            if (_loadedAssemblies.ContainsKey(assemblyPath) &&

               _proxies.ContainsKey(assemblyPath))
            {
                // check if there are more assemblies loaded in the same app domain; in this case fail
                AppDomain appDomain = _loadedAssemblies[assemblyPath];
                int count = _loadedAssemblies.Values.Count(a => a == appDomain);
                if (count != 1)
                    return false;

                //try
                //{
                // remove the appdomain from the dictionary and unload it from the process
                _mapDomains.Remove(appDomain.FriendlyName);
                AppDomain.Unload(appDomain);

                // remove the assembly from the dictionaries
                _loadedAssemblies.Remove(assemblyPath);
                _proxies.Remove(assemblyPath);

                return true;
                //}
                //catch
                //{
                //}
            }

            return false;
        }

        public bool UnloadDomain(string domainName)
        {
            // check the appdomain name is valid
            if (string.IsNullOrEmpty(domainName))
                return false;

            // check we have an instance of the domain
            if (_mapDomains.ContainsKey(domainName))
            {
                //try
                //{
                var appDomain = _mapDomains[domainName];

                // check the assemblies that are loaded in this app domain
                var assemblies = new List<string>();
                foreach (var kvp in _loadedAssemblies)
                {
                    if (kvp.Value == appDomain)
                        assemblies.Add(kvp.Key);
                }

                // remove these assemblies from the internal dictionaries
                foreach (var assemblyName in assemblies)
                {
                    _loadedAssemblies.Remove(assemblyName);
                    _proxies.Remove(assemblyName);
                }

                // remove the appdomain from the dictionary
                _mapDomains.Remove(domainName);

                // unload the appdomain
                AppDomain.Unload(appDomain);

                return true;
                //}
                //catch
                //{
                //}
            }

            return false;
        }

        public TResult Reflect<TResult>(string assemblyPath, Func<Assembly, TResult> func)
        {
            // check if the assembly is found in the internal dictionaries
            if (_loadedAssemblies.ContainsKey(assemblyPath) &&
               _proxies.ContainsKey(assemblyPath))
            {
                return _proxies[assemblyPath].Reflect(func);
            }

            return default(TResult);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~AssemblyReflectionManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var appDomain in _mapDomains.Values)
                    AppDomain.Unload(appDomain);

                _loadedAssemblies.Clear();
                _proxies.Clear();
                _mapDomains.Clear();
            }
        }

        private AppDomain CreateChildDomain(AppDomain parentDomain, string domainName)
        {
            Evidence evidence = new Evidence(parentDomain.Evidence);
            AppDomainSetup setup = parentDomain.SetupInformation;
            return AppDomain.CreateDomain(domainName, evidence, setup);
        }
    }
}