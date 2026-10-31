//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-31
// Desc: 底部道具栏（磁铁 / 钩子 / 刷新）
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 底部道具栏。纯展示，点击先只打日志，后续接道具逻辑。
    /// </summary>
    public class BoosterBar : MonoBehaviour
    {
        private static readonly string[] sLabels = { "磁铁", "钩子", "刷新" };

        private GUIStyle mLabelStyle;
        private GUIStyle mPlusStyle;
        private float mStyleScale = -1f;

        /// <summary>
        /// 绘制。
        /// </summary>
        private void OnGUI()
        {
            var scale = MathUtil.Clamp(Screen.height / 900f, 0.75f, 2.2f);
            EnsureStyles(scale);

            var barHeight = 96f * scale;
            var gap = 14f * scale;
            var margin = 18f * scale;
            var width = (Screen.width - margin * 2f - gap * 2f) / 3f;
            var top = Screen.height - barHeight - margin;

            for (var i = 0; i < sLabels.Length; ++i)
            {
                var rect = new Rect(margin + i * (width + gap), top, width, barHeight);

                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.93f, 0.94f, 0.97f, 0.96f);
                GUI.Box(rect, "");
                GUI.backgroundColor = old;

                // 图标占位
                var iconSize = barHeight * 0.46f;
                var iconRect = new Rect(rect.x + (rect.width - iconSize) * 0.5f, rect.y + barHeight * 0.12f, iconSize, iconSize);
                var oldColor = GUI.color;
                GUI.color = new Color(0.62f, 0.72f, 0.88f, 1f);
                GUI.DrawTexture(iconRect, Texture2D.whiteTexture);
                GUI.color = oldColor;

                GUI.Label(new Rect(rect.x, rect.y + barHeight * 0.62f, rect.width, barHeight * 0.3f), sLabels[i], mLabelStyle);

                // 右上角绿色加号
                var plusSize = barHeight * 0.28f;
                var plusRect = new Rect(rect.x + rect.width - plusSize * 0.95f, rect.y - plusSize * 0.22f, plusSize, plusSize);
                var oldBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.48f, 0.80f, 0.18f, 1f);
                GUI.Box(plusRect, "+", mPlusStyle);
                GUI.backgroundColor = oldBg;
            }
        }

        /// <summary>
        /// 按分辨率构建样式。
        /// </summary>
        /// <param name="scale">缩放</param>
        private void EnsureStyles(float scale)
        {
            if (MathUtil.MaxFloat(mStyleScale, scale) - mStyleScale < 0.01f)
            {
                return;
            }

            mStyleScale = scale;

            mLabelStyle = new GUIStyle(GUI.skin.label);
            mLabelStyle.fontSize = MathUtil.RoundToInt(15f * scale);
            mLabelStyle.alignment = TextAnchor.MiddleCenter;
            mLabelStyle.normal.textColor = new Color(0.28f, 0.30f, 0.36f);

            mPlusStyle = new GUIStyle(GUI.skin.box);
            mPlusStyle.fontSize = MathUtil.RoundToInt(16f * scale);
            mPlusStyle.alignment = TextAnchor.MiddleCenter;
            mPlusStyle.fontStyle = FontStyle.Bold;
            mPlusStyle.normal.textColor = Color.white;
        }
    }
}
