using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Mime;
using System.Web;

namespace System.Net
{
    public sealed class HttpFile : HttpPostedFileBase
    {
        #region Fields
        private long _offset, _length;
        private Stream _inputStream;
        private string _fileName;
        #endregion

        #region Properties
        /// <summary>
        /// 文件名称字段 (&lt;input type="file" name=InputName /&gt;)
        /// </summary>
        public string InputName { get; set; }
        public override string FileName
        {
            get { return _fileName; }
        }
        public override string ContentType
        {
            get { return MediaTypeNames.Application.Octet; }
        }
        public override int ContentLength
        {
            get
            {
                checked
                {
                    return (int)_length;
                }
            }
        }
        public long StreamOffset
        {
            get { return _offset; }
        }
        public long ContentLength64
        {
            get { return _length; }
        }
        public override Stream InputStream
        {
            get { return _inputStream; }
        }
        #endregion

        #region Constructors
        public HttpFile(string inputName, string filePath, long offset = 0L, long length = -1L)
            : this(inputName, Path.GetFileName(filePath), File.OpenRead(filePath), offset, length)
        {

        }
        public HttpFile(string inputName, string fileName, Stream fileStream, long offset = 0L, long length = -1L)
        {
            Contract.Requires(inputName != null);
            Contract.Requires(!string.IsNullOrEmpty(fileName));
            Contract.Requires(offset >= 0L);
            Contract.Ensures(_offset < _length);
            if (length == -1L)
            {
                length = fileStream.Length;
            }

            this.InputName = inputName;
            _fileName = fileName;
            _inputStream = fileStream;
            _offset = offset;
            _length = length;
        }
        public HttpFile(string inputName, HttpPostedFileBase httpFile)
            : this(inputName, httpFile.FileName, httpFile.InputStream, 0L, httpFile.ContentLength)
        {

        }
        #endregion

        #region Methods
        public override void SaveAs(string filename)
        {
            using (var fileStream = File.OpenWrite(filename))
            {
                _inputStream.Position = _offset;
                _inputStream.SetLength(_length);
                _inputStream.FixedCopyTo(fileStream);
            }
        }
        #endregion
    }
}