using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    partial class App : IServiceProvider, IDisposeService
    {
        private ConcurrentDictionary<Type, object> _container;

        private App()
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

        void IDisposeService.Release(Type owner, IDisposable instance)
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

        void IDisposeService.ReleaseAll(Type owner)
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
    }
}