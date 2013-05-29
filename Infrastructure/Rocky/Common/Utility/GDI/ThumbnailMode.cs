using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rocky
{
    public enum ThumbnailMode
    {
        /// <summary>
        /// 按指定大小缩放（可能变形）
        /// </summary>
        Default,
        /// <summary>
        /// 宽高等比例缩放
        /// </summary>
        Zoom,
        /// <summary>
        /// 宽按比例缩放
        /// </summary>
        ZoomWidth,
        /// <summary>
        /// 高按比例缩放
        /// </summary>
        ZoomHeight,
        /// <summary>
        /// 宽高裁减（不变形）
        /// </summary>
        Cut
    }
}