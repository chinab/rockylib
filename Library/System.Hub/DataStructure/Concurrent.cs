using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Collections.Concurrent
{
    public static class Concurrent
    {
        public static Partitioner<T> GetConsumingPartitioner<T>(this BlockingCollection<T> collection)
        {
            return new BlockingCollectionPartitioner<T>(collection);
        }
        /// <summary>
        /// http://stackoverflow.com/questions/10208330/why-does-iterating-over-getconsumingenumerable-not-fully-empty-the-underlying
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class BlockingCollectionPartitioner<T> : Partitioner<T>
        {
            private BlockingCollection<T> _collection;

            public override bool SupportsDynamicPartitions
            {
                get { return true; }
            }

            internal BlockingCollectionPartitioner(BlockingCollection<T> collection)
            {
                if (collection == null)
                {
                    throw new ArgumentNullException("collection");
                }
                _collection = collection;
            }

            public override IEnumerable<T> GetDynamicPartitions()
            {
                return _collection.GetConsumingEnumerable();
            }
            public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
            {
                if (partitionCount < 1)
                {
                    throw new ArgumentOutOfRangeException("partitionCount");
                }
                var dynamicPartitioner = GetDynamicPartitions();
                return Enumerable.Range(0, partitionCount).Select(_ => dynamicPartitioner.GetEnumerator()).ToArray();
            }
        }
    }
}