//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 通用数学工具（不依赖 UnityEngine）
//---------------------------------------------------------------------------------------

using System;

namespace PixelCar.Common
{
    /// <summary>
    /// 通用数学工具
    /// </summary>
    public static class MathUtil
    {
        /// <summary>
        /// 取较小值
        /// </summary>
        /// <param name="a">值 a</param>
        /// <param name="b">值 b</param>
        /// <returns>较小值</returns>
        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        /// <summary>
        /// 取较大值
        /// </summary>
        /// <param name="a">值 a</param>
        /// <param name="b">值 b</param>
        /// <returns>较大值</returns>
        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        /// <summary>
        /// 取较大的浮点值
        /// </summary>
        /// <param name="a">值 a</param>
        /// <param name="b">值 b</param>
        /// <returns>较大值</returns>
        public static float MaxFloat(float a, float b)
        {
            return a > b ? a : b;
        }

        /// <summary>
        /// 取较小的浮点值
        /// </summary>
        /// <param name="a">值 a</param>
        /// <param name="b">值 b</param>
        /// <returns>较小值</returns>
        public static float MinFloat(float a, float b)
        {
            return a < b ? a : b;
        }

        /// <summary>
        /// 把整数限制到区间内
        /// </summary>
        /// <param name="value">原值</param>
        /// <param name="min">下限</param>
        /// <param name="max">上限</param>
        /// <returns>限制后的值</returns>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// 把浮点数限制到区间内
        /// </summary>
        /// <param name="value">原值</param>
        /// <param name="min">下限</param>
        /// <param name="max">上限</param>
        /// <returns>限制后的值</returns>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        /// <summary>
        /// 把浮点数限制到 [0,1]
        /// </summary>
        /// <param name="value">原值</param>
        /// <returns>限制后的值</returns>
        public static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// 向下取整
        /// </summary>
        /// <param name="value">原值</param>
        /// <returns>整数</returns>
        public static int FloorToInt(float value)
        {
            return (int)Math.Floor(value);
        }

        /// <summary>
        /// 向上取整
        /// </summary>
        /// <param name="value">原值</param>
        /// <returns>整数</returns>
        public static int CeilToInt(float value)
        {
            return (int)Math.Ceiling(value);
        }

        /// <summary>
        /// 四舍五入
        /// </summary>
        /// <param name="value">原值</param>
        /// <returns>整数</returns>
        public static int RoundToInt(float value)
        {
            return (int)Math.Round(value);
        }
    }
}
