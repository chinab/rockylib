﻿//------------------------------------------------------------------------------
// <auto-generated>
//    姝や唬鐮佹槸鏍规嵁妯℃澘鐢熸垚鐨勩€?//
//    鎵嬪姩鏇存敼姝ゆ枃浠跺彲鑳戒細瀵艰嚧搴旂敤绋嬪簭涓彂鐢熷紓甯歌涓恒€?//    濡傛灉閲嶆柊鐢熸垚浠ｇ爜锛屽垯灏嗚鐩栧姝ゆ枃浠剁殑鎵嬪姩鏇存敼銆?// </auto-generated>
//------------------------------------------------------------------------------

namespace InfrastructureService.Repository.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class EmailMessage
    {
        public System.Guid RowID { get; set; }
        public System.Guid AppID { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CC { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public System.DateTime CreateDate { get; set; }
        public Nullable<System.DateTime> SendDate { get; set; }
        public Nullable<System.DateTime> ExpiredDate { get; set; }
        public int Status { get; set; }
    
        public virtual AppInfo AppInfo { get; set; }
    }
}
