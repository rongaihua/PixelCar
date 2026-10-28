//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 色块盘生成器编辑器窗口
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using PixelCar.Client;
using PixelCar.Common;
using UnityEditor;
using UnityEngine;

namespace PixelCar.Editor
{
    /// <summary>
    /// 色块盘生成器：选图 → 设格子数 → 量化调色板 → 生成 → 拆车 → 导出
    /// </summary>
    public class PixelBoardGeneratorWindow : EditorWindow
    {
        private Texture2D mSourceAsset;
        private Texture2D mWorkTexture;
        private string mWorkPath = "";

        private int mGridWidth = 30;
        private int mGridHeight = 20;
        private bool mLockAspect = true;
        private ESampleMode mSampleMode = ESampleMode.Average;
        private bool mAlphaAsEmpty = true;

        private bool mUseFixedPalette;
        private int mPaletteSize = 8;
        private int mIterations = 16;
        private readonly List<Color> mPalette = new();
        private Vector2 mPaletteScroll;

        private Color32[] mCellColors;
        private int[] mCellIndices;
        private int[] mCounts;
        private Texture2D mBoardTexture;
        private int mBoardWidth;
        private int mBoardHeight;

        private int mCarSmall = 50;
        private int mCarMid = 100;
        private int mCarLarge = 180;
        private int mShareSmall = 30;
        private int mShareMid = 40;
        private int mShareLarge = 30;
        private bool mAllowTail = true;

        private List<int>[] mColorCars;
        private int mCarTotal;
        private int mTailCount;
        private int mSmallCount;
        private int mMidCount;
        private int mLargeCount;

        private string mExportDir = "Assets/Generated/PixelBoards";
        private string mFileName = "level_01";

        private string mStatus = "① 选图   ② 设格子数   ③ 生成   ④ 导出";
        private Vector2 mScroll;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/PixelCar/色块盘生成器")]
        public static void Open()
        {
            var window = GetWindow<PixelBoardGeneratorWindow>(false, "色块盘生成器", true);
            window.minSize = new Vector2(580f, 720f);
            window.Show();
        }

        /// <summary>
        /// 关闭时清理临时纹理
        /// </summary>
        private void OnDisable()
        {
            if (mBoardTexture)
            {
                DestroyImmediate(mBoardTexture);
            }

            mBoardTexture = null;

            if (mWorkTexture)
            {
                DestroyImmediate(mWorkTexture);
            }

            mWorkTexture = null;
        }

        /// <summary>
        /// 绘制窗口
        /// </summary>
        private void OnGUI()
        {
            mScroll = EditorGUILayout.BeginScrollView(mScroll);
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(mStatus, mStatus.StartsWith("⚠") ? MessageType.Warning : MessageType.None);

            DrawSourceSection();
            DrawGridSection();
            DrawPaletteSection();
            DrawActionSection();
            DrawPreviewSection();
            DrawStatsSection();
            DrawExportSection();

            EditorGUILayout.Space(12f);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 源图区
        /// </summary>
        private void DrawSourceSection()
        {
            EditorGUILayout.LabelField("1. 源图", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            mSourceAsset = (Texture2D)EditorGUILayout.ObjectField("图片资源", mSourceAsset, typeof(Texture2D), false);
            if (EditorGUI.EndChangeCheck() && mSourceAsset)
            {
                var assetPath = AssetDatabase.GetAssetPath(mSourceAsset);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    LoadFromPath(assetPath, true);
                }
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("从磁盘选择图片…", GUILayout.Height(24f)))
            {
                var path = EditorUtility.OpenFilePanel("选择图片", "", "png,jpg,jpeg");
                if (!string.IsNullOrEmpty(path))
                {
                    LoadFromPath(path, false);
                }
            }

            GUI.enabled = mWorkTexture;
            if (GUILayout.Button("清空", GUILayout.Width(64f), GUILayout.Height(24f)))
            {
                ClearSource();
                mStatus = "已清空源图";
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            if (mWorkTexture)
            {
                EditorGUILayout.LabelField($"已载入：{Path.GetFileName(mWorkPath)}（{mWorkTexture.width} × {mWorkTexture.height}）", EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 格子数区
        /// </summary>
        private void DrawGridSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("2. 格子数", EditorStyles.boldLabel);

            mGridWidth = EditorGUILayout.IntSlider("横向格数", mGridWidth, 4, 200);

            if (mLockAspect && mWorkTexture)
            {
                EditorGUILayout.LabelField("纵向格数", $"{ActualGridHeight}（按图片比例）");
            }
            else
            {
                mGridHeight = EditorGUILayout.IntSlider("纵向格数", mGridHeight, 4, 200);
            }

            mLockAspect = EditorGUILayout.Toggle("锁定图片比例", mLockAspect);
            mSampleMode = (ESampleMode)EditorGUILayout.EnumPopup("采样方式", mSampleMode);
            mAlphaAsEmpty = EditorGUILayout.Toggle("透明色算空格", mAlphaAsEmpty);

            var total = mGridWidth * ActualGridHeight;
            EditorGUILayout.LabelField($"盘面 {mGridWidth} × {ActualGridHeight} = {total} 格", EditorStyles.miniLabel);
            EditorGUILayout.HelpBox("格子数决定单关时长：格数越多，需要的车越多。参考 60 辆车 × 100 格 ≈ 30×20 的盘面。", MessageType.None);
        }

        /// <summary>
        /// 调色板区
        /// </summary>
        private void DrawPaletteSection()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("3. 调色板", EditorStyles.boldLabel);

            mUseFixedPalette = EditorGUILayout.Toggle("使用固定色板", mUseFixedPalette);
            if (mUseFixedPalette)
            {
                EditorGUILayout.HelpBox("固定色板模式下，颜色顺序就是游戏里的车色顺序，必须一一对应。", MessageType.Info);
            }
            else
            {
                mPaletteSize = EditorGUILayout.IntSlider("颜色数量", mPaletteSize, 2, 32);
                mIterations = EditorGUILayout.IntSlider("k-means 迭代", mIterations, 4, 40);
            }

            if (mPalette.Count > 0)
            {
                EditorGUILayout.LabelField("色板（可手动改色，改完点「重新映射」）", EditorStyles.miniLabel);
                mPaletteScroll = EditorGUILayout.BeginScrollView(mPaletteScroll, GUILayout.Height(118f));
                for (var i = 0; i < mPalette.Count; ++i)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(24f));
                    mPalette[i] = EditorGUILayout.ColorField(GUIContent.none, mPalette[i], false, false, false, GUILayout.Width(90f));
                    EditorGUILayout.SelectableLabel($"#{ColorUtility.ToHtmlStringRGB(mPalette[i])}", GUILayout.Width(78f), GUILayout.Height(16f));

                    if (GUILayout.Button("✕", GUILayout.Width(24f)))
                    {
                        mPalette.RemoveAt(i);
                        --i;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("＋ 加一种颜色"))
            {
                mPalette.Add(Color.white);
            }

            GUI.enabled = mCellColors != null && mPalette.Count > 0;
            if (GUILayout.Button("重新映射（改色后点这里）"))
            {
                Remap();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 生成按钮
        /// </summary>
        private void DrawActionSection()
        {
            EditorGUILayout.Space(8f);
            GUI.enabled = mWorkTexture;
            if (GUILayout.Button("生成色块盘", GUILayout.Height(34f)))
            {
                Generate();
            }

            GUI.enabled = true;
        }

        /// <summary>
        /// 按当前源图与参数生成
        /// </summary>
        private void Generate()
        {
            if (!mWorkTexture)
            {
                mStatus = "⚠ 先选一张图片";
                return;
            }

            var width = Mathf.Max(1, mGridWidth);
            var height = Mathf.Max(1, ActualGridHeight);

            try
            {
                mCellColors = ColorQuantizer.Sample(mWorkTexture, width, height, mSampleMode);

                List<Color32> palette;
                if (mUseFixedPalette && mPalette.Count > 0)
                {
                    palette = ToColor32List(mPalette);
                }
                else
                {
                    palette = ColorQuantizer.ExtractPalette(mCellColors, mPaletteSize, mIterations);
                    if (palette.Count == 0)
                    {
                        mStatus = "⚠ 这张图没有可用像素（全透明？）";
                        return;
                    }

                    mPalette.Clear();
                    foreach (var color in palette)
                    {
                        mPalette.Add(color);
                    }
                }

                mCellIndices = ColorQuantizer.Map(mCellColors, palette, mAlphaAsEmpty);
                mCounts = ColorQuantizer.CountColors(mCellIndices, palette.Count);

                mBoardWidth = width;
                mBoardHeight = height;
                RebuildBoardTexture();
                PlanCars();

                var valid = 0;
                foreach (var count in mCounts)
                {
                    valid += count;
                }

                mStatus = $"已生成 {width}×{height} = {width * height} 格，{palette.Count} 种颜色，有效 {valid} 格";
            }
            catch (System.Exception exception)
            {
                mStatus = $"⚠ 生成失败：{exception.Message}";
                Debug.LogException(exception);
            }

            Repaint();
        }

        /// <summary>
        /// 按当前色板重新映射
        /// </summary>
        private void Remap()
        {
            if (mCellColors == null || mPalette.Count == 0)
            {
                Generate();
                return;
            }

            var palette = ToColor32List(mPalette);
            mCellIndices = ColorQuantizer.Map(mCellColors, palette, mAlphaAsEmpty);
            mCounts = ColorQuantizer.CountColors(mCellIndices, palette.Count);
            RebuildBoardTexture();
            PlanCars();
            mStatus = $"已按当前色板重新映射，{palette.Count} 种颜色";
            Repaint();
        }

        /// <summary>
        /// 重绘预览纹理
        /// </summary>
        private void RebuildBoardTexture()
        {
            if (mBoardTexture)
            {
                DestroyImmediate(mBoardTexture);
            }

            if (mCellIndices == null || mBoardWidth <= 0 || mBoardHeight <= 0)
            {
                return;
            }

            var texture = new Texture2D(mBoardWidth, mBoardHeight, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[mBoardWidth * mBoardHeight];
            for (var i = 0; i < pixels.Length; ++i)
            {
                var index = mCellIndices[i];
                pixels[i] = index >= 0 && index < mPalette.Count ? (Color32)mPalette[index] : new Color32(0, 0, 0, 0);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            mBoardTexture = texture;
        }

        /// <summary>
        /// 预览区
        /// </summary>
        private void DrawPreviewSection()
        {
            if (!mWorkTexture && !mBoardTexture)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("4. 预览（左：源图   右：色块盘）", EditorStyles.boldLabel);

            var half = Mathf.Max(140f, (position.width - 56f) * 0.5f);
            EditorGUILayout.BeginHorizontal();
            if (mWorkTexture)
            {
                DrawFitted(mWorkTexture, half);
            }
            else
            {
                GUILayoutUtility.GetRect(half, half);
            }

            if (mBoardTexture)
            {
                DrawFitted(mBoardTexture, half);
            }
            else
            {
                GUILayoutUtility.GetRect(half, half);
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 按宽高比画一张贴图
        /// </summary>
        /// <param name="texture">贴图</param>
        /// <param name="maxWidth">最大宽度</param>
        private static void DrawFitted(Texture2D texture, float maxWidth)
        {
            var aspect = texture.height / (float)texture.width;
            var width = maxWidth;
            var height = width * aspect;
            if (height > 340f)
            {
                height = 340f;
                width = height / aspect;
            }

            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        }

        /// <summary>
        /// 统计与拆车区
        /// </summary>
        private void DrawStatsSection()
        {
            if (mCellIndices == null || mCounts == null || mPalette.Count == 0)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("5. 统计与车辆预算", EditorStyles.boldLabel);

            var total = mBoardWidth * mBoardHeight;
            var valid = 0;
            foreach (var count in mCounts)
            {
                valid += count;
            }

            EditorGUILayout.LabelField($"总格 {total}  ·  有颜色 {valid}  ·  空格 {total - valid}", EditorStyles.miniLabel);

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField("车辆规格（格）", EditorStyles.miniLabel);
            EditorGUILayout.BeginHorizontal();
            mCarSmall = Mathf.Max(1, EditorGUILayout.IntField("小", mCarSmall));
            mCarMid = Mathf.Max(1, EditorGUILayout.IntField("中", mCarMid));
            mCarLarge = Mathf.Max(1, EditorGUILayout.IntField("大", mCarLarge));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("目标配比（按车数，%）", EditorStyles.miniLabel);
            mShareSmall = EditorGUILayout.IntSlider("小 %", mShareSmall, 0, 100);
            mShareMid = EditorGUILayout.IntSlider("中 %", mShareMid, 0, 100);
            mShareLarge = EditorGUILayout.IntSlider("大 %", mShareLarge, 0, 100);

            var allowTail = EditorGUILayout.Toggle("允许每色留一辆尾车（装不满）", mAllowTail);
            if (allowTail != mAllowTail)
            {
                mAllowTail = allowTail;
                PlanCars();
            }

            if (GUILayout.Button("按当前规格 / 配比重新拆车"))
            {
                PlanCars();
            }

            var maxCount = 1;
            foreach (var count in mCounts)
            {
                maxCount = Mathf.Max(maxCount, count);
            }

            for (var i = 0; i < mCounts.Length && i < mPalette.Count; ++i)
            {
                var count = mCounts[i];
                if (count <= 0)
                {
                    continue;
                }

                EditorGUILayout.BeginHorizontal();
                var swatch = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f), GUILayout.Height(16f));
                EditorGUI.DrawRect(swatch, mPalette[i]);
                EditorGUILayout.LabelField($"#{ColorUtility.ToHtmlStringRGB(mPalette[i])}  {count,6} 格", GUILayout.Width(150f));
                EditorGUILayout.LabelField(DescribeCars(i), GUILayout.Width(110f));

                var bar = GUILayoutUtility.GetRect(10f, 12f, GUILayout.ExpandWidth(true), GUILayout.Height(12f));
                EditorGUI.DrawRect(bar, new Color(0.18f, 0.18f, 0.18f));
                EditorGUI.DrawRect(new Rect(bar.x, bar.y, bar.width * count / maxCount, bar.height), mPalette[i]);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.LabelField($"共 {mCarTotal} 辆：大 {mLargeCount} / 中 {mMidCount} / 小 {mSmallCount}，尾车 {mTailCount} 辆", EditorStyles.boldLabel);

            if (mCarTotal > 0)
            {
                var smallPercent = 100f * mSmallCount / mCarTotal;
                var midPercent = 100f * mMidCount / mCarTotal;
                var largePercent = 100f * mLargeCount / mCarTotal;
                EditorGUILayout.LabelField($"实际车数占比：小 {smallPercent:F0}% / 中 {midPercent:F0}% / 大 {largePercent:F0}%", EditorStyles.miniLabel);
            }

            if (mTailCount > 0)
            {
                EditorGUILayout.HelpBox($"有 {mTailCount} 种颜色无法整拆（50/100/180 能拼出的数很稀疏），已用尾车装余量。", MessageType.Warning);
            }
        }

        /// <summary>
        /// 按当前规格与配比拆车
        /// </summary>
        private void PlanCars()
        {
            if (mCounts == null)
            {
                return;
            }

            var sizes = new int[] { mCarSmall, mCarMid, mCarLarge };
            var shares = new float[] { mShareSmall, mShareMid, mShareLarge };

            mColorCars = new List<int>[mCounts.Length];
            mCarTotal = 0;
            mTailCount = 0;
            mSmallCount = 0;
            mMidCount = 0;
            mLargeCount = 0;

            for (var i = 0; i < mCounts.Length; ++i)
            {
                int tail;
                var plan = CarSplitter.Split(mCounts[i], sizes, shares, mAllowTail, out tail) ?? new List<int>();
                mColorCars[i] = plan;
                mCarTotal += plan.Count;
                if (tail > 0)
                {
                    ++mTailCount;
                }

                foreach (var capacity in plan)
                {
                    if (capacity == mCarLarge)
                    {
                        ++mLargeCount;
                    }
                    else if (capacity == mCarMid)
                    {
                        ++mMidCount;
                    }
                    else
                    {
                        ++mSmallCount;
                    }
                }
            }
        }

        /// <summary>
        /// 某色的拆车描述
        /// </summary>
        /// <param name="color">颜色索引</param>
        /// <returns>描述</returns>
        private string DescribeCars(int color)
        {
            if (mColorCars == null || color < 0 || color >= mColorCars.Length)
            {
                return "—";
            }

            var plan = mColorCars[color];
            if (plan == null || plan.Count == 0)
            {
                return "拆不出（格数太少）";
            }

            var large = 0;
            var mid = 0;
            var small = 0;
            var other = 0;

            foreach (var capacity in plan)
            {
                if (capacity == mCarLarge)
                {
                    ++large;
                }
                else if (capacity == mCarMid)
                {
                    ++mid;
                }
                else if (capacity == mCarSmall)
                {
                    ++small;
                }
                else
                {
                    ++other;
                }
            }

            var text = "";
            if (large > 0)
            {
                text += $"{large}大 ";
            }

            if (mid > 0)
            {
                text += $"{mid}中 ";
            }

            if (small > 0)
            {
                text += $"{small}小 ";
            }

            if (other > 0)
            {
                text += $"{other}其它 ";
            }

            return text.TrimEnd();
        }

        /// <summary>
        /// 导出区
        /// </summary>
        private void DrawExportSection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("6. 导出", EditorStyles.boldLabel);

            mExportDir = EditorGUILayout.TextField("输出目录", mExportDir);
            mFileName = EditorGUILayout.TextField("文件名", mFileName);

            var ready = mCellIndices != null && mPalette.Count > 0;

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = ready;
            if (GUILayout.Button("导出 JSON", GUILayout.Height(26f)))
            {
                ExportJson();
            }

            if (GUILayout.Button("导出 PNG", GUILayout.Height(26f)))
            {
                ExportPng();
            }

            if (GUILayout.Button("生成资产", GUILayout.Height(26f)))
            {
                ExportAsset();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            GUI.enabled = ready;
            if (GUILayout.Button("把 JSON 复制到剪贴板", GUILayout.Height(22f)))
            {
                EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(BuildLevel(), true);
                mStatus = "JSON 已复制到剪贴板";
            }

            GUI.enabled = true;
            EditorGUILayout.HelpBox("JSON 结构：Width / Height / Palette(0xRRGGBB) / Cells(行主序，第 0 行是最底层，-1=空) / CellCounts / CarSizes / Cars[{Color, Capacity, Load}]。", MessageType.None);
        }

        /// <summary>
        /// 组装关卡数据
        /// </summary>
        /// <returns>关卡数据</returns>
        private PixelBoardLevel BuildLevel()
        {
            var level = new PixelBoardLevel
            {
                Name = mFileName,
                SourceImage = mWorkPath,
                Width = mBoardWidth,
                Height = mBoardHeight,
                Palette = new int[mPalette.Count],
            };

            for (var i = 0; i < mPalette.Count; ++i)
            {
                Color32 color = mPalette[i];
                level.Palette[i] = (color.r << 16) | (color.g << 8) | color.b;
            }

            level.Cells = (int[])mCellIndices.Clone();
            level.CellCounts = mCounts == null ? new int[mPalette.Count] : (int[])mCounts.Clone();

            var valid = 0;
            foreach (var count in level.CellCounts)
            {
                valid += count;
            }

            level.EmptyCount = level.TotalCells - valid;
            level.CarSizes = new int[] { mCarSmall, mCarMid, mCarLarge };

            var cars = new List<CarEntry>();
            if (mColorCars != null)
            {
                for (var color = 0; color < mColorCars.Length; ++color)
                {
                    var plan = mColorCars[color];
                    if (plan == null)
                    {
                        continue;
                    }

                    var remaining = color < level.CellCounts.Length ? level.CellCounts[color] : 0;
                    foreach (var capacity in plan)
                    {
                        var load = Mathf.Min(capacity, remaining);
                        if (load <= 0)
                        {
                            continue;
                        }

                        remaining -= load;
                        cars.Add(new CarEntry { Color = color, Capacity = capacity, Load = load });
                    }
                }
            }

            level.Cars = cars.ToArray();
            return level;
        }

        /// <summary>
        /// 导出 JSON
        /// </summary>
        private void ExportJson()
        {
            var directory = ToAbsolute(mExportDir);
            if (!EnsureDirectory(directory, true))
            {
                mStatus = $"⚠ 输出目录必须是 Assets 下的路径（现在是 {mExportDir}）";
                return;
            }

            var file = Path.Combine(directory, SafeFileName(mFileName) + ".json");
            File.WriteAllText(file, JsonUtility.ToJson(BuildLevel(), true));
            AssetDatabase.Refresh();
            mStatus = $"已导出 {file}";
            Debug.Log($"色块盘导出 JSON → {file}");
        }

        /// <summary>
        /// 导出预览 PNG
        /// </summary>
        private void ExportPng()
        {
            if (!mBoardTexture)
            {
                mStatus = "⚠ 还没有色块盘可导出";
                return;
            }

            var directory = ToAbsolute(mExportDir);
            EnsureDirectory(directory, false);
            var file = Path.Combine(directory, SafeFileName(mFileName) + "_board.png");
            File.WriteAllBytes(file, mBoardTexture.EncodeToPNG());
            AssetDatabase.Refresh();
            mStatus = $"已导出 {file}";
            Debug.Log($"色块盘导出 PNG → {file}");
        }

        /// <summary>
        /// 生成 ScriptableObject 资产
        /// </summary>
        private void ExportAsset()
        {
            var directory = ToAbsolute(mExportDir);
            if (!EnsureDirectory(directory, true))
            {
                mStatus = "⚠ 输出目录必须是 Assets 下的路径";
                return;
            }

            var path = mExportDir.TrimEnd('/', '\\') + "/" + SafeFileName(mFileName) + ".asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            var asset = CreateInstance<PixelBoardAsset>();
            asset.Level = BuildLevel();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            mStatus = $"已生成资产 {path}";
        }

        /// <summary>
        /// 从磁盘或资产路径载入纹理
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="isAssetPath">是否是 Assets 相对路径</param>
        private void LoadFromPath(string path, bool isAssetPath)
        {
            var absolute = isAssetPath ? ToAbsolute(path) : path;
            var texture = LoadTextureFromBytes(absolute);
            if (!texture)
            {
                mStatus = $"⚠ 读不到这张图（支持 png / jpg）：{path}";
                return;
            }

            if (mWorkTexture)
            {
                DestroyImmediate(mWorkTexture);
            }

            mWorkTexture = texture;
            mWorkTexture.filterMode = FilterMode.Point;
            mWorkTexture.wrapMode = TextureWrapMode.Clamp;
            mWorkPath = path;
            mFileName = SafeFileName(Path.GetFileNameWithoutExtension(path));
            mStatus = $"已载入 {texture.width}×{texture.height}";
            Repaint();
        }

        /// <summary>
        /// 清空源图
        /// </summary>
        private void ClearSource()
        {
            if (mWorkTexture)
            {
                DestroyImmediate(mWorkTexture);
            }

            mWorkTexture = null;
            mSourceAsset = null;
            mWorkPath = "";
            mCellColors = null;
            mCellIndices = null;
            mCounts = null;

            if (mBoardTexture)
            {
                DestroyImmediate(mBoardTexture);
            }

            mBoardTexture = null;
        }

        /// <summary>
        /// 从文件字节载入纹理（绕开纹理的 Read/Write 设置）
        /// </summary>
        /// <param name="absolutePath">绝对路径</param>
        /// <returns>纹理</returns>
        private static Texture2D LoadTextureFromBytes(string absolutePath)
        {
            try
            {
                if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                {
                    return null;
                }

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (texture.LoadImage(File.ReadAllBytes(absolutePath)))
                {
                    return texture;
                }

                DestroyImmediate(texture);
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 实际纵向格数
        /// </summary>
        private int ActualGridHeight
        {
            get
            {
                if (mLockAspect && mWorkTexture && mWorkTexture.width > 0)
                {
                    return Mathf.Max(2, Mathf.RoundToInt(mGridWidth * (float)mWorkTexture.height / mWorkTexture.width));
                }

                return mGridHeight;
            }
        }

        /// <summary>
        /// Color 列表转 Color32 列表
        /// </summary>
        /// <param name="source">颜色列表</param>
        /// <returns>Color32 列表</returns>
        private static List<Color32> ToColor32List(List<Color> source)
        {
            var result = new List<Color32>(source.Count);
            foreach (var color in source)
            {
                result.Add(color);
            }

            return result;
        }

        /// <summary>
        /// 过滤非法文件名
        /// </summary>
        /// <param name="name">文件名</param>
        /// <returns>安全文件名</returns>
        private static string SafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "level";
            }

            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                if (name.IndexOf(invalid) >= 0)
                {
                    return "level";
                }
            }

            return name;
        }

        /// <summary>
        /// 转绝对路径
        /// </summary>
        /// <param name="assetPath">Assets 相对路径</param>
        /// <returns>绝对路径</returns>
        private static string ToAbsolute(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return Application.dataPath;
            }

            if (assetPath.StartsWith("Assets"))
            {
                assetPath = assetPath.Replace('/', Path.DirectorySeparatorChar);
            }

            if (Path.IsPathRooted(assetPath))
            {
                return assetPath;
            }

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        /// <summary>
        /// 建目录
        /// </summary>
        /// <param name="directory">绝对路径</param>
        /// <param name="requireInAssets">是否必须位于 Assets 内</param>
        /// <returns>是否可以写入</returns>
        private static bool EnsureDirectory(string directory, bool requireInAssets)
        {
            if (requireInAssets)
            {
                var assets = Path.GetFullPath(Application.dataPath);
                if (!directory.StartsWith(assets))
                {
                    return false;
                }
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return true;
        }
    }
}
