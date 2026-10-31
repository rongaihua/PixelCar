//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-29
// Desc: 按中灰模板 + 遮罩生成彩色车辆精灵
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 车辆精灵工厂。素材包提供「中灰车身模板 + 车身遮罩」，
    /// 这里按说明的三步换色：模板转 HSV、只对遮罩内的车身像素替换色相、明暗沿用模板灰度。
    /// 箭头、车窗、描边、高光不在遮罩内，原样保留。
    /// </summary>
    public static class CarSpriteFactory
    {
        private const string Root = "PixelCar/Cars/";

        /// <summary>
        /// 已生成的精灵，键是 尺寸_朝向_颜色。
        /// </summary>
        private static readonly Dictionary<string, Sprite> sCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// 取一张指定颜色、尺寸、朝向的车辆精灵。同参数只生成一次。
        /// </summary>
        /// <param name="size">尺寸。</param>
        /// <param name="dir">朝向。</param>
        /// <param name="color">车身颜色。</param>
        /// <returns>精灵。素材缺失时为空。</returns>
        public static Sprite Get(ECarSize size, ECarDir dir, Color color)
        {
            var sizeName = SizeName(size);
            var dirName = DirName(dir);
            var key = $"{sizeName}_{dirName}_{Mathf.RoundToInt(color.r * 255f)}_{Mathf.RoundToInt(color.g * 255f)}_{Mathf.RoundToInt(color.b * 255f)}";

            Sprite cached;
            if (sCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            var sprite = Build(sizeName, dirName, color);
            if (sprite)
            {
                sCache.Add(key, sprite);
            }

            return sprite;
        }

        /// <summary>
        /// 清空缓存并销毁生成的贴图。切关时调用。
        /// </summary>
        public static void Clear()
        {
            foreach (var pair in sCache)
            {
                if (pair.Value)
                {
                    var texture = pair.Value.texture;
                    Object.Destroy(pair.Value);
                    if (texture)
                    {
                        Object.Destroy(texture);
                    }
                }
            }

            sCache.Clear();
        }

        /// <summary>
        /// 生成一张精灵。
        /// </summary>
        /// <param name="sizeName">尺寸名。</param>
        /// <param name="dirName">朝向名。</param>
        /// <param name="color">车身颜色。</param>
        /// <returns>精灵。素材缺失时为空。</returns>
        private static Sprite Build(string sizeName, string dirName, Color color)
        {
            var template = Resources.Load<Texture2D>($"{Root}tpl_{sizeName}_{dirName}");
            var mask = Resources.Load<Texture2D>($"{Root}mask_{sizeName}_{dirName}");
            if (!template || !mask)
            {
                MyGame.Logger.Warn($"车辆素材缺失：{sizeName}_{dirName}");
                return null;
            }

            var source = template.GetPixels32();
            var maskPixels = mask.GetPixels32();

            Color.RGBToHSV(color, out var hue, out var saturation, out _);

            // 白/米白属于低饱和，套统一公式会发灰：降饱和 + 提亮
            var shadeScale = 1f;
            if (saturation < 0.15f)
            {
                saturation *= 0.4f;
                shadeScale = 1.12f;
            }

            // 红色明度压一档，避免偏粉
            if (hue < 0.05f || hue > 0.95f)
            {
                shadeScale *= 0.92f;
            }

            var result = new Color32[source.Length];
            for (var i = 0; i < source.Length; ++i)
            {
                var pixel = source[i];
                if (maskPixels[i].r <= 127)
                {
                    // 遮罩外：箭头、车窗、描边、高光，原样保留
                    result[i] = pixel;
                    continue;
                }

                var shade = Mathf.Clamp01(pixel.r / 255f * shadeScale);
                var tinted = Color.HSVToRGB(hue, saturation, shade);
                result[i] = new Color32(
                    (byte)(tinted.r * 255f),
                    (byte)(tinted.g * 255f),
                    (byte)(tinted.b * 255f),
                    pixel.a);
            }

            var texture = new Texture2D(template.width, template.height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.SetPixels32(result);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 尺寸转素材名。
        /// </summary>
        /// <param name="size">尺寸。</param>
        /// <returns>素材名。</returns>
        private static string SizeName(ECarSize size)
        {
            switch (size)
            {
                case ECarSize.Small:
                    return "small";
                case ECarSize.Mid:
                    return "mid";
                default:
                    return "big";
            }
        }

        /// <summary>
        /// 朝向转素材名。
        /// </summary>
        /// <param name="dir">朝向。</param>
        /// <returns>素材名。</returns>
        private static string DirName(ECarDir dir)
        {
            switch (dir)
            {
                case ECarDir.Up:
                    return "up";
                case ECarDir.Down:
                    return "down";
                case ECarDir.Left:
                    return "left";
                default:
                    return "right";
            }
        }
    }
}
