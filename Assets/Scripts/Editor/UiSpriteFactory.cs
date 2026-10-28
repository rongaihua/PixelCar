//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 程序化生成 UGUI 用图元精灵（圆角矩形 / 圆形 / 渐变）
//---------------------------------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelCar.Editor
{
    /// <summary>
    /// 程序化生成 UGUI 用图元精灵。工程暂时没有美术资源，先用这些占位。
    /// </summary>
    public static class UiSpriteFactory
    {
        /// <summary>
        /// 图元输出目录
        /// </summary>
        public const string Directory = "Assets/Res/UI/Generated";

        private const string RoundedRectPath = Directory + "/rounded_rect.png";
        private const string CirclePath = Directory + "/circle.png";
        private const string GradientPath = Directory + "/sky_gradient.png";

        /// <summary>
        /// 圆角矩形（九宫格，圆角 24）
        /// </summary>
        /// <returns>精灵</returns>
        public static Sprite RoundedRect()
        {
            var sprite = Load(RoundedRectPath);
            if (sprite)
            {
                return sprite;
            }

            EnsureDirectory();
            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            DrawRoundedRect(texture, 24);
            return Save(texture, RoundedRectPath, new Vector4(24f, 24f, 24f, 24f));
        }

        /// <summary>
        /// 实心圆
        /// </summary>
        /// <returns>精灵</returns>
        public static Sprite Circle()
        {
            var sprite = Load(CirclePath);
            if (sprite)
            {
                return sprite;
            }

            EnsureDirectory();
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            DrawCircle(texture);
            return Save(texture, CirclePath, Vector4.zero);
        }

        /// <summary>
        /// 竖向渐变（天空占位）
        /// </summary>
        /// <returns>精灵</returns>
        public static Sprite Gradient()
        {
            var sprite = Load(GradientPath);
            if (sprite)
            {
                return sprite;
            }

            EnsureDirectory();
            var texture = new Texture2D(4, 256, TextureFormat.RGBA32, false);
            DrawGradient(texture);
            return Save(texture, GradientPath, Vector4.zero);
        }

        /// <summary>
        /// 读取已生成的精灵
        /// </summary>
        /// <param name="path">资产路径</param>
        /// <returns>精灵</returns>
        private static Sprite Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// 保存纹理并设置导入参数
        /// </summary>
        /// <param name="texture">纹理</param>
        /// <param name="path">资产路径</param>
        /// <param name="border">九宫格边框</param>
        /// <returns>精灵</returns>
        private static Sprite Save(Texture2D texture, string path, Vector4 border)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (!importer)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>
        /// 建目录
        /// </summary>
        private static void EnsureDirectory()
        {
            if (!DirectoryExists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// 目录是否存在
        /// </summary>
        /// <param name="path">资产路径</param>
        /// <returns>是否存在</returns>
        private static bool DirectoryExists(string path)
        {
            return AssetDatabase.IsValidFolder(path);
        }

        /// <summary>
        /// 画圆角矩形
        /// </summary>
        /// <param name="texture">目标纹理</param>
        /// <param name="radius">圆角半径</param>
        private static void DrawRoundedRect(Texture2D texture, int radius)
        {
            var width = texture.width;
            var height = texture.height;
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var dx = Mathf.Max(0, Mathf.Max(radius - x, x - (width - 1 - radius)));
                    var dy = Mathf.Max(0, Mathf.Max(radius - y, y - (height - 1 - radius)));
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius - distance + 1f);
                    pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        /// <summary>
        /// 画实心圆（边缘 1px 抗锯齿）
        /// </summary>
        /// <param name="texture">目标纹理</param>
        private static void DrawCircle(Texture2D texture)
        {
            var size = texture.width;
            var center = (size - 1) * 0.5f;
            var radius = center;
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; ++y)
            {
                for (var x = 0; x < size; ++x)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy);
                    var alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }

        /// <summary>
        /// 画竖向渐变（下深上浅）
        /// </summary>
        /// <param name="texture">目标纹理</param>
        private static void DrawGradient(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; ++y)
            {
                var t = y / (float)(height - 1);
                var color = Color.Lerp(new Color(0.35f, 0.68f, 0.92f), new Color(0.72f, 0.90f, 1f), t);
                Color32 packed = color;

                for (var x = 0; x < width; ++x)
                {
                    pixels[y * width + x] = packed;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
        }
    }
}
