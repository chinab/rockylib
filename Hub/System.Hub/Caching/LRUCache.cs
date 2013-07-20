using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;

namespace System.Caching
{
    public class LRUCache : Disposable
    {
        #region Nested Interfaces
        /// <summary>
        /// The public wrapper for a Index
        /// Index provides dictionary key / value access to any object in cache
        /// </summary>
        public interface IIndex<TKey, TItem> : IEnumerable<KeyValuePair<TKey, TItem>>
        {
            int Count { get; }
            /// <summary>Getter for index</summary>
            /// <param name="key">key to find (or load if needed)</param>
            /// <returns>the object value associated with key, or null if not found & could not be loaded</returns>
            TItem this[TKey key] { get; set; }
            void Add(TKey key, TItem item);
            void AddRange(IEnumerable<KeyValuePair<TKey, TItem>> set);
            /// <summary>Delete object that matches key from cache</summary>
            /// <param name="key">key to find</param>
            void Remove(TKey key);
            void RemoveRange(IEnumerable<TKey> set);
        }

        /// <summary>
        /// Because there is no auto inheritance between generic types, this interface is used to send messages to Index objects
        /// </summary>
        private interface IIndex
        {
            string IndexName { get; }
            /// <summary>Add new item to index</summary>
            /// <param name="node">item to add</param>
            /// <returns>was item key previously contained in index</returns>
            bool AddNode(Node node);
            /// <summary>try to find this item in the index and return Node</summary>
            Node GetNode(object value);
            /// <summary>removes all items from index and reloads each item (this gets rid of dead nodes)</summary>
            int RebuildIndex();
            /// <summary>Remove all items from index</summary>
            void ClearIndex();
        }
        #endregion

        #region Nested Classes
        private class Index<TKey, TItem> : IIndex<TKey, TItem>, IIndex where TItem : class
        {
            #region Fields
            private const int Timeout = 10000;
            private LRUCache _owner;
            private Dictionary<TKey, WeakReference> _index;
            private ReaderWriterLockSlim _locker;
            private readonly Func<TItem, TKey> _getKey;
            private readonly Func<TKey, TItem> _loadItem;
            #endregion

            #region Properties
            public string IndexName { get; private set; }
            public int Count
            {
                get { return _index.Count; }
            }

            public TItem this[TKey key]
            {
                get
                {
                    var node = this.GetNode(key);
                    if (node != null)
                    {
                        _owner._lifeSpan.Touch(node);
                    }
                    if ((node == null || node.Value == null) && _loadItem != null)
                    {
                        node = _owner.AddAndGetNode(_loadItem(key));
                    }
                    return node == null ? null : (TItem)node.Value;
                }
                set
                {
                    var node = this.GetNode(key);
                    if (node != null)
                    {
                        lock (_owner._lifeSpan)
                        {
                            node.Value = value;
                        }
                    }
                }
            }
            #endregion

            #region Constructors
            /// <summary>constructor</summary>
            /// <param name="owner">parent of index</param>
            /// <param name="getKey">delegate to get key from object</param>
            /// <param name="loadItem">delegate to load object if it is not found in index</param>
            public Index(LRUCache owner, string indexName, Func<TItem, TKey> getKey, Func<TKey, TItem> loadItem)
            {
                if (getKey == null)
                {
                    throw new ArgumentNullException("getKey delegate required");
                }

                _owner = owner;
                this.IndexName = indexName;
                _index = new Dictionary<TKey, WeakReference>(_owner._capacity * 2);
                _getKey = getKey;
                _loadItem = loadItem;
                _locker = new ReaderWriterLockSlim();
                this.RebuildIndex();
            }
            #endregion

            #region IIndex Methods
            public bool AddNode(Node node)
            {
                var item = node.Value as TItem;
                if (item == null)
                {
                    return false;
                }
                TKey key = _getKey(item);
                return LockWrapper.WriteLock(_locker, Timeout, () =>
                {
                    bool isDup = _index.ContainsKey(key);
                    _index[key] = new WeakReference(node, false);
                    return isDup;
                });
            }

            public Node GetNode(object value)
            {
                var item = value as TItem;
                if (item == null)
                {
                    return null;
                }
                return this.GetNode(_getKey(item));
            }
            private Node GetNode(TKey key)
            {
                return LockWrapper.ReadLock(_locker, Timeout, () =>
                {
                    WeakReference value;
                    return (Node)(_index.TryGetValue(key, out value) ? value.Target : null);
                });
            }

            public int RebuildIndex()
            {
                return LockWrapper.WriteLock(_locker, Timeout, () =>
                {
                    _index.Clear();
                    foreach (var node in _owner._lifeSpan)
                    {
                        var item = node.Value as TItem;
                        if (item == null)
                        {
                            continue;
                        }
                        TKey key = _getKey(item);
                        _index[key] = new WeakReference(node, false);
                    }
                    return _index.Count;
                });
            }

            public void ClearIndex()
            {
                LockWrapper.WriteLock(_locker, Timeout, () =>
                {
                    _index.Clear();
                    return false;
                });
            }
            #endregion

            #region Methods
            public void Add(TKey key, TItem item)
            {
                var node = _owner._lifeSpan.Add(item);
                LockWrapper.WriteLock(_locker, Timeout, () =>
                {
                    _index[key] = new WeakReference(node, false);
                    return false;
                });
            }
            public void AddRange(IEnumerable<KeyValuePair<TKey, TItem>> set)
            {
                LockWrapper.WriteLock(_locker, Timeout, () =>
                {
                    foreach (var pair in set)
                    {
                        var node = _owner._lifeSpan.Add(pair.Value);
                        _index[pair.Key] = new WeakReference(node, false);
                    }
                    return false;
                });
            }

            public void Remove(TKey key)
            {
                LockWrapper.WriteLock(_locker, Timeout, () =>
                {
                    _index.Remove(key);
                    return false;
                });
            }
            public void RemoveRange(IEnumerable<TKey> set)
            {
                LockWrapper.WriteLock(_locker, Timeout, () =>
                {
                    foreach (var key in set)
                    {
                        _index.Remove(key);
                    }
                    return false;
                });
            }

            public IEnumerator<KeyValuePair<TKey, TItem>> GetEnumerator()
            {
                return LockWrapper.ReadLock(_locker, Timeout, () =>
                {
                    return _index.Where(t => t.Value.IsAlive).Select(t => new KeyValuePair<TKey, TItem>(t.Key, (TItem)t.Value.Target)).ToList().GetEnumerator();
                });
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
            #endregion
        }

        #region LifespanMgr
        private class LifespanMgr : IEnumerable<Node>
        {
            #region Fields
            internal LRUCache Owner;
            private readonly TimeSpan _minAge;
            private readonly TimeSpan _maxAge;
            private readonly TimeSpan _timeSlice;
            private DateTime _nextValidCheck;
            private readonly int _bagItemLimit;

            private readonly AgeBag[] _bags;
            private AgeBag _currentBag;
            private int _currentSize;
            private int _current;
            private int _oldest;
            private const int _size = 265; // based on 240 timeslices + 20 bags for ItemLimit + 5 bags empty buffer
            #endregion

            #region Constructors
            public LifespanMgr(LRUCache owner, TimeSpan minAge, TimeSpan maxAge)
            {
                Owner = owner;
                int maxMS = Math.Min((int)maxAge.TotalMilliseconds, 12 * 60 * 60 * 1000); // max = 12 hours
                _minAge = minAge;
                _maxAge = TimeSpan.FromMilliseconds(maxMS);
                _timeSlice = TimeSpan.FromMilliseconds(maxMS / 240.0); // max timeslice = 3 min
                _bagItemLimit = Owner._capacity / 20; // max 5% of capacity per bag
                _bags = new AgeBag[_size];
                for (int loop = _size - 1; loop >= 0; --loop)
                {
                    _bags[loop] = new AgeBag();
                }
                this.OpenCurrentBag(DateTime.Now, 0);
            }
            #endregion

            #region Methods
            /// <summary>ready a new current AgeBag for use and close the previous one</summary>
            private void OpenCurrentBag(DateTime now, int bagNumber)
            {
                lock (this)
                {
                    // close last age bag
                    if (_currentBag != null)
                    {
                        _currentBag.StopTime = now;
                    }
                    // open new age bag for next time slice
                    AgeBag currentBag = _bags[(_current = bagNumber) % _size];
                    currentBag.StartTime = now;
                    currentBag.First = null;
                    _currentBag = currentBag;
                    // reset counters for CheckValid()
                    _nextValidCheck = now.Add(_timeSlice);
                    _currentSize = 0;
                }
            }

            private void CheckIndexValid()
            {
                // if indexes are getting too big its time to rebuild them
                if (Owner._totalCount - Owner._curCount > Owner._capacity)
                {
                    lock (Owner._indexList)
                    {
                        foreach (var index in Owner._indexList)
                        {
                            Owner._curCount = index.RebuildIndex();
                        }
                    }
                    Owner._totalCount = Owner._curCount;
                }
            }

            public Node Add(object value)
            {
                var node = new Node();
                node.Value = value;
                Interlocked.Increment(ref Owner._curCount);
                this.Touch(node);
                return node;
            }

            /// <summary>Updates the status of the node to prevent it from being dropped from cache</summary>
            public void Touch(Node node)
            {
                if (node.Value != null && node.Bag != _currentBag)
                {
                    if (node.Bag == null)
                    {
                        lock (this)
                        {
                            if (node.Bag == null)
                            {
                                // if node.AgeBag==null then the object is not currently managed by LifespanMgr so add it
                                node.Next = _currentBag.First;
                                _currentBag.First = node;
                                Interlocked.Increment(ref Owner._curCount);
                            }
                        }
                    }
                    node.Bag = _currentBag;
                    Interlocked.Increment(ref _currentSize);
                }
                this.CheckValid();
            }

            /// <summary>Removes the object from node, thereby removing it from all indexes and allows it to be garbage collected</summary>
            public void Remove(Node node)
            {
                if (node.Bag != null && node.Value != null)
                {
                    Interlocked.Decrement(ref Owner._curCount);
                }
                node.Value = null;
                node.Bag = null;
            }

            /// <summary>checks to see if cache is still valid and if LifespanMgr needs to do maintenance</summary>
            public void CheckValid()
            {
                DateTime now = DateTime.Now;
                // Note: Monitor.Enter(this) / Monitor.Exit(this) is the same as lock(this)... We are using Monitor.TryEnter() because it
                // does not wait for a lock, if lock is currently held then skip and let next Touch perform cleanup.
                if ((_currentSize > _bagItemLimit || now > _nextValidCheck) && Monitor.TryEnter(this))
                {
                    try
                    {
                        if ((_currentSize > _bagItemLimit || now > _nextValidCheck))
                        {
                            // if cache is no longer valid throw contents away and start over, else cleanup old items
                            if (_current > 1000000 || (Owner._isValid != null && !Owner._isValid()))
                            {
                                Owner.Clear();
                            }
                            else
                            {
                                this.CleanUp(now);
                            }
                        }
                    }
                    finally
                    {
                        Monitor.Exit(this);
                    }
                }
            }

            /// <summary>remove old items or items beyond capacity from LifespanMgr allowing them to be garbage collected</summary>
            /// <remarks>since we do not physically move items when touched we must check items in bag to determine if they should be deleted 
            /// or moved.  Also items that were removed by setting value to null get removed now.  Rremoving an item from LifespanMgr allows 
            /// it to be garbage collected.  If removed item is retrieved by index prior to GC then it will be readded to LifespanMgr.</remarks>
            public void CleanUp(DateTime now)
            {
                lock (this)
                {
                    //calculate how many items should be removed
                    DateTime maxAge = now.Subtract(_maxAge);
                    DateTime minAge = now.Subtract(_minAge);
                    int itemsToRemove = Owner._curCount - Owner._capacity;
                    AgeBag bag = _bags[_oldest % _size];
                    while (_current != _oldest &&
                        (_current - _oldest > _size - 5 || bag.StartTime < maxAge
                        || (itemsToRemove > 0 && bag.StopTime > minAge)))
                    {
                        // cache is still too big / old so remove oldest bag
                        Node node = bag.First;
                        bag.First = null;
                        while (node != null)
                        {
                            Node next = node.Next;
                            node.Next = null;
                            if (node.Value != null && node.Bag != null)
                            {
                                if (node.Bag == bag)
                                {
                                    // item has not been touched since bag was closed, so remove it from LifespanMgr
                                    ++itemsToRemove;
                                    node.Bag = null;
                                    Interlocked.Decrement(ref Owner._curCount);
                                }
                                else
                                {
                                    // item has been touched and should be moved to correct age bag now
                                    node.Next = node.Bag.First;
                                    node.Bag.First = node;
                                }
                            }
                            node = next;
                        }
                        // increment oldest bag
                        bag = _bags[(++_oldest) % _size];
                    }
                    this.OpenCurrentBag(now, ++_current);
                    this.CheckIndexValid();
                }
            }

            /// <summary>Remove all items from LifespanMgr and reset</summary>
            public void Clear()
            {
                lock (this)
                {
                    foreach (AgeBag bag in _bags)
                    {
                        Node node = bag.First;
                        bag.First = null;
                        while (node != null)
                        {
                            Node next = node.Next;
                            node.Next = null;
                            node.Bag = null;
                            node = next;
                        }
                    }
                    // reset item counters 
                    Owner._curCount = Owner._totalCount = 0;
                    // reset age bags
                    this.OpenCurrentBag(DateTime.Now, _oldest = 0);
                }
            }

            /// <summary>Create item enumerator</summary>
            public IEnumerator<Node> GetEnumerator()
            {
                lock (this)
                {
                    for (int bagNumber = _current; bagNumber >= _oldest; --bagNumber)
                    {
                        AgeBag bag = _bags[bagNumber];
                        // if bag.first == null then bag is empty or being cleaned up, so skip it!
                        for (Node node = bag.First; node != null && bag.First != null; node = node.Next)
                        {
                            if (node.Value != null)
                            {
                                yield return node;
                            }
                        }
                    }
                }
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
            #endregion
        }

        /// <summary>
        /// container class used to hold nodes added within a descrete timeframe
        /// </summary>
        private class AgeBag
        {
            public DateTime StartTime;
            public DateTime StopTime;
            public Node First;
        }

        /// <summary>
        /// LRUNodes is a linked list of items
        /// </summary>
        private class Node
        {
            public object Value;
            public AgeBag Bag;
            public Node Next;
        }
        #endregion
        #endregion

        #region Fields
        protected Func<bool> _isValid;
        private readonly LifespanMgr _lifeSpan;
        private List<IIndex> _indexList;
        private int _capacity, _curCount, _totalCount;
        #endregion

        #region Properties
        public int Capacity
        {
            get { return _capacity; }
        }
        public int Count
        {
            get { return _curCount; }
        }
        #endregion

        #region Constructor
        /// <summary>Constructor</summary>
        /// <param name="capacity">the normal item limit for cache (Count may exeed capacity due to minAge)</param>
        /// <param name="minAge">the minimium time after an access before an item becomes eligible for removal, during this time
        /// the item is protected and will not be removed from cache even if over capacity</param>
        /// <param name="maxAge">the max time that an object will sit in the cache without being accessed, before being removed</param>
        /// <param name="isValid">delegate used to determine if cache is out of date.  Called before index access not more than once per 10 seconds</param>
        public LRUCache(int capacity, TimeSpan minAge, TimeSpan maxAge, Func<bool> isValid = null)
        {
            _capacity = capacity;
            _lifeSpan = new LifespanMgr(this, minAge, maxAge);
            _indexList = new List<IIndex>();
            _isValid = isValid;
        }
        protected override void DisposeInternal(bool disposing)
        {
            if (disposing)
            {
                _indexList.Clear();
            }
            _indexList = null;
            _lifeSpan.Owner = null;
        }
        #endregion

        #region IIndex
        /// <summary>Add a new index to the cache</summary>
        /// <typeparam name="TKey">the type of the key value</typeparam>
        /// <param name="indexName">the name to be associated with this list</param>
        /// <param name="getKey">delegate to get key from object</param>
        /// <param name="loadItem">delegate to load object if it is not found in index</param>
        /// <returns>the newly created index</returns>
        public IIndex<TKey, TItem> CreateIndex<TKey, TItem>(string indexName, Func<TItem, TKey> getKey, Func<TKey, TItem> loadItem = null) where TItem : class
        {
            base.CheckDisposed();

            lock (_indexList)
            {
                int i = _indexList.FindIndex(t => t.IndexName == indexName);
                if (i != -1)
                {
                    _indexList.RemoveAt(i);
                }
                var index = new Index<TKey, TItem>(this, indexName, getKey, loadItem);
                _indexList.Add(index);
                return index;
            }
        }

        /// <summary>Retrieve a index by name</summary>
        public IIndex<TKey, TItem> GetIndex<TKey, TItem>(string indexName)
        {
            base.CheckDisposed();

            lock (_indexList)
            {
                var index = _indexList.Find(t => t.IndexName == indexName) as IIndex<TKey, TItem>;
                if (index == null)
                {
                    throw new ArgumentException("indexName");
                }
                return index;
            }
        }

        /// <summary>Retrieve a object by index name / key</summary>
        public TItem GetValue<TKey, TItem>(string indexName, TKey key)
        {
            base.CheckDisposed();

            return this.GetIndex<TKey, TItem>(indexName)[key];
        }
        #endregion

        #region Methods
        /// <summary>Add an item to the cache (not needed if accessed by index)</summary>
        public void Add(object value)
        {
            base.CheckDisposed();
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            this.AddAndGetNode(value);
        }
        /// <summary>Add an item to the cache</summary>
        private Node AddAndGetNode(object value)
        {
            if (value == null)
            {
                return null;
            }
            // see if item is already in index
            Node node = null;
            IIndex[] indexs;
            lock (_indexList)
            {
                indexs = _indexList.ToArray();
            }
            foreach (var index in indexs)
            {
                if ((node = index.GetNode(value)) != null)
                {
                    break;
                }
            }
            // dupl is used to prevent total count from growing when item is already in indexes (only new Nodes)
            bool isDupl = (node != null && node.Value == value);
            if (!isDupl)
            {
                node = _lifeSpan.Add(value);
            }
            // make sure node gets inserted into all indexes
            foreach (var index in indexs)
            {
                if (!index.AddNode(node))
                {
                    isDupl = true;
                }
            }
            if (!isDupl)
            {
                Interlocked.Increment(ref _totalCount);
            }
            return node;
        }

        public void Remove(object value)
        {
            base.CheckDisposed();
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            Node node = null;
            lock (_indexList)
            {
                foreach (var index in _indexList)
                {
                    if ((node = index.GetNode(value)) != null)
                    {
                        break;
                    }
                }
            }
            bool isDupl = (node != null && node.Value == value);
            if (isDupl)
            {
                _lifeSpan.Remove(node);
                Interlocked.Decrement(ref _totalCount);
            }
            _lifeSpan.CheckValid();
        }

        public void Clear()
        {
            base.CheckDisposed();

            lock (_indexList)
            {
                foreach (var index in _indexList)
                {
                    index.ClearIndex();
                }
            }
            _lifeSpan.Clear();
        }
        #endregion
    }
}