using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Caching;
using EmitMapper;
using EmitMapper.MappingConfiguration;
using EmitMapper.MappingConfiguration.MappingOperations;
using EmitMapper.Utils;

namespace System.Data
{
    public sealed class ObjectChangeTracker
    {
        #region NestedTypes
        private class MappingConfiguration : IMappingConfigurator
        {
            public IMappingOperation[] GetMappingOperations(Type from, Type to)
            {
                return ReflectionUtils
                    .GetPublicFieldsAndProperties(from)
                    .Select(m => new SrcReadOperation
                    {
                        Source = new MemberDescriptor(m),
                        Setter = (obj, value, state) => (state as TrackingMembersList).TrackingMembers.Add(new TrackingMember
                        {
                            Name = m.Name,
                            Value = value
                        })
                    }).ToArray();
            }

            public IRootMappingOperation GetRootMappingOperation(Type from, Type to)
            {
                return null;
            }

            public string GetConfigurationName()
            {
                return "ObjectsTracker";
            }

            public StaticConvertersManager GetStaticConvertersManager()
            {
                return null;
            }
        }

        private class TrackingMembersList
        {
            public object Key;
            public List<TrackingMember> TrackingMembers = new List<TrackingMember>();
        }

        public struct TrackingMember
        {
            public string Name { get; set; }
            public object Value { get; set; }
        }
        #endregion

        #region Fields
        private LRUCache _trackingObjects;
        private string indexRef = string.Empty, indexStr = "extend";
        #endregion

        #region Constractors
        public ObjectChangeTracker()
        {
            _trackingObjects = new LRUCache(1000, TimeSpan.FromMinutes(1), TimeSpan.FromHours(1));
            _trackingObjects.CreateIndex<object, TrackingMembersList>(indexRef, item => item.Key);
            _trackingObjects.CreateIndex<string, TrackingMembersList>(indexStr, item => item.Key.ToString());
        }
        #endregion

        #region Methods
        #region Helper
        private List<TrackingMember> GetObjectMembers(object obj)
        {
            var list = new TrackingMembersList();
            ObjectMapperManager.DefaultInstance.GetMapperImpl(obj.GetType(), null, new MappingConfiguration()).Map(obj, null, list);
            return list.TrackingMembers;
        }

        private TrackingMember[] GetObjectChanges(List<TrackingMember> originalValues, List<TrackingMember> currentValues)
        {
            return currentValues.Where((current, idx) =>
            {
                var original = originalValues[idx];
                return ((original.Value == null) != (current.Value == null))
                    || (original.Value != null && !original.Value.Equals(current.Value));
            }).ToArray();
        }
        #endregion

        public void Register(object originalObj, string objID = null)
        {
            if (originalObj == null)
            {
                throw new ArgumentNullException("originalObj");
            }

            var trackingMembers = this.GetObjectMembers(originalObj);
            if (trackingMembers.Count == 0)
            {
                return;
            }
            _trackingObjects.Add(new TrackingMembersList()
            {
                Key = objID ?? originalObj,
                TrackingMembers = trackingMembers
            });
        }

        public TrackingMember[] GetChanges(object currentObj, string objID = null)
        {
            if (currentObj == null)
            {
                throw new ArgumentNullException("currentObj");
            }

            TrackingMembersList trackingItem;
            if (objID != null)
            {
                trackingItem = _trackingObjects.GetValue<string, TrackingMembersList>(indexStr, objID);
            }
            else
            {
                trackingItem = _trackingObjects.GetValue<object, TrackingMembersList>(indexRef, currentObj);
            }
            if (trackingItem == null)
            {
                return new TrackingMember[0];
            }
            var originalValues = trackingItem.TrackingMembers;
            var currentValues = GetObjectMembers(currentObj);
            return this.GetObjectChanges(originalValues, currentValues);
        }

        public TrackingMember[] GetChanges(object originalObj, object currentObj)
        {
            if (originalObj == null || currentObj == null || originalObj.GetType() != currentObj.GetType())
            {
                throw new ArgumentException("originalObj || currentObj");
            }

            var originalValues = GetObjectMembers(originalObj);
            var currentValues = GetObjectMembers(currentObj);
            return this.GetObjectChanges(originalValues, currentValues);
        }
        #endregion
    }
}