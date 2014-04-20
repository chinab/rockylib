using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;

namespace System.Net
{
    /// <summary>
    /// IHttpClient
    /// </summary>
    [ContractClass(typeof(IHttpClientContract))]
    public interface IHttpClient
    {
        /// <summary>
        /// 超时时间
        /// </summary>
        int SendReceiveTimeout { get; set; }
        /// <summary>
        /// 响应异常失败重试次数
        /// </summary>
        ushort? RetryCount { get; set; }
        /// <summary>
        /// 每次失败重试时的等待时间
        /// </summary>
        TimeSpan? RetryWaitDuration { get; set; }
        /// <summary>
        /// 是否保存Cookie
        /// </summary>
        bool UseCookies { get; set; }
        /// <summary>
        /// Cookie持久对象
        /// </summary>
        CookieContainer CookieContainer { get; }
        /// <summary>
        /// 保存下载文件的文件夹
        /// </summary>
        string SaveFileDirectory { get; set; }

        void SetProxy(EndPoint address, NetworkCredential credential = null);
        /// <summary>
        /// 获取响应文本
        /// </summary>
        /// <param name="requestUrl">url</param>
        /// <param name="content">post data</param>
        /// <returns></returns>
        string GetHtml(Uri requestUrl, HttpRequestContent content = null);
        /// <summary>
        /// 获取响应数据流
        /// </summary>
        /// <param name="requestUrl">url</param>
        /// <param name="content">post data</param>
        /// <returns></returns>
        Stream GetStream(Uri requestUrl, HttpRequestContent content = null);
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">fileUrl</param>
        /// <param name="fileName">保存到本地的文件名（动态生成）</param>
        /// <exception cref="System.Net.DownloadException"></exception>
        void DownloadFile(Uri fileUrl, out string fileName);
    }

    [ContractClassFor(typeof(IHttpClient))]
    internal abstract class IHttpClientContract : IHttpClient
    {
        int IHttpClient.SendReceiveTimeout
        {
            get { return default(int); }
            set { }
        }
        ushort? IHttpClient.RetryCount
        {
            get { return default(ushort?); }
            set { }
        }
        TimeSpan? IHttpClient.RetryWaitDuration
        {
            get { return default(TimeSpan?); }
            set { }
        }
        bool IHttpClient.UseCookies
        {
            get { return default(bool); }
            set { }
        }
        CookieContainer IHttpClient.CookieContainer
        {
            get { return default(CookieContainer); }
        }
        string IHttpClient.SaveFileDirectory
        {
            get { return default(string); }
            set { }
        }

        void IHttpClient.SetProxy(EndPoint address, NetworkCredential credential)
        {
            Contract.Requires(address != null);
        }

        string IHttpClient.GetHtml(Uri requestUrl, HttpRequestContent content)
        {
            Contract.Requires(requestUrl != null);
            Contract.Ensures(Contract.Result<string>() != null);
            return default(string);
        }

        Stream IHttpClient.GetStream(Uri requestUrl, HttpRequestContent content)
        {
            Contract.Requires(requestUrl != null);
            Contract.Ensures(Contract.Result<Stream>() != null);
            return default(Stream);
        }

        void IHttpClient.DownloadFile(Uri fileUrl, out string fileName)
        {
            Contract.Requires(fileUrl != null);
            fileName = default(string);
        }
    }
}