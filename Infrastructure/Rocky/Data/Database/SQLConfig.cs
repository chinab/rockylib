/*********************************************************************************
** File Name	:	SQLConfig
** Copyright (C) 2013 guzhen.net. All Rights Reserved.
** Creator		:	SONGGUO\wangxiaoming
** Create Date	:	2013/1/23 10:47:36
** Update Date	:	
** Description	:	
** Version No	:	
*********************************************************************************/
using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Reflection;
using System.Configuration;
using System.Runtime.Caching;
using System.IO;

namespace Rocky.Data
{
    /// <summary>
    /// 配置映射
    /// </summary>
    internal class SQLConfig
    {
        private ObjectCache _cache;
        private string _configPath;

        /// <summary>
        /// Constractor
        /// </summary>
        /// <param name="configPath">配置文件的路径</param>
        public SQLConfig(string configPath = null)
        {
            if (configPath == null)
            {
                configPath = AppDomain.CurrentDomain.BaseDirectory + @"\DbSetting\";
            }

            _cache = new MemoryCache(this.GetType().FullName);
            _configPath = configPath;
        }

        private void GenerateKey(MethodBase method, out string key)
        {
            key = method.DeclaringType.Name + "." + method.Name;
        }

        private bool TryFindText(string key, out string text, out string configPath)
        {
            configPath = text = null;
            foreach (string filePath in Directory.EnumerateFiles(_configPath, "*.config"))
            {
                var map = new ExeConfigurationFileMap();
                map.ExeConfigFilename = filePath;
                var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
                var pair = config.AppSettings.Settings[key];
                if (pair != null)
                {
                    text = pair.Value;
                    configPath = filePath;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取调用的方法映射的SQL语句
        /// </summary>
        /// <param name="method">调用的方法</param>
        /// <returns>SQL语句</returns>
        /// <exception cref="Guzhen.Common.DbLiteException"></exception>
        public string GetSQL(MethodBase method)
        {
            string key;
            this.GenerateKey(method, out key);
            string sql = (string)_cache[key], configPath;
            if (sql == null)
            {
                if (!this.TryFindText(key, out sql, out configPath))
                {
                    throw new InvalidOperationException(string.Format("没有配置{0}该项", key));
                }
                var policy = new CacheItemPolicy()
                {
                    AbsoluteExpiration = ObjectCache.InfiniteAbsoluteExpiration,
                    //相对过期时间
                    SlidingExpiration = TimeSpan.FromMinutes(10D),
                };
                //监控配置文件变更
                try
                {
                    policy.ChangeMonitors.Add(new HostFileChangeMonitor(new List<string>() { configPath }));
                }
                catch (Exception ex)
                {
                    Runtime.LogError(ex, string.Format("ChangeMonitor:{0}", ex.Message));
                }
                _cache.Add(key, sql, policy);
            }
            return sql;
        }
    }
}