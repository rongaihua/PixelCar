//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
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
    /// 采用按列紧密堆叠：每列从最靠近车道的一行往下排，
    /// 所以每列最上面的车一定没有阻挡，整关一定有解。
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
        /// 从后往前放：后放的车排在更靠近车道的位置，先放的车被它挡住，关卡通得了。
        /// </summary>
        /// <param name="cars">待命车辆</param>
        /// <param name="seed">随机种子，保留参数以兼容调用方</param>
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

            var columnTop = new int[cols];
            for (var i = 0; i < cols; ++i)
            {
                columnTop[i] = 0;
            }

            // 倒序放：列表末尾的车排在最上面，最靠近车道
            for (var i = cars.Count - 1; i >= 0; --i)
            {
                var car = cars[i];
                if (car == null)
                {
                    continue;
                }

                var span = MathUtil.Clamp(SpanOf(car.Capacity), 1, 3);
                var col = ShortestColumn(columnTop);
                car.Dir = ECarDir.Up;
                car.Span = span;
                car.Col = col;
                car.Row = columnTop[col];
                columnTop[col] += span;
            }

            var maxTop = 1;
            for (var i = 0; i < cols; ++i)
            {
                maxTop = MathUtil.Max(maxTop, columnTop[i]);
            }

            rows = maxTop;
        }

        /// <summary>
        /// 找当前最空的一列，尽量让各列高度接近。
        /// </summary>
        /// <param name="columnTop">每列已占到的行号</param>
        /// <returns>列号</returns>
        private static int ShortestColumn(int[] columnTop)
        {
            var best = 0;
            for (var i = 1; i < columnTop.Length; ++i)
            {
                if (columnTop[i] < columnTop[best])
                {
                    best = i;
                }
            }

            return best;
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
