//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 采样方式
//---------------------------------------------------------------------------------------

namespace PixelCar.Editor
{
    /// <summary>
    /// 采样方式
    /// </summary>
    public enum ESampleMode
    {
        /// <summary>
        /// 区域内平均色，画面过渡自然，适合照片
        /// </summary>
        Average = 0,

        /// <summary>
        /// 区域中心像素，最锐利，适合本来就像素化的素材
        /// </summary>
        Center = 1,

        /// <summary>
        /// 区域内出现最多的颜色，能保住色块边缘
        /// </summary>
        Dominant = 2,
    }
}
