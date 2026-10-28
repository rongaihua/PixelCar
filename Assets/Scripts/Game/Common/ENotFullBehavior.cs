//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 没装满的车开到出口后的处理方式
//---------------------------------------------------------------------------------------

namespace PixelCar.Common
{
    /// <summary>
    /// 没装满的车开到出口后的处理方式
    /// </summary>
    public enum ENotFullBehavior
    {
        /// <summary>
        /// 从右边开出屏幕，再从左边入口驶入
        /// </summary>
        Loop = 0,

        /// <summary>
        /// 绕一圈没装到任何格子就退回待命区，装到过就继续绕
        /// </summary>
        LoopOrReturn = 1,

        /// <summary>
        /// 退回待命区
        /// </summary>
        ReturnToPool = 2,

        /// <summary>
        /// 停在出口占着车位（会死局，仅供对比）
        /// </summary>
        Park = 3,
    }
}
