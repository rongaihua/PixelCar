//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 基础图元精灵
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 基础图元精灵：项目暂时没有美术资源，全部用 1x1 白图拉伸
    /// </summary>
    public static class SpriteLibrary
    {
        private static Sprite sBodySprite;
        private static Sprite sFillSprite;
        private static Sprite sCarBodySprite;
        private static Sprite sCarArrowSprite;
        private static Sprite sPanelSprite;

        /// <summary>
        /// 车身用精灵（中心轴点）
        /// </summary>
        public static Sprite BodySprite
        {
            get
            {
                if (!sBodySprite)
                {
                    sBodySprite = Create(new Vector2(0.5f, 0.5f));
                }

                return sBodySprite;
            }
        }

        /// <summary>
        /// 进度条用精灵（左轴点）
        /// </summary>
        public static Sprite FillSprite
        {
            get
            {
                if (!sFillSprite)
                {
                    sFillSprite = Create(new Vector2(0f, 0.5f));
                }

                return sFillSprite;
            }
        }

        /// <summary>
        /// 俯视车身精灵（白色，运行时按车辆颜色染色）
        /// </summary>
        public static Sprite CarBodySprite
        {
            get
            {
                if (!sCarBodySprite)
                {
                    sCarBodySprite = Resources.Load<Sprite>("PixelCar/car_body");
                }

                if (!sCarBodySprite)
                {
                    sCarBodySprite = BodySprite;
                }

                return sCarBodySprite;
            }
        }

        /// <summary>
        /// 车上的方向箭头精灵
        /// </summary>
        public static Sprite CarArrowSprite
        {
            get
            {
                if (!sCarArrowSprite)
                {
                    sCarArrowSprite = Resources.Load<Sprite>("PixelCar/car_arrow");
                }

                return sCarArrowSprite;
            }
        }

        /// <summary>
        /// 圆角面板（九宫格），白底，用时染色。
        /// </summary>
        public static Sprite PanelSprite
        {
            get
            {
                if (!sPanelSprite)
                {
                    sPanelSprite = CreateRounded(64, 20);
                }

                return sPanelSprite;
            }
        }
        /// <summary>
        /// 用白色贴图生成 1x1 精灵
        /// </summary>
        /// <param name="pivot">轴点</param>
        /// <returns>精灵</returns>
        /// <summary>
        /// 生成一张圆角九宫格精灵。
        /// </summary>
        /// <param name="size">边长</param>
        /// <param name="radius">圆角半径</param>
        /// <returns>精灵</returns>
        private static Sprite CreateRounded(int size, int radius)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (var y = 0; y < size; ++y)
            {
                for (var x = 0; x < size; ++x)
                {
                    var dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    var dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius - distance + 1f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }
        private static Sprite Create(Vector2 pivot)
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), pivot, 1f);
        }
    }
}
