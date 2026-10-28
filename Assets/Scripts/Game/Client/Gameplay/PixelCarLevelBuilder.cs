//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 关卡数据加载与测试关生成
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 关卡数据加载与测试关生成
    /// </summary>
    public static class PixelCarLevelBuilder
    {
        /// <summary>
        /// 载入关卡：优先读入口上配置的 JSON，读不到就生成一张测试图
        /// </summary>
        /// <param name="game">客户端入口</param>
        /// <returns>关卡数据</returns>
        public static PixelBoardLevel Load(GameEntry game)
        {
            var json = game.LevelJson;
            if (json && !string.IsNullOrEmpty(json.text))
            {
                try
                {
                    var level = JsonUtility.FromJson<PixelBoardLevel>(json.text);
                    if (level != null && level.Width > 0 && level.Height > 0 && level.Palette != null && level.Palette.Length > 0)
                    {
                        if (level.Cars == null || level.Cars.Length == 0)
                        {
                            BuildCars(level, CarSizes(game), CarShares(game));
                        }

                        return level;
                    }
                }
                catch (System.Exception exception)
                {
                    MyGame.Logger.Warn($"关卡 JSON 解析失败，改用测试关：{exception.Message}");
                }
            }

            return BuildTest(game);
        }

        /// <summary>
        /// 生成测试关
        /// </summary>
        /// <param name="game">客户端入口</param>
        /// <returns>关卡数据</returns>
        public static PixelBoardLevel BuildTest(GameEntry game)
        {
            var width = MathUtil.Clamp(game.TestWidth, 8, 200);
            var height = MathUtil.Clamp(game.TestHeight, 8, 200);
            var colors = MathUtil.Clamp(game.TestColors, 2, 12);

            var level = new PixelBoardLevel
            {
                Name = "prototype",
                Width = width,
                Height = height,
                Palette = new int[colors],
                Cells = new int[width * height],
                CellCounts = new int[colors],
                EmptyCount = 0,
            };

            // 调色板：均匀色相，保证色块区分度
            for (var i = 0; i < colors; ++i)
            {
                var color = Color.HSVToRGB(i / (float)colors, 0.62f, 0.92f);
                Color32 packed = color;
                level.Palette[i] = (packed.r << 16) | (packed.g << 8) | packed.b;
            }

            // 用 Perlin 噪声凑一张有形状的图
            for (var y = 0; y < height; ++y)
            {
                for (var x = 0; x < width; ++x)
                {
                    var noise = Mathf.PerlinNoise(x * 0.105f + 4.31f, y * 0.105f + 8.97f);
                    var color = MathUtil.Clamp(MathUtil.FloorToInt(noise * colors), 0, colors - 1);
                    level.Cells[y * width + x] = color;
                    ++level.CellCounts[color];
                }
            }

            BuildCars(level, CarSizes(game), CarShares(game));
            return level;
        }

        /// <summary>
        /// 按格数拆车，保证同色车的 Load 之和等于该色格数
        /// </summary>
        /// <param name="level">关卡数据</param>
        /// <param name="sizes">车辆规格</param>
        /// <param name="shares">车数配比</param>
        public static void BuildCars(PixelBoardLevel level, int[] sizes, float[] shares)
        {
            if (level == null || level.CellCounts == null || level.CellCounts.Length == 0)
            {
                return;
            }

            var colorCount = level.CellCounts.Length;
            var plans = new List<int>[colorCount];
            var remaining = new int[colorCount];

            for (var color = 0; color < colorCount; ++color)
            {
                remaining[color] = level.CellCounts[color];
                int tail;
                plans[color] = CarSplitter.Split(level.CellCounts[color], sizes, shares, true, out tail) ?? new List<int>();
            }

            var maxCars = 0;
            foreach (var plan in plans)
            {
                maxCars = MathUtil.Max(maxCars, plan.Count);
            }

            var cars = new List<CarEntry>();

            // 轮转取车，避免待命区里同色车挤在一起
            for (var index = 0; index < maxCars; ++index)
            {
                for (var color = 0; color < colorCount; ++color)
                {
                    if (index >= plans[color].Count)
                    {
                        continue;
                    }

                    var capacity = plans[color][index];
                    var load = MathUtil.Min(capacity, remaining[color]);
                    if (load <= 0)
                    {
                        continue;
                    }

                    remaining[color] -= load;
                    cars.Add(new CarEntry { Color = color, Capacity = capacity, Load = load });
                }
            }

            level.CarSizes = new int[] { sizes[0], sizes[1], sizes[2] };
            level.Cars = cars.ToArray();
        }

        /// <summary>
        /// 取车辆规格
        /// </summary>
        /// <param name="game">客户端入口</param>
        /// <returns>规格数组</returns>
        private static int[] CarSizes(GameEntry game)
        {
            return new int[] { game.SmallCar, game.MidCar, game.LargeCar };
        }

        /// <summary>
        /// 取车数配比
        /// </summary>
        /// <param name="game">客户端入口</param>
        /// <returns>配比数组</returns>
        private static float[] CarShares(GameEntry game)
        {
            return new float[] { game.TestShares.x, game.TestShares.y, game.TestShares.z };
        }
    }
}
