//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-11-02
// Desc: 色块盘显示（UGUI）
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using PixelCar.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCar.Client
{
    /// <summary>
    /// 色块盘显示：把 BoardState 拆成一列列纯色 Image 摆进画布，
    /// 不再烘焙贴图。同色的连续格合并成一个矩形，
    /// 所以元件数是「每列的颜色段数」，跟格数无关。
    /// </summary>
    public class BoardView
    {
        private static readonly Color sEmptyColor = new Color32(236, 232, 224, 255);

        /// <summary>
        /// 每个矩形比格子多画一点点，避免相邻矩形之间露出底色的细缝。
        /// </summary>
        private const float Overlap = 0.02f;

        private readonly BoardState mBoard;
        private readonly Color32[] mPalette;
        private readonly RectTransform mRoot;
        private readonly RectTransform mPaper;
        private readonly List<Image> mCells = new List<Image>();

        private Vector2 mOrigin;
        private Vector2 mCellSize = Vector2.one;
        private bool mPlaced;
        private int mDrawn;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="board">盘面</param>
        /// <param name="palette">调色板</param>
        public BoardView(Transform parent, BoardState board, Color32[] palette)
        {
            mBoard = board;
            mPalette = palette;

            mRoot = GameCanvas.NewRect("Board", parent);
            mPaper = GameCanvas.NewQuad("Paper", mRoot, sEmptyColor);
        }

        /// <summary>
        /// 把盘面铺到指定世界矩形。左下角对齐 origin，格数与世界尺寸解耦。
        /// </summary>
        /// <param name="origin">左下角世界坐标</param>
        /// <param name="width">世界宽度</param>
        /// <param name="height">世界高度</param>
        public void Place(Vector3 origin, float width, float height)
        {
            mOrigin = new Vector2(origin.x, origin.y);
            mCellSize = new Vector2(
                width / MathUtil.MaxFloat(0.0001f, mBoard.Width),
                height / MathUtil.MaxFloat(0.0001f, mBoard.Height));

            GameCanvas.Place(
                mPaper,
                new Vector2(origin.x + width * 0.5f, origin.y + height * 0.5f),
                new Vector2(width, height));

            mPlaced = true;
            Refresh();
        }

        /// <summary>
        /// 按当前盘面重排
        /// </summary>
        public void Refresh()
        {
            mDrawn = 0;
            if (!mPlaced)
            {
                return;
            }

            for (var x = 0; x < mBoard.Width; ++x)
            {
                var count = mBoard.ColumnCount(x);
                var row = 0;
                while (row < count)
                {
                    var color = mBoard.CellAt(x, row);
                    var run = 1;
                    while (row + run < count && mBoard.CellAt(x, row + run) == color)
                    {
                        ++run;
                    }

                    DrawCell(x, row, run, color);
                    row += run;
                }
            }

            // 用不上的格子先收起来，留着下次复用
            for (var i = mDrawn; i < mCells.Count; ++i)
            {
                var idle = mCells[i];
                if (idle.gameObject.activeSelf)
                {
                    idle.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Destroy()
        {
            if (mRoot)
            {
                Object.Destroy(mRoot.gameObject);
            }
        }

        /// <summary>
        /// 画一段同色的连续格
        /// </summary>
        /// <param name="column">列号</param>
        /// <param name="row">最底下一格的列内下标</param>
        /// <param name="run">这一段有几格</param>
        /// <param name="color">颜色索引</param>
        private void DrawCell(int column, int row, int run, int color)
        {
            var image = TakeCell();
            image.color = ColorOf(color);

            GameCanvas.Place(
                image.rectTransform,
                new Vector2(
                    mOrigin.x + (column + 0.5f) * mCellSize.x,
                    mOrigin.y + (row + run * 0.5f) * mCellSize.y),
                new Vector2(
                    mCellSize.x * (1f + Overlap),
                    mCellSize.y * (run + Overlap)));
        }

        /// <summary>
        /// 从池里取一个格子，不够就新加
        /// </summary>
        /// <returns>格子</returns>
        private Image TakeCell()
        {
            if (mDrawn < mCells.Count)
            {
                var cached = mCells[mDrawn];
                if (!cached.gameObject.activeSelf)
                {
                    cached.gameObject.SetActive(true);
                }

                ++mDrawn;
                return cached;
            }

            var image = GameCanvas.NewImage("Cell", mRoot, Color.white);
            mCells.Add(image);
            ++mDrawn;
            return image;
        }

        /// <summary>
        /// 颜色索引转显示色
        /// </summary>
        /// <param name="color">颜色索引</param>
        /// <returns>显示色</returns>
        private Color ColorOf(int color)
        {
            if (color < 0 || color >= mPalette.Length)
            {
                return sEmptyColor;
            }

            return mPalette[color];
        }
    }
}
