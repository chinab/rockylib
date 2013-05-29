using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InfrastructureService.Common
{
    [Serializable]
    public class DomainException : Exception
    {
        public DomainExceptionLevel ExceptionLevel { get; set; }
        public int ErrorCode { get; set; }

        public DomainException() { }
        public DomainException(string message) : base(message) { }
        public DomainException(string message, Exception inner) : base(message, inner) { }
        protected DomainException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    public enum DomainExceptionLevel
    {
        /// <summary>
        /// 用户操作预料中异常 
        /// </summary>
        OperationException,
        /// <summary>
        /// 系统非预料异常
        /// </summary>
        SystemUnusual
    }
}