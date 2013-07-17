using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Caching;
using System.Collections.ObjectModel;

namespace System.Caching
{
    public class CacheChangeMonitor : CacheEntryChangeMonitor
    {
        private ReadOnlyCollection<string> _keys;
        private string _regionName;
        private Guid _uniqueID;
        private DateTimeOffset _lastModified;

        public override ReadOnlyCollection<string> CacheKeys
        {
            get { return _keys; }
        }
        public override DateTimeOffset LastModified
        {
            get { return _lastModified; }
        }
        public override string RegionName
        {
            get { return _regionName; }
        }
        public override string UniqueId
        {
            get { return _uniqueID.ToString("N"); }
        }

        public CacheChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            _keys = keys.ToList().AsReadOnly();
            _regionName = regionName;
            _uniqueID = Guid.NewGuid();
            _lastModified = DateTimeOffset.MinValue;
        }
        protected override void Dispose(bool disposing)
        {
            //if (disposing)
            //{

            //}
            _keys = null;
        }
    }
}