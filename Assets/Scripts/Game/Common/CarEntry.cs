//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 一辆车的数据（颜色 / 额定载量 / 实际装载）
//---------------------------------------------------------------------------------------

using System;

namespace PixelCar.Common
{
    /// <summary>
    /// 一辆车的数据
    /// </summary>
    [Serializable]
    public class CarEntry
    {
        /// <summary>
        /// 调色板索引
        /// </summary>
        public int Color;

        /// <summary>
        /// 额定载量（50 / 100 / 180）
        /// </summary>
        public int Capacity;

        /// <summary>
        /// 实际装载量，满车时等于 Capacity
        /// </summary>
        public int Load;
    }
}
