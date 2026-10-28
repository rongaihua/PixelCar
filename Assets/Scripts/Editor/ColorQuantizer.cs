//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 图片采样与调色板量化（编辑器工具用）
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace PixelCar.Editor
{
    /// <summary>
    /// 图片 → 色块盘 的核心算法：按格子采样、k-means 提取调色板、把每格映射到色板索引
    /// </summary>
    public static class ColorQuantizer
    {
        /// <summary>
        /// 空格的颜色索引
        /// </summary>
        public const int Empty = -1;

        /// <summary>
        /// 按格数采样成颜色数组，行 0 为最底部
        /// </summary>
        /// <param name="texture">源图</param>
        /// <param name="gridWidth">横向格数</param>
        /// <param name="gridHeight">纵向格数</param>
        /// <param name="mode">采样方式</param>
        /// <returns>长度 gridWidth * gridHeight 的颜色数组</returns>
        public static Color32[] Sample(Texture2D texture, int gridWidth, int gridHeight, ESampleMode mode)
        {
            if (!texture)
            {
                return new Color32[0];
            }

            gridWidth = Mathf.Max(1, gridWidth);
            gridHeight = Mathf.Max(1, gridHeight);

            var source = texture.GetPixels32();
            var sourceWidth = texture.width;
            var sourceHeight = texture.height;
            var result = new Color32[gridWidth * gridHeight];

            for (var gridY = 0; gridY < gridHeight; ++gridY)
            {
                var y0 = Mathf.Clamp(Mathf.FloorToInt((float)gridY * sourceHeight / gridHeight), 0, sourceHeight - 1);
                var y1 = Mathf.CeilToInt((float)(gridY + 1) * sourceHeight / gridHeight);
                y1 = Mathf.Clamp(y1, y0 + 1, sourceHeight);

                for (var gridX = 0; gridX < gridWidth; ++gridX)
                {
                    var x0 = Mathf.Clamp(Mathf.FloorToInt((float)gridX * sourceWidth / gridWidth), 0, sourceWidth - 1);
                    var x1 = Mathf.CeilToInt((float)(gridX + 1) * sourceWidth / gridWidth);
                    x1 = Mathf.Clamp(x1, x0 + 1, sourceWidth);

                    result[gridY * gridWidth + gridX] = SampleRect(source, sourceWidth, x0, y0, x1, y1, mode);
                }
            }

            return result;
        }

        /// <summary>
        /// k-means++ 提取调色板，结果按亮度从高到低排序
        /// </summary>
        /// <param name="cells">每格颜色</param>
        /// <param name="colorCount">目标颜色数</param>
        /// <param name="iterations">迭代次数</param>
        /// <returns>调色板</returns>
        public static List<Color32> ExtractPalette(Color32[] cells, int colorCount, int iterations)
        {
            var samples = new List<Vector3>(cells.Length);
            foreach (var cell in cells)
            {
                if (cell.a >= 128)
                {
                    samples.Add(new Vector3(cell.r, cell.g, cell.b));
                }
            }

            if (samples.Count == 0)
            {
                return new List<Color32>();
            }

            var count = Mathf.Clamp(colorCount, 1, Mathf.Min(48, samples.Count));
            iterations = Mathf.Clamp(iterations, 1, 100);

            var random = new System.Random(1337);
            var centroids = new Vector3[count];
            var distances = new float[samples.Count];

            centroids[0] = samples[random.Next(samples.Count)];
            for (var i = 0; i < samples.Count; ++i)
            {
                distances[i] = SqrDistance(samples[i], centroids[0]);
            }

            // k-means++ 初始化：按距离平方加权挑下一个中心
            for (var c = 1; c < count; ++c)
            {
                var total = 0d;
                foreach (var distance in distances)
                {
                    total += distance;
                }

                var pick = 0;
                if (total > 0d)
                {
                    var cursor = random.NextDouble() * total;
                    for (var i = 0; i < samples.Count; ++i)
                    {
                        cursor -= distances[i];
                        if (cursor <= 0d)
                        {
                            pick = i;
                            break;
                        }
                    }
                }

                centroids[c] = samples[pick];
                for (var i = 0; i < samples.Count; ++i)
                {
                    var distance = SqrDistance(samples[i], centroids[c]);
                    if (distance < distances[i])
                    {
                        distances[i] = distance;
                    }
                }
            }

            var assignment = new int[samples.Count];
            for (var i = 0; i < assignment.Length; ++i)
            {
                assignment[i] = -1;
            }

            for (var iteration = 0; iteration < iterations; ++iteration)
            {
                var changed = false;
                for (var i = 0; i < samples.Count; ++i)
                {
                    var best = 0;
                    var bestDistance = float.MaxValue;
                    for (var c = 0; c < count; ++c)
                    {
                        var distance = SqrDistance(samples[i], centroids[c]);
                        if (distance < bestDistance)
                        {
                            bestDistance = distance;
                            best = c;
                        }
                    }

                    if (assignment[i] == best)
                    {
                        continue;
                    }

                    assignment[i] = best;
                    changed = true;
                }

                if (!changed && iteration > 0)
                {
                    break;
                }

                var sums = new Vector3[count];
                var counts = new int[count];
                for (var i = 0; i < samples.Count; ++i)
                {
                    sums[assignment[i]] += samples[i];
                    ++counts[assignment[i]];
                }

                for (var c = 0; c < count; ++c)
                {
                    centroids[c] = counts[c] > 0 ? sums[c] / counts[c] : samples[random.Next(samples.Count)];
                }
            }

            var palette = new List<Color32>(count);
            foreach (var centroid in centroids)
            {
                var color = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(centroid.x), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(centroid.y), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(centroid.z), 0, 255),
                    255);

                var duplicated = false;
                foreach (var exist in palette)
                {
                    if (SqrDistance(new Vector3(exist.r, exist.g, exist.b), centroid) < 9f)
                    {
                        duplicated = true;
                        break;
                    }
                }

                if (!duplicated)
                {
                    palette.Add(color);
                }
            }

            palette.Sort(CompareByLuma);
            return palette;
        }

        /// <summary>
        /// 把每格颜色映射到色板索引，透明格返回 -1
        /// </summary>
        /// <param name="cells">每格颜色</param>
        /// <param name="palette">色板</param>
        /// <param name="alphaAsEmpty">透明格是否算空格</param>
        /// <returns>索引数组</returns>
        public static int[] Map(Color32[] cells, IList<Color32> palette, bool alphaAsEmpty)
        {
            var indices = new int[cells.Length];
            if (palette == null || palette.Count == 0)
            {
                for (var i = 0; i < indices.Length; ++i)
                {
                    indices[i] = Empty;
                }

                return indices;
            }

            for (var i = 0; i < cells.Length; ++i)
            {
                var cell = cells[i];
                if (alphaAsEmpty && cell.a < 128)
                {
                    indices[i] = Empty;
                    continue;
                }

                indices[i] = NearestIndex(cell, palette);
            }

            return indices;
        }

        /// <summary>
        /// 统计每种颜色占用多少格
        /// </summary>
        /// <param name="indices">索引数组</param>
        /// <param name="paletteSize">色板长度</param>
        /// <returns>每色格数</returns>
        public static int[] CountColors(int[] indices, int paletteSize)
        {
            var counts = new int[paletteSize];
            foreach (var index in indices)
            {
                if (index >= 0 && index < paletteSize)
                {
                    ++counts[index];
                }
            }

            return counts;
        }

        /// <summary>
        /// 对矩形区域采样
        /// </summary>
        /// <param name="source">源像素</param>
        /// <param name="sourceWidth">源图宽度</param>
        /// <param name="x0">左</param>
        /// <param name="y0">下</param>
        /// <param name="x1">右</param>
        /// <param name="y1">上</param>
        /// <param name="mode">采样方式</param>
        /// <returns>采样颜色</returns>
        private static Color32 SampleRect(Color32[] source, int sourceWidth, int x0, int y0, int x1, int y1, ESampleMode mode)
        {
            if (mode == ESampleMode.Center)
            {
                var centerX = (x0 + x1 - 1) / 2;
                var centerY = (y0 + y1 - 1) / 2;
                return source[centerY * sourceWidth + centerX];
            }

            if (mode == ESampleMode.Dominant)
            {
                return SampleDominant(source, sourceWidth, x0, y0, x1, y1);
            }

            return SampleAverage(source, sourceWidth, x0, y0, x1, y1);
        }

        /// <summary>
        /// 区域平均色
        /// </summary>
        /// <param name="source">源像素</param>
        /// <param name="sourceWidth">源图宽度</param>
        /// <param name="x0">左</param>
        /// <param name="y0">下</param>
        /// <param name="x1">右</param>
        /// <param name="y1">上</param>
        /// <returns>平均色</returns>
        private static Color32 SampleAverage(Color32[] source, int sourceWidth, int x0, int y0, int x1, int y1)
        {
            long r = 0;
            long g = 0;
            long b = 0;
            long a = 0;
            var count = 0;

            for (var y = y0; y < y1; ++y)
            {
                var row = y * sourceWidth;
                for (var x = x0; x < x1; ++x)
                {
                    var pixel = source[row + x];
                    r += pixel.r;
                    g += pixel.g;
                    b += pixel.b;
                    a += pixel.a;
                    ++count;
                }
            }

            if (count == 0)
            {
                return new Color32(0, 0, 0, 0);
            }

            return new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)(a / count));
        }

        /// <summary>
        /// 区域内出现最多的颜色（5 位分桶后求均值）
        /// </summary>
        /// <param name="source">源像素</param>
        /// <param name="sourceWidth">源图宽度</param>
        /// <param name="x0">左</param>
        /// <param name="y0">下</param>
        /// <param name="x1">右</param>
        /// <param name="y1">上</param>
        /// <returns>主色</returns>
        private static Color32 SampleDominant(Color32[] source, int sourceWidth, int x0, int y0, int x1, int y1)
        {
            var histogram = new Dictionary<int, int>(128);
            for (var y = y0; y < y1; ++y)
            {
                var row = y * sourceWidth;
                for (var x = x0; x < x1; ++x)
                {
                    var key = BucketKey(source[row + x]);
                    histogram.TryGetValue(key, out var value);
                    histogram[key] = value + 1;
                }
            }

            var bestKey = 0;
            var bestCount = -1;
            foreach (var pair in histogram)
            {
                if (pair.Value > bestCount)
                {
                    bestCount = pair.Value;
                    bestKey = pair.Key;
                }
            }

            long r = 0;
            long g = 0;
            long b = 0;
            long a = 0;
            var count = 0;

            for (var y = y0; y < y1; ++y)
            {
                var row = y * sourceWidth;
                for (var x = x0; x < x1; ++x)
                {
                    var pixel = source[row + x];
                    if (BucketKey(pixel) != bestKey)
                    {
                        continue;
                    }

                    r += pixel.r;
                    g += pixel.g;
                    b += pixel.b;
                    a += pixel.a;
                    ++count;
                }
            }

            if (count == 0)
            {
                return new Color32(0, 0, 0, 0);
            }

            return new Color32((byte)(r / count), (byte)(g / count), (byte)(b / count), (byte)(a / count));
        }

        /// <summary>
        /// 颜色分桶键
        /// </summary>
        /// <param name="color">颜色</param>
        /// <returns>键</returns>
        private static int BucketKey(Color32 color)
        {
            return ((color.r >> 3) << 10) | ((color.g >> 3) << 5) | (color.b >> 3);
        }

        /// <summary>
        /// 找最接近的色板索引
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="palette">色板</param>
        /// <returns>索引</returns>
        private static int NearestIndex(Color32 color, IList<Color32> palette)
        {
            var best = 0;
            var bestDistance = int.MaxValue;

            for (var i = 0; i < palette.Count; ++i)
            {
                var candidate = palette[i];
                var dr = color.r - candidate.r;
                var dg = color.g - candidate.g;
                var db = color.b - candidate.b;

                // 近似感知权重：绿色最敏感，蓝色最钝
                var distance = dr * dr * 2 + dg * dg * 4 + db * db * 3;
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                best = i;
            }

            return best;
        }

        /// <summary>
        /// 按亮度降序比较
        /// </summary>
        /// <param name="left">左</param>
        /// <param name="right">右</param>
        /// <returns>比较结果</returns>
        private static int CompareByLuma(Color32 left, Color32 right)
        {
            return Luma(right).CompareTo(Luma(left));
        }

        /// <summary>
        /// 颜色距离平方
        /// </summary>
        /// <param name="a">颜色 a</param>
        /// <param name="b">颜色 b</param>
        /// <returns>距离平方</returns>
        private static float SqrDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dy = a.y - b.y;
            var dz = a.z - b.z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// 亮度
        /// </summary>
        /// <param name="color">颜色</param>
        /// <returns>亮度</returns>
        private static float Luma(Color32 color)
        {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }
    }
}
