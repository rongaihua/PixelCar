//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 一辆车的运行态数据
//---------------------------------------------------------------------------------------

namespace PixelCar.Common
{
    /// <summary>
    /// 一辆车的运行态数据
    /// </summary>
    public class CarState
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
        /// 这辆车应该装多少格：满车等于 Capacity，尾车小于 Capacity
        /// </summary>
        public int Planned;

        /// <summary>
        /// 已经装了多少格
        /// </summary>
        public int Loaded;

        /// <summary>
        /// 所处阶段
        /// </summary>
        public ECarPhase Phase = ECarPhase.Pool;

        /// <summary>
        /// 车道上的位置（世界 x）
        /// </summary>
        public float X;

        /// <summary>
        /// 正在装色的列，-1 表示没有
        /// </summary>
        public int TargetColumn = -1;

        /// <summary>
        /// 装色节拍计时
        /// </summary>
        public float LoadTimer;

        /// <summary>
        /// 刚被退回待命区，模拟器用它把车从车道里摘出来
        /// </summary>
        public bool Recycled;

        /// <summary>
        /// 上一圈出发时装了多少格，用来判断这一圈有没有白跑
        /// </summary>
        public int LoadedAtLapStart;

        /// <summary>
        /// 是否已装满计划量
        /// </summary>
        public bool IsFull => Loaded >= MathUtil.Min(Capacity, Planned);

        /// <summary>
        /// 装载进度 [0,1]
        /// </summary>
        public float Fill
        {
            get
            {
                var target = MathUtil.Min(Capacity, Planned);
                if (target <= 0)
                {
                    return 0f;
                }

                return MathUtil.Clamp01((float)Loaded / target);
            }
        }
    }
}
