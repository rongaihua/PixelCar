//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 车辆所处阶段
//---------------------------------------------------------------------------------------

namespace PixelCar.Common
{
    /// <summary>
    /// 车辆所处阶段
    /// </summary>
    public enum ECarPhase
    {
        /// <summary>
        /// 还在下方待命区
        /// </summary>
        Pool = 0,

        /// <summary>
        /// 在车道上行驶，寻找能装的颜色
        /// </summary>
        Driving = 1,

        /// <summary>
        /// 停在某列下方装色
        /// </summary>
        Loading = 2,

        /// <summary>
        /// 装完开向出口
        /// </summary>
        Exiting = 3,

        /// <summary>
        /// 开到出口却没装满，停在车道上
        /// </summary>
        Parked = 4,

        /// <summary>
        /// 已离场，等待回收
        /// </summary>
        Done = 5,
    }
}
