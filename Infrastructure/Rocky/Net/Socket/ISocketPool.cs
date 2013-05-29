using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Rocky.Net
{
    [ContractClass(typeof(ISocketPoolContract))]
    public interface ISocketPool : IDisposable
    {
        DistributedEndPoint ServerEndpoint { get; }

        Socket Take();
        Socket[] TakeRange(int count);
        void Return(Socket item);
        void Return(Socket[] items);
    }

    [ContractClassFor(typeof(ISocketPool))]
    internal abstract class ISocketPoolContract : ISocketPool
    {
        DistributedEndPoint ISocketPool.ServerEndpoint
        {
            get
            {
                Contract.Ensures(Contract.Result<DistributedEndPoint>() != null);
                return default(DistributedEndPoint);
            }
        }

        Socket ISocketPool.Take()
        {
            Contract.Ensures(Contract.Result<Socket>() != null);
            return default(Socket);
        }

        Socket[] ISocketPool.TakeRange(int count)
        {
            Contract.Requires(count > 0);
            Contract.Ensures(Contract.Result<Socket[]>() != null && Contract.Result<Socket[]>().Length == count);
            return default(Socket[]);
        }

        void ISocketPool.Return(Socket item)
        {
            Contract.Requires(item != null);
        }

        void ISocketPool.Return(Socket[] items)
        {
            Contract.Requires(!items.IsNullOrEmpty());
        }

        void IDisposable.Dispose()
        {

        }
    }
}