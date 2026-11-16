//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-29
// Desc: 把车按朝向和车长塞进停车场
//---------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace PixelCar.Common
{
    /// <summary>
    /// 停车场排布。车头格子写在 <see cref="CarState.Col"/> / <see cref="CarState.Row"/>，
    /// 车身沿朝向的反方向占 <see cref="CarState.Span"/> 格。
    ///
    /// 按带横排：每带最多 <see cref="LotCols"/> 辆，都朝上，车头贴着带顶，
    /// 所以每带最上面那一排车一定没有阻挡。带内颜色混着排，见 <see cref="Pack"/>。
    /// </summary>
    public static class LotPacker
    {
        private const int LotCols = 8;

        /// <summary>
        /// 按载量给车长：小车 2 格，中车和大巴 3 格。
        /// </summary>
        /// <param name="capacity">额定载量</param>
        /// <returns>占用格数</returns>
        public static int SpanOf(int capacity)
        {
            if (capacity <= 60)
            {
                return 2;
            }

            return 3;
        }

        /// <summary>
        /// 写出一辆车占的格子下标。越界时返回 -1。
        /// </summary>
        /// <param name="noseCol">车头列</param>
        /// <param name="noseRow">车头行</param>
        /// <param name="span">车长</param>
        /// <param name="dir">朝向</param>
        /// <param name="cols">停车场列数</param>
        /// <param name="rows">停车场行数</param>
        /// <param name="indices">接收下标的缓冲，长度至少为车长</param>
        /// <returns>占用格数。放不下时为 -1</returns>
        public static int Collect(int noseCol, int noseRow, int span, ECarDir dir, int cols, int rows, int[] indices)
        {
            if (indices == null || span <= 0 || span > indices.Length)
            {
                return -1;
            }

            for (var i = 0; i < span; ++i)
            {
                int col;
                int row;
                Step(noseCol, noseRow, dir, i, out col, out row);
                if (col < 0 || row < 0 || col >= cols || row >= rows)
                {
                    return -1;
                }

                indices[i] = row * cols + col;
            }

            return span;
        }

        /// <summary>
        /// 车头前方一直到停车场边界有没有别的车。
        /// </summary>
        /// <param name="noseCol">车头列</param>
        /// <param name="noseRow">车头行</param>
        /// <param name="dir">朝向</param>
        /// <param name="cols">列数</param>
        /// <param name="rows">行数</param>
        /// <param name="occupancy">占位表，空格为空</param>
        /// <returns>被挡住时为 true</returns>
        public static bool PathBlocked(int noseCol, int noseRow, ECarDir dir, int cols, int rows, CarState[] occupancy)
        {
            if (occupancy == null)
            {
                return false;
            }

            switch (dir)
            {
                case ECarDir.Up:
                    for (var row = noseRow - 1; row >= 0; --row)
                    {
                        if (occupancy[row * cols + noseCol] != null)
                        {
                            return true;
                        }
                    }

                    return false;

                case ECarDir.Down:
                    for (var row = noseRow + 1; row < rows; ++row)
                    {
                        if (occupancy[row * cols + noseCol] != null)
                        {
                            return true;
                        }
                    }

                    return false;

                case ECarDir.Left:
                    for (var col = noseCol - 1; col >= 0; --col)
                    {
                        if (occupancy[noseRow * cols + col] != null)
                        {
                            return true;
                        }
                    }

                    return false;

                default:
                    for (var col = noseCol + 1; col < cols; ++col)
                    {
                        if (occupancy[noseRow * cols + col] != null)
                        {
                            return true;
                        }
                    }

                    return false;
            }
        }

        /// <summary>
        /// 排布全部车辆。结果写回每辆车的朝向、车头格子和车长。
        ///
        /// 顺序做两道处理：
        /// 1. 按颜色轮转重排（<see cref="DealByColor"/>），让每一带都尽量凑齐各色。
        ///    关卡数据里同色的车是连着的，直接分带会变成一带全同色，
        ///    盘面底层的另一种颜色一辆都拉不到，一开局就是死局；
        /// 2. 每一带内部按 seed 打乱列顺序，避免每次都排成同一串颜色。
        /// </summary>
        /// <param name="cars">待命车辆</param>
        /// <param name="seed">随机种子，决定每带内的列顺序</param>
        /// <param name="cols">停车场列数</param>
        /// <param name="rows">停车场行数</param>
        public static void Pack(IList<CarState> cars, int seed, out int cols, out int rows)
        {
            cols = LotCols;
            rows = 1;
            if (cars == null || cars.Count == 0)
            {
                return;
            }

            var ordered = DealByColor(cars);
            var random = seed == 0 ? 0x9E3779B9u : (uint)seed;

            // 按“带”横排：一带最多 cols 辆，带高取这一带里最长的车，横向填满再换带
            var row = 0;
            var index = 0;
            while (index < ordered.Count)
            {
                var count = MathUtil.Min(cols, ordered.Count - index);

                // 这一带内部的列顺序打乱
                for (var i = count - 1; i > 0; --i)
                {
                    var j = (int)(NextRandom(ref random) % (uint)(i + 1));
                    var swap = ordered[index + i];
                    ordered[index + i] = ordered[index + j];
                    ordered[index + j] = swap;
                }

                var bandSpan = 1;
                for (var i = 0; i < count; ++i)
                {
                    var car = ordered[index + i];
                    car.Span = MathUtil.Clamp(SpanOf(car.Capacity), 1, 3);
                    bandSpan = MathUtil.Max(bandSpan, car.Span);
                }

                for (var i = 0; i < count; ++i)
                {
                    var car = ordered[index + i];
                    car.Dir = ECarDir.Up;
                    car.Col = i;
                    car.Row = row;
                }

                row += bandSpan;
                index += count;
            }

            rows = MathUtil.Max(1, row);
        }

        /// <summary>
        /// 按颜色轮转排队：按色号取车，一轮一种颜色，同色的车因此被摊到各带，
        /// 一带里颜色尽量齐。结果与传入顺序无关，只跟每色几辆有关。
        /// </summary>
        /// <param name="cars">待命车辆</param>
        /// <returns>重排后的列表</returns>
        private static List<CarState> DealByColor(IList<CarState> cars)
        {
            var colors = new List<int>();
            for (var i = 0; i < cars.Count; ++i)
            {
                var car = cars[i];
                if (car == null)
                {
                    continue;
                }

                if (!colors.Contains(car.Color))
                {
                    colors.Add(car.Color);
                }
            }

            colors.Sort();

            var buckets = new List<CarState>[colors.Count];
            for (var i = 0; i < buckets.Length; ++i)
            {
                buckets[i] = new List<CarState>();
            }

            for (var i = 0; i < cars.Count; ++i)
            {
                var car = cars[i];
                if (car != null)
                {
                    buckets[colors.IndexOf(car.Color)].Add(car);
                }
            }

            var ordered = new List<CarState>(cars.Count);
            var longest = 0;
            foreach (var bucket in buckets)
            {
                longest = MathUtil.Max(longest, bucket.Count);
            }

            for (var index = 0; index < longest; ++index)
            {
                foreach (var bucket in buckets)
                {
                    if (index < bucket.Count)
                    {
                        ordered.Add(bucket[index]);
                    }
                }
            }

            return ordered;
        }

        /// <summary>
        /// xorshift 随机数，只用来打乱列顺序，不依赖 System.Random 的具体实现
        /// </summary>
        /// <param name="state">状态</param>
        /// <returns>下一个值</returns>
        private static uint NextRandom(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return state;
        }

        /// <summary>
        /// 沿车尾方向走第 i 格。i = 0 是车头。
        /// </summary>
        /// <param name="noseCol">车头列</param>
        /// <param name="noseRow">车头行</param>
        /// <param name="dir">朝向</param>
        /// <param name="step">离车头几格</param>
        /// <param name="col">目标列</param>
        /// <param name="row">目标行</param>
        private static void Step(int noseCol, int noseRow, ECarDir dir, int step, out int col, out int row)
        {
            col = noseCol;
            row = noseRow;
            switch (dir)
            {
                case ECarDir.Up:
                    row = noseRow + step;
                    return;
                case ECarDir.Down:
                    row = noseRow - step;
                    return;
                case ECarDir.Left:
                    col = noseCol + step;
                    return;
                default:
                    col = noseCol - step;
                    return;
            }
        }
    }
}
