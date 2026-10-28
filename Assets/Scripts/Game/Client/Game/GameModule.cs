//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 模块基类
//---------------------------------------------------------------------------------------

namespace PixelCar.Client
{
    /// <summary>
    /// 模块基类
    /// </summary>
    public abstract class GameModule
    {
        /// <summary>
        /// 客户端入口
        /// </summary>
        protected GameEntry Game { get; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="game">客户端入口</param>
        protected GameModule(GameEntry game)
        {
            Game = game;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Init()
        {
        }

        /// <summary>
        /// 每帧推进
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        public virtual void Tick(float deltaTime)
        {
        }

        /// <summary>
        /// 释放
        /// </summary>
        public virtual void Dispose()
        {
        }
    }
}
