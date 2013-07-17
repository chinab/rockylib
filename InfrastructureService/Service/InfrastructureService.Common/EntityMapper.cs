using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EmitMapper;
using EmitMapper.MappingConfiguration;

namespace InfrastructureService.Common
{
    public static class EntityMapper
    {
        #region Fields
        private static ConcurrentDictionary<string, DefaultMapConfig> _config;
        #endregion

        #region Constructors
        static EntityMapper()
        {
            _config = new ConcurrentDictionary<string, DefaultMapConfig>();
        }
        #endregion

        #region MapMethods
        private static string GetKey(Type tFrom, Type tTo)
        {
            return tFrom.GUID.ToString("N") + tTo.GUID.ToString("N");
        }

        private static DefaultMapConfig GetMapConfig(string key)
        {
            return _config.GetOrAdd(key, k => new DefaultMapConfig());
        }

        public static void SetMembersMatcher<TFrom, TTO>(IDictionary<string, string> membersNameMatcher, Action<TFrom, TTO> postProcessor = null)
        {
            Type tFrom = typeof(TFrom), tTo = typeof(TTO);
            var safeMap = new ConcurrentDictionary<string, string>(membersNameMatcher);
            SetMembersMatcher<TFrom, TTO>((fM, tM) =>
            {
                string val;
                if (safeMap.TryGetValue(fM, out val) && val == tM)
                {
                    return true;
                }
                return fM == tM;
            });
            if (postProcessor != null)
            {
                string key = GetKey(tFrom, tTo);
                var config = GetMapConfig(key);
                config.PostProcess<TTO>((val, state) => { postProcessor((TFrom)state, (TTO)val); return val; });
            }
        }
        public static void SetMembersMatcher<TFrom, TTO>(Func<string, string, bool> membersMatcher)
        {
            Type tFrom = typeof(TFrom), tTo = typeof(TTO);
            string key = GetKey(tFrom, tTo);
            var config = GetMapConfig(key);
            config.MatchMembers(membersMatcher);
        }

        public static void SetIgnoreMembers<TFrom, TTO>(params string[] ignoreNames)
        {
            Type tFrom = typeof(TFrom), tTo = typeof(TTO);
            string key = GetKey(tFrom, tTo);
            var config = GetMapConfig(key);
            config.IgnoreMembers(tFrom, tTo, ignoreNames);
        }
        #endregion

        #region Methods
        public static TTO Map<TFrom, TTO>(TFrom from, TTO to, string[] ignoreNames = null)
        {
            Type tFrom = typeof(TFrom), tTo = typeof(TTO);
            DefaultMapConfig config;
            if (!ignoreNames.IsNullOrEmpty())
            {
                config = new DefaultMapConfig().IgnoreMembers(tFrom, tTo, ignoreNames);
            }
            else
            {
                string key = GetKey(tFrom, tTo);
                config = GetMapConfig(key);
            }
            var mapper = ObjectMapperManager.DefaultInstance.GetMapperImpl(tFrom, tTo, config);
            return (TTO)mapper.Map(from, to, from);
        }
        #endregion
    }
}