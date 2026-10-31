//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 色块盘显示
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 色块盘显示：把 BoardState 画成一张点采样贴图
    /// </summary>
    public class BoardView
    {
        private static readonly Color32 sEmptyColor = new(236, 232, 224, 255);

        private readonly GameObject mRoot;
        private readonly Texture2D mTexture;
        private readonly Color32[] mPixels;
        private readonly BoardState mBoard;
        private readonly Color32[] mPalette;

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

            mRoot = new GameObject("Board");
            mRoot.transform.SetParent(parent, false);
            mRoot.transform.position = Vector3.zero;

            mTexture = new Texture2D(board.Width, board.Height, TextureFormat.RGBA32, false);
            mTexture.filterMode = FilterMode.Point;
            mTexture.wrapMode = TextureWrapMode.Clamp;
            mPixels = new Color32[board.Width * board.Height];

            var sprite = Sprite.Create(mTexture, new Rect(0f, 0f, board.Width, board.Height), Vector2.zero, 1f);
            var renderer = mRoot.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 0;
        }

        /// <summary>
        /// 把盘面铺到指定世界矩形。左下角对齐 origin，格数与世界尺寸解耦。
        /// </summary>
        /// <param name="origin">左下角世界坐标</param>
        /// <param name="width">世界宽度</param>
        /// <param name="height">世界高度</param>
        public void Place(Vector3 origin, float width, float height)
        {
            mRoot.transform.position = origin;
            mRoot.transform.localScale = new Vector3(
                width / MathUtil.MaxFloat(0.0001f, mBoard.Width),
                height / MathUtil.MaxFloat(0.0001f, mBoard.Height),
                1f);
        }
        /// <summary>
        /// 按当前盘面重绘
        /// </summary>
        public void Refresh()
        {
            var width = mBoard.Width;
            var height = mBoard.Height;

            for (var x = 0; x < width; ++x)
            {
                var count = mBoard.ColumnCount(x);
                for (var row = 0; row < height; ++row)
                {
                    var pixel = sEmptyColor;
                    if (row < count)
                    {
                        var color = mBoard.CellAt(x, row);
                        if (color >= 0 && color < mPalette.Length)
                        {
                            pixel = mPalette[color];
                        }
                    }

                    mPixels[row * width + x] = pixel;
                }
            }

            mTexture.SetPixels32(mPixels);
            mTexture.Apply(false);
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Destroy()
        {
            Object.Destroy(mRoot);
            Object.Destroy(mTexture);
        }
    }
}
