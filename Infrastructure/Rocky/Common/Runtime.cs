﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace Rocky
{
    public sealed class Runtime : IServiceProvider, IDisposeService
    {
        #region Fields
        public const string DebugSymbal = "DEBUG";
        private static log4net.ILog DefaultLogger, ExceptionLogger;
        private static readonly ConcurrentDictionary<string, Assembly> _injectMapper;
        private static Runtime _host;
        #endregion

        #region Properties
        public static ushort MaxThreadCount
        {
            get { return (ushort)(Environment.ProcessorCount + 1); }
        }
        public static IServiceProvider Host
        {
            get { return _host; }
        }
        public static IDisposeService DisposeService
        {
            get { return _host; }
        }
        #endregion

        #region Constructor
        static Runtime()
        {
            log4net.Config.XmlConfigurator.Configure();
            DefaultLogger = log4net.LogManager.GetLogger("DefaultLogger");
            ExceptionLogger = log4net.LogManager.GetLogger("ExceptionLogger");
            _injectMapper = new ConcurrentDictionary<string, Assembly>();
            _host = new Runtime();
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                LogError((Exception)e.ExceptionObject, "Unhandled:{0}", sender);
            };
        }
        #endregion

        #region Log
        [Conditional(DebugSymbal)]
        [DebuggerStepThrough]
        [Pure]
        public static void LogDebug(string messageOrFormat, params object[] formatArgs)
        {
            if (!formatArgs.IsNullOrEmpty())
            {
                DefaultLogger.DebugFormat(messageOrFormat, formatArgs);
                return;
            }
            DefaultLogger.Debug(messageOrFormat);
        }

        [DebuggerStepThrough]
        [Pure]
        public static void LogInfo(string messageOrFormat, params object[] formatArgs)
        {
            if (!formatArgs.IsNullOrEmpty())
            {
                DefaultLogger.InfoFormat(messageOrFormat, formatArgs);
                return;
            }
            DefaultLogger.Info(messageOrFormat);
        }

        [DebuggerStepThrough]
        [Pure]
        public static void LogError(Exception ex, string messageOrFormat, params object[] formatArgs)
        {
            var msg = new StringBuilder();
            if (!formatArgs.IsNullOrEmpty())
            {
                msg.AppendFormat(messageOrFormat, formatArgs);
            }
            else
            {
                msg.Append(messageOrFormat);
            }
            try
            {
                var process = Process.GetCurrentProcess();
                msg.Insert(0, Environment.NewLine);
                msg.Insert(0, string.Format("[Process={0}:{1}]", process.Id, process.ProcessName));
            }
            catch (Exception pEx)
            {
                //Process Information Unavailable
                msg.AppendFormat("Process Information Unavailable:{0}", pEx.Message);
            }
            ExceptionLogger.Error(msg, ex);
        }
        #endregion

        #region Methods
        public static void LoopSleep(ref int loopIndex)
        {
            int procCount = Environment.ProcessorCount;
            if (procCount == 1 || (++loopIndex % (procCount * 50)) == 0)
            {
                //----- Single-core!
                //----- Switch to another running thread!
                Thread.Sleep(5);
            }
            else
            {
                //----- Multi-core / HT!
                //----- Loop n iterations!
                Thread.SpinWait(20);
            }
        }

        public static bool Retry(Func<bool> func, int retryCount, int? retryWaitTimeout = null)
        {
            Contract.Requires(func != null);

            int failTimes = 0;
            while (failTimes < retryCount)
            {
                if (func())
                {
                    return true;
                }
                if (retryWaitTimeout.HasValue)
                {
                    Thread.Sleep(Math.Max(1, retryWaitTimeout.Value));
                    failTimes++;
                }
                else
                {
                    LoopSleep(ref failTimes);
                }
            }
            return false;
        }

        public static TDelegate Lambda<TDelegate>(MethodInfo method)
        {
            Contract.Requires(method != null);

            var paramExpressions = method.GetParameters().Select(p => Expression.Parameter(p.ParameterType, p.Name)).ToList();
            MethodCallExpression callExpression;
            if (method.IsStatic)
            {
                callExpression = Expression.Call(method, paramExpressions);
            }
            else
            {
                var instanceExpression = Expression.Parameter(method.ReflectedType, "instance");
                callExpression = Expression.Call(instanceExpression, method, paramExpressions);
                paramExpressions.Insert(0, instanceExpression);
            }
            var lambdaExpression = Expression.Lambda<TDelegate>(callExpression, paramExpressions);
            return lambdaExpression.Compile();
        }

        public static Assembly Inject(string checksum, Stream rawStream = null, object arg = null)
        {
            Contract.Requires(!string.IsNullOrEmpty(checksum));

            if (rawStream != null)
            {
                var raw = new MemoryStream();
                rawStream.FixedCopyTo(raw);
                raw.Position = 0L;
                string checksumNew = CryptoManaged.MD5Hash(raw);
                if (checksum != checksumNew)
                {
                    throw new InvalidOperationException("checksum");
                }
                return _injectMapper.GetOrAdd(checksum, k => AppDomain.CurrentDomain.Load(raw.ToArray()));
            }
            Assembly ass;
            if (!_injectMapper.TryGetValue(checksum, out ass))
            {
                throw new InvalidOperationException("checksum");
            }
            Type entryType = ass.GetType(string.Format("{0}.Program", ass.FullName), true);
            var entry = (IRawEntry)Activator.CreateInstance(entryType);
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
            return ass;
        }

        /// <summary>
        /// 注册服务对象实例(单例)
        /// </summary>
        /// <param name="service"></param>
        public static void Register(object service)
        {
            Contract.Requires(service != null);

            _host.Register(service.GetType(), service);
        }
        #endregion

        #region IO
        public static string CombinePath(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        public static void CreateDirectory(string path)
        {
            path = Path.GetDirectoryName(path);
            if (path == null)
            {
                return;
            }

            Directory.CreateDirectory(path);
        }

        public static Stream GetResourceStream(string name, string dllPath = null)
        {
            Contract.Ensures(Contract.Result<Stream>() != null);

            Assembly dll = dllPath == null ? Assembly.GetCallingAssembly() : Assembly.LoadFrom(dllPath);
            string[] names = dll.GetManifestResourceNames();
            return dll.GetManifestResourceStream(name);
        }

        public static bool CreateFileFromResource(string name, string filePath, string dllPath = null)
        {
            Assembly dll = dllPath == null ? Assembly.GetCallingAssembly() : Assembly.LoadFrom(dllPath);
            var stream = dll.GetManifestResourceStream(name);
            var file = new FileInfo(filePath);
            if (file.Exists)
            {
                string checksum = CryptoManaged.MD5Hash(stream);
                stream.Position = 0L;
                using (var fileStream = file.OpenRead())
                {
                    if (checksum == CryptoManaged.MD5Hash(fileStream))
                    {
                        return false;
                    }
                }
            }
            using (var fileStream = file.OpenWrite())
            {
                stream.FixedCopyTo(fileStream);
            }
            return true;
        }
        #endregion

        #region Instance
        private ConcurrentDictionary<Type, object> _container;

        private Runtime()
        {
            _container = new ConcurrentDictionary<Type, object>();
            _container.TryAdd(typeof(IDisposeService), this);
        }

        public void Register(Type serviceType, object serviceInstance)
        {
            if (!_container.TryAdd(serviceType, serviceInstance))
            {
                throw new ArgumentException("serviceType");
            }
        }

        public object GetService(Type serviceType)
        {
            object serviceInstance;
            if (!_container.TryGetValue(serviceType, out serviceInstance))
            {
                foreach (var svc in _container.Values)
                {
                    if (serviceType.IsInstanceOfType(svc))
                    {
                        serviceInstance = svc;
                        break;
                    }
                }
            }
            return serviceInstance;
        }

        void IDisposeService.Register(Type owner, IDisposable instance)
        {
            var queue = _container.GetOrAdd(owner, k => new ConcurrentBag<IDisposable>()) as ConcurrentBag<IDisposable>;
            if (queue == null)
            {
                throw new InvalidOperationException("owner");
            }
            queue.Add(instance);
        }

        void IDisposeService.Free(Type owner, IDisposable instance)
        {
            try
            {
                object boxed;
                if (!_container.TryGetValue(owner, out boxed))
                {
                    return;
                }
                var queue = (ConcurrentBag<IDisposable>)boxed;
                if (queue.TryTake(out instance))
                {
                    instance.Dispose();
                }
            }
            catch (ObjectDisposedException ex)
            {
                LogError(ex, "IDisposeService.Free");
            }
        }

        void IDisposeService.FreeAll(Type owner)
        {
            object boxed;
            if (!_container.TryRemove(owner, out boxed))
            {
                return;
            }
            var queue = (ConcurrentBag<IDisposable>)boxed;
            foreach (var instance in queue)
            {
                instance.Dispose();
            }
        }
        #endregion
    }
}