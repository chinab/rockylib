using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace System.Net.WCF
{
    [DataContract]
    public class InvokeFaultDetail
    {
        [DataMember]
        public Guid LogID { get; set; }
        [DataMember]
        public InvokeFaultLevel FaultLevel { get; set; }
        [DataMember]
        public ExceptionDetail Exception { get; set; }
    }

    public enum InvokeFaultLevel
    {
        /// <summary>
        /// 用户操作预料异常 
        /// </summary>
        OperationException,
        /// <summary>
        /// 系统非预料异常
        /// </summary>
        SystemUnusual
    }
}