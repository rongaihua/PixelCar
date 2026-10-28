//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 客户端入口（创建模块）
//---------------------------------------------------------------------------------------

namespace PixelCar.Client
{
    /// <summary>
    /// 客户端入口
    /// </summary>
    public partial class GameEntry
    {
        /// <summary>
        /// 创建模块
        /// </summary>
        private void OnCreate()
        {
            Main = new MainModule(this);
            Main.Init();
        }
    }
}
