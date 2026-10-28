//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 可变色块盘（每列一个自底向上的堆栈，只能从最底格取，取完自动塌落）
//---------------------------------------------------------------------------------------

namespace PixelCar.Common
{
    /// <summary>
    /// 可变色块盘
    /// </summary>
    public class BoardState
    {
        // 列内自底向上存：mCells[x * Height + i]，i = 0 是最底格
        private readonly int[] mCells;

        // 每列当前最底格的列内下标
        private readonly int[] mBottom;

        // 每列剩余格数
        private readonly int[] mCount;

        /// <summary>
        /// 横向格数
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// 纵向格数
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// 总格数
        /// </summary>
        public int TotalCells { get; }

        /// <summary>
        /// 每种颜色剩余格数
        /// </summary>
        public int[] Remaining { get; }

        /// <summary>
        /// 总计剩余格数
        /// </summary>
        public int TotalRemaining { get; private set; }

        /// <summary>
        /// 是否已经全部取空
        /// </summary>
        public bool IsCleared => TotalRemaining <= 0;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="level">关卡数据</param>
        /// <param name="paletteSize">调色板颜色数</param>
        public BoardState(PixelBoardLevel level, int paletteSize)
        {
            Width = MathUtil.Max(1, level.Width);
            Height = MathUtil.Max(1, level.Height);
            TotalCells = Width * Height;

            mCells = new int[Width * Height];
            mBottom = new int[Width];
            mCount = new int[Width];
            Remaining = new int[MathUtil.Max(1, paletteSize)];

            for (var x = 0; x < Width; ++x)
            {
                var count = 0;
                for (var y = 0; y < Height; ++y)
                {
                    var index = y * Width + x;
                    if (level.Cells == null || index >= level.Cells.Length)
                    {
                        continue;
                    }

                    var color = level.Cells[index];
                    if (color < 0 || color >= Remaining.Length)
                    {
                        // 空格直接压掉，视觉上表现为上方色块落下
                        continue;
                    }

                    mCells[x * Height + count] = color;
                    ++count;
                    ++Remaining[color];
                    ++TotalRemaining;
                }

                mCount[x] = count;
            }
        }

        /// <summary>
        /// 取某列剩余格数
        /// </summary>
        /// <param name="x">列号</param>
        /// <returns>格数</returns>
        public int ColumnCount(int x)
        {
            if (x < 0 || x >= Width)
            {
                return 0;
            }

            return mCount[x];
        }

        /// <summary>
        /// 取某列最底格的颜色，列空返回 -1
        /// </summary>
        /// <param name="x">列号</param>
        /// <returns>颜色索引</returns>
        public int BottomColor(int x)
        {
            if (x < 0 || x >= Width || mCount[x] <= 0)
            {
                return -1;
            }

            return mCells[x * Height + mBottom[x]];
        }

        /// <summary>
        /// 取某列从底往上第 row 格的颜色，越界返回 -1
        /// </summary>
        /// <param name="x">列号</param>
        /// <param name="rowFromBottom">从底往上第几格</param>
        /// <returns>颜色索引</returns>
        public int CellAt(int x, int rowFromBottom)
        {
            if (x < 0 || x >= Width || rowFromBottom < 0 || rowFromBottom >= mCount[x])
            {
                return -1;
            }

            return mCells[x * Height + mBottom[x] + rowFromBottom];
        }

        /// <summary>
        /// 把某列最底格取走，颜色不匹配或列空返回 false
        /// </summary>
        /// <param name="x">列号</param>
        /// <param name="color">要取的颜色</param>
        /// <returns>是否取到</returns>
        public bool TryTake(int x, int color)
        {
            if (x < 0 || x >= Width || mCount[x] <= 0)
            {
                return false;
            }

            var bottom = mBottom[x];
            if (mCells[x * Height + bottom] != color)
            {
                return false;
            }

            mBottom[x] = bottom + 1;
            --mCount[x];
            --Remaining[color];
            --TotalRemaining;
            return true;
        }

        /// <summary>
        /// 某色是否已经取完
        /// </summary>
        /// <param name="color">颜色索引</param>
        /// <returns>是否取完</returns>
        public bool ColorCleared(int color)
        {
            if (color < 0 || color >= Remaining.Length)
            {
                return true;
            }

            return Remaining[color] <= 0;
        }
    }
}
