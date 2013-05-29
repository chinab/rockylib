using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Rocky.Data
{
    internal class DbLogger : TextWriter
    {
        private log4net.ILog _logger;

        public DbLogger()
        {
            _logger = log4net.LogManager.GetLogger(this.GetType());
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }

        public override void Write(string value)
        {
            _logger.Debug(value);
        }
        public override void Write(char[] buffer, int index, int count)
        {
            Write(new string(buffer, index, count));
        }
    }
}