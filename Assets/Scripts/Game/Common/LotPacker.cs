//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-29
// Desc: 把车按朝向和车长塞进停车场，并保证有一条能开出来的顺序
//---------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace PixelCar.Common
{
    /// <summary>
    /// 停车场排布。车头格子写在 <see cref="CarState.Col"/> / <see cref="CarState.Row"/>，
    /// 车身沿朝向的反方向占 <see cref="CarState.Span"/> 格。
    /// 从后往前放：后放的车先能开出去，先放的车会被它挡住，所以关卡一定有解。
    /// </summary>
    public static class LotPacker
    {
        private const int LotCols = 8;

        /// <summary>
        /// 按载量给车长：小车 2 格，中车 3 格，大巴 3 格。
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
        /// <param name="cols">停车场列数</param>
        /// <param name="rows">停车场行数</param>
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
        /// </summary>
        /// <param name="cars">待命车辆</param>
        /// <param name="seed">随机种子，同一关重复开是同一张停车场</param>
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

            var cells = 0;
            for (var i = 0; i < cars.Count; ++i)
            {
                var car = cars[i];
                if (car == null)
                {
                    continue;
                }

                car.Span = SpanOf(car.Capacity);
                cells += car.Span;
            }

            var baseRows = MathUtil.Max(2, (cells + cols - 1) / cols);
            var rngSeed = seed == 0 ? 1 : seed;
            for (var extra = 0; extra < 6; ++extra)
            {
                rows = baseRows + extra;
                if (TryPack(cars, cols, rows, rngSeed + extra * 17))
                {
                    return;
                }
            }

            Fallback(cars, cols, out rows);
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

        /// <summary>
        /// 试着把全部车放进给定网格。放不下返回 false，不保留半成品。
        /// </summary>
        /// <param name="cars">车辆</param>
        /// <param name="cols">列数</param>
        /// <param name="rows">行数</param>
        /// <param name="seed">随机种子</param>
        /// <returns>是否全部放下</returns>
        private static bool TryPack(IList<CarState> cars, int cols, int rows, int seed)
        {
            var occupancy = new CarState[cols * rows];
            var rng = new Rng((uint)seed);
            var buffer = new int[4];

            for (var i = cars.Count - 1; i >= 0; --i)
            {
                var car = cars[i];
                if (car == null)
                {
                    continue;
                }

                if (!PlaceOne(car, occupancy, cols, rows, rng, buffer))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 随机找一个当前就能开出去的空位。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <param name="occupancy">占位表</param>
        /// <param name="cols">列数</param>
        /// <param name="rows">行数</param>
        /// <param name="rng">随机数</param>
        /// <param name="buffer">格子缓冲</param>
        /// <returns>是否放下</returns>
        private static bool PlaceOne(CarState car, CarState[] occupancy, int cols, int rows, Rng rng, int[] buffer)
        {
            var span = MathUtil.Clamp(car.Span, 1, buffer.Length);
            for (var attempt = 0; attempt < 240; ++attempt)
            {
                var dir = (ECarDir)rng.Next(4);
                int noseCol;
                int noseRow;
                switch (dir)
                {
                    case ECarDir.Up:
                        noseCol = rng.Next(cols);
                        noseRow = rng.Next(rows - span + 1);
                        break;
                    case ECarDir.Down:
                        noseCol = rng.Next(cols);
                        noseRow = span - 1 + rng.Next(rows - span + 1);
                        break;
                    case ECarDir.Left:
                        noseCol = rng.Next(cols - span + 1);
                        noseRow = rng.Next(rows);
                        break;
                    default:
                        noseCol = span - 1 + rng.Next(cols - span + 1);
                        noseRow = rng.Next(rows);
                        break;
                }

                var count = Collect(noseCol, noseRow, span, dir, cols, rows, buffer);
                if (count < 0)
                {
                    continue;
                }

                var free = true;
                for (var i = 0; i < count; ++i)
                {
                    if (occupancy[buffer[i]] != null)
                    {
                        free = false;
                        break;
                    }
                }

                if (!free || PathBlocked(noseCol, noseRow, dir, cols, rows, occupancy))
                {
                    continue;
                }

                car.Col = noseCol;
                car.Row = noseRow;
                car.Dir = dir;
                car.Span = span;
                for (var i = 0; i < count; ++i)
                {
                    occupancy[buffer[i]] = car;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 随机放不下时改成朝上的整齐队列，前排能直接开出。
        /// </summary>
        /// <param name="cars">车辆</param>
        /// <param name="cols">列数</param>
        /// <param name="rows">实际用到的行数</param>
        private static void Fallback(IList<CarState> cars, int cols, out int rows)
        {
            var band = 0;
            var col = 0;
            for (var i = 0; i < cars.Count; ++i)
            {
                var car = cars[i];
                if (car == null)
                {
                    continue;
                }

                if (col >= cols)
                {
                    col = 0;
                    band += 3;
                }

                car.Dir = ECarDir.Up;
                car.Span = MathUtil.Clamp(car.Span, 1, 3);
                car.Col = col;
                car.Row = band;
                ++col;
            }

            rows = band + 3;
        }

        /// <summary>
        /// 不分配的小随机数。
        /// </summary>
        private sealed class Rng
        {
            private uint mState;

            /// <summary>
            /// 构造
            /// </summary>
            /// <param name="seed">种子</param>
            public Rng(uint seed)
            {
                mState = seed == 0u ? 1u : seed;
            }

            /// <summary>
            /// 取 [0, count) 的整数
            /// </summary>
            /// <param name="count">上界</param>
            /// <returns>随机值</returns>
            public int Next(int count)
            {
                mState = mState * 1664525u + 1013904223u;
                if (count <= 1)
                {
                    return 0;
                }

                return (int)(mState % (uint)count);
            }
        }
    }
}
