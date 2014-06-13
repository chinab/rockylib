using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
    public class SandboxManager //: Disposable
    {
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

        /// <summary>
        /// 创建沙箱
        /// </summary>
        /// <param name="name">Null则为CurrentDomain</param>
        /// <param name="isTrusted"></param>
        /// <returns></returns>
        public static Sandboxer Create(string name, bool isTrusted = false)
        {
            if (string.IsNullOrEmpty(name))
            {
                return new Sandboxer(AppDomain.CurrentDomain);
            }

            if (_boxes == null)
            {
                Interlocked.CompareExchange(ref _boxes, new SynchronizedCollection<Sandboxer>(), null);
            }
            lock (_boxes)
            {
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
                App.LogInfo("Sandbox Ref Decrement {0} {1}", box.Name, ok);
#endif
            }
            //AppDomain不能在IDisposable中卸载？
            //延迟卸载确保AppDomain内部执行完毕否则会引发AppDomainUnloadedException
            new JobTimer(state =>
            {
                var b = (Sandboxer)state;
                try
                {
#if DEBUG
                    App.LogInfo("Sandbox Unload {0}", b.Name);
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

        //Dictionary<string, AppDomain> _mapDomains = new Dictionary<string, AppDomain>();
        //Dictionary<string, AppDomain> _loadedAssemblies = new Dictionary<string, AppDomain>();
        //Dictionary<string, AssemblyReflectionProxy> _proxies = new Dictionary<string, AssemblyReflectionProxy>();

        //public bool LoadAssembly(string assemblyPath, string domainName)
        //{
        //    // if the assembly file does not exist then fail
        //    if (!File.Exists(assemblyPath))
        //        return false;

        //    // if the assembly was already loaded then fail
        //    if (_loadedAssemblies.ContainsKey(assemblyPath))
        //    {
        //        return false;
        //    }

        //    // check if the appdomain exists, and if not create a new one
        //    AppDomain appDomain = null;
        //    if (_mapDomains.ContainsKey(domainName))
        //    {
        //        appDomain = _mapDomains[domainName];
        //    }
        //    else
        //    {
        //        appDomain = CreateChildDomain(AppDomain.CurrentDomain, domainName);
        //        _mapDomains[domainName] = appDomain;
        //    }

        //    // load the assembly in the specified app domain
        //    //try
        //    //{
        //    Type proxyType = typeof(AssemblyReflectionProxy);
        //    if (proxyType.Assembly != null)
        //    {
        //        var proxy =
        //            (AssemblyReflectionProxy)appDomain.
        //                CreateInstanceFrom(
        //                proxyType.Assembly.Location,
        //                proxyType.FullName).Unwrap();

        //        proxy.LoadAssembly(assemblyPath);

        //        _loadedAssemblies[assemblyPath] = appDomain;
        //        _proxies[assemblyPath] = proxy;

        //        return true;
        //    }
        //    //}
        //    //catch
        //    //{ }

        //    return false;
        //}

        //public bool UnloadAssembly(string assemblyPath)
        //{
        //    if (!File.Exists(assemblyPath))
        //        return false;

        //    // check if the assembly is found in the internal dictionaries
        //    if (_loadedAssemblies.ContainsKey(assemblyPath) &&

        //       _proxies.ContainsKey(assemblyPath))
        //    {
        //        // check if there are more assemblies loaded in the same app domain; in this case fail
        //        AppDomain appDomain = _loadedAssemblies[assemblyPath];
        //        int count = _loadedAssemblies.Values.Count(a => a == appDomain);
        //        if (count != 1)
        //            return false;

        //        //try
        //        //{
        //        // remove the appdomain from the dictionary and unload it from the process
        //        _mapDomains.Remove(appDomain.FriendlyName);
        //        AppDomain.Unload(appDomain);

        //        // remove the assembly from the dictionaries
        //        _loadedAssemblies.Remove(assemblyPath);
        //        _proxies.Remove(assemblyPath);

        //        return true;
        //        //}
        //        //catch
        //        //{
        //        //}
        //    }

        //    return false;
        //}

        //public bool UnloadDomain(string domainName)
        //{
        //    // check the appdomain name is valid
        //    if (string.IsNullOrEmpty(domainName))
        //        return false;

        //    // check we have an instance of the domain
        //    if (_mapDomains.ContainsKey(domainName))
        //    {
        //        //try
        //        //{
        //        var appDomain = _mapDomains[domainName];

        //        // check the assemblies that are loaded in this app domain
        //        var assemblies = new List<string>();
        //        foreach (var kvp in _loadedAssemblies)
        //        {
        //            if (kvp.Value == appDomain)
        //                assemblies.Add(kvp.Key);
        //        }

        //        // remove these assemblies from the internal dictionaries
        //        foreach (var assemblyName in assemblies)
        //        {
        //            _loadedAssemblies.Remove(assemblyName);
        //            _proxies.Remove(assemblyName);
        //        }

        //        // remove the appdomain from the dictionary
        //        _mapDomains.Remove(domainName);

        //        // unload the appdomain
        //        AppDomain.Unload(appDomain);

        //        return true;
        //        //}
        //        //catch
        //        //{
        //        //}
        //    }

        //    return false;
        //}

        //public TResult Reflect<TResult>(string assemblyPath, Func<Assembly, TResult> func)
        //{
        //    // check if the assembly is found in the internal dictionaries
        //    if (_loadedAssemblies.ContainsKey(assemblyPath) &&
        //       _proxies.ContainsKey(assemblyPath))
        //    {
        //        return _proxies[assemblyPath].Reflect(func);
        //    }

        //    return default(TResult);
        //}

        //protected virtual void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        foreach (var appDomain in _mapDomains.Values)
        //            AppDomain.Unload(appDomain);

        //        _loadedAssemblies.Clear();
        //        _proxies.Clear();
        //        _mapDomains.Clear();
        //    }
        //}

        //private AppDomain CreateChildDomain(AppDomain parentDomain, string domainName)
        //{
        //    Evidence evidence = new Evidence(parentDomain.Evidence);
        //    AppDomainSetup setup = parentDomain.SetupInformation;
        //    return AppDomain.CreateDomain(domainName, evidence, setup);
        //}
    }
}