using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace InfrastructureService.Model.Site
{
    [Flags]
    public enum PermissionFlags
    {
        [Description("拒绝")]
        None = 0,
        [Description("读取")]
        Read = 1 << 0,
        [Description("写入")]
        Write = 1 << 1,
        [Description("特权")]
        Special = 1 << 2,
        [Description("完全控制")]
        FullControl = Read | Write | Special
    }
}