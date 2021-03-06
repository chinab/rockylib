﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace System.Net
{
    /// <summary>
    /// 下载异常
    /// </summary>
    [Serializable]
    public class DownloadException : HttpRequestException
    {
        /// <summary>
        /// 远程下载Url
        /// </summary>
        public Uri RemoteUrl { get; set; }
        /// <summary>
        /// 本地保存Path
        /// </summary>
        public string LocalPath { get; set; }

        public DownloadException() { }
        public DownloadException(string message) : base(message) { }
        public DownloadException(string message, Exception inner) : base(message, inner) { }
    }
}