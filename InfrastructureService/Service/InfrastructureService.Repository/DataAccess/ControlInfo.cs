﻿//------------------------------------------------------------------------------
// <auto-generated>
//    姝や唬鐮佹槸鏍规嵁妯℃澘鐢熸垚鐨勩€?//
//    鎵嬪姩鏇存敼姝ゆ枃浠跺彲鑳戒細瀵艰嚧搴旂敤绋嬪簭涓彂鐢熷紓甯歌涓恒€?//    濡傛灉閲嶆柊鐢熸垚浠ｇ爜锛屽垯灏嗚鐩栧姝ゆ枃浠剁殑鎵嬪姩鏇存敼銆?// </auto-generated>
//------------------------------------------------------------------------------

namespace InfrastructureService.Repository.DataAccess
{
    using System;
    using System.Collections.Generic;
    
    public partial class ControlInfo
    {
        public ControlInfo()
        {
            this.RoleControlMaps = new HashSet<RoleControlMap>();
            this.UserControlMaps = new HashSet<UserControlMap>();
        }
    
        public System.Guid RowID { get; set; }
        public System.Guid ComponentID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public int Sort { get; set; }
        public int Status { get; set; }
    
        public virtual ComponentInfo ComponentInfo { get; set; }
        public virtual ICollection<RoleControlMap> RoleControlMaps { get; set; }
        public virtual ICollection<UserControlMap> UserControlMaps { get; set; }
    }
}
