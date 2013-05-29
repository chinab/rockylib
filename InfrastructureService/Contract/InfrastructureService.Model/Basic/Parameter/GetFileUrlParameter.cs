using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Drawing;
using Microsoft.Practices.EnterpriseLibrary.Validation.Validators;

namespace InfrastructureService.Model.Basic
{
    [DataContract]
    public class GetFileUrlParameter : HeaderEntity
    {
        [DataMember]
        [StringLengthValidator(32, 32)]
        public string FileKey { get; set; }

        /// <summary>
        /// 如果为图片文件，并且此属性已赋值，则获取生成缩略图后的URL
        /// </summary>
        [DataMember]
        public Size? ImageThumbnailSize { get; set; }
    }
}