//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-11-02
// Desc: UI 用图元精灵
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// UGUI 用图元精灵。目前只有一张圆角九宫格，给 HUD 的面板和按钮用。
    /// 游戏画面里的车身、色块盘、车道都是不带 sprite 的纯色 Image，不占贴图；
    /// 车辆用的是 <see cref="CarSpriteFactory"/> 按车色生成的精灵。
    /// </summary>
    public static class SpriteLibrary
    {
        private static Sprite sPanelSprite;

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
    }
}
