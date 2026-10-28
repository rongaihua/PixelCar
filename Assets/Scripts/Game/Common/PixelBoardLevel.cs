//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 一关的色块盘数据（由图片量化而来）
//---------------------------------------------------------------------------------------

using System;

namespace PixelCar.Common
{
    /// <summary>
    /// 一关的色块盘数据
    ///
    /// 坐标系约定：Cells 为行主序，索引 = Y * Width + X；
    /// Y = 0 是盘面最底下一行，游戏从这一层开始取色，取完上面整体下落。
    ///
    /// 数据守恒：所有 CellCounts 之和 + EmptyCount 等于 Width * Height。
    /// </summary>
    [Serializable]
    public class PixelBoardLevel
    {
        /// <summary>
        /// 关卡名
        /// </summary>
        public string Name = "level";

        /// <summary>
        /// 源图路径（仅作记录）
        /// </summary>
        public string SourceImage = "";

        /// <summary>
        /// 横向格数
        /// </summary>
        public int Width;

        /// <summary>
        /// 纵向格数
        /// </summary>
        public int Height;

        /// <summary>
        /// 调色板，每项 0xRRGGBB
        /// </summary>
        public int[] Palette = new int[0];

        /// <summary>
        /// Width * Height 个颜色索引，-1 表示空格
        /// </summary>
        public int[] Cells = new int[0];

        /// <summary>
        /// 每种颜色占多少格，长度与 Palette 一致
        /// </summary>
        public int[] CellCounts = new int[0];

        /// <summary>
        /// 空格数量
        /// </summary>
        public int EmptyCount;

        /// <summary>
        /// 本关使用的车辆规格，升序，例如 {50, 100, 180}
        /// </summary>
        public int[] CarSizes = new int[0];

        /// <summary>
        /// 全部车辆，同色车的 Load 之和等于该色的 CellCounts
        /// </summary>
        public CarEntry[] Cars = new CarEntry[0];

        /// <summary>
        /// 总格数
        /// </summary>
        public int TotalCells => Width * Height;
    }
}
