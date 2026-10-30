//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: HUD 与失败/过关弹窗
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// HUD 与失败/过关弹窗
    /// </summary>
    public class HudWindow : MonoBehaviour
    {
        private MainModule mModule;
        private GUIStyle mTitleStyle;
        private GUIStyle mLabelStyle;
        private GUIStyle mOverlayTitleStyle;
        private GUIStyle mOverlayBodyStyle;
        private float mStyleScale = -1f;
        private bool mRestartRequested;

        /// <summary>
        /// 绑定主模块
        /// </summary>
        /// <param name="module">主模块</param>
        public void Bind(MainModule module)
        {
            mModule = module;
        }

        /// <summary>
        /// 每帧
        /// </summary>
        private void Update()
        {
            if (!mRestartRequested)
            {
                return;
            }

            mRestartRequested = false;
            if (mModule != null)
            {
                mModule.Restart();
            }
        }

        /// <summary>
        /// 绘制
        /// </summary>
        private void OnGUI()
        {
            if (mModule == null || mModule.Sim == null)
            {
                return;
            }

            var scale = MathUtil.Clamp(Screen.height / 900f, 0.75f, 2f);
            EnsureStyles(scale);
            DrawHud(scale);
            DrawOverlay(scale);
        }

        /// <summary>
        /// 按分辨率缩放构建样式
        /// </summary>
        /// <param name="scale">缩放</param>
        private void EnsureStyles(float scale)
        {
            if (MathUtil.MaxFloat(mStyleScale, scale) - mStyleScale < 0.01f)
            {
                return;
            }

            mStyleScale = scale;

            mTitleStyle = new GUIStyle(GUI.skin.label);
            mTitleStyle.fontSize = MathUtil.RoundToInt(20f * scale);
            mTitleStyle.normal.textColor = Color.white;

            mLabelStyle = new GUIStyle(GUI.skin.label);
            mLabelStyle.fontSize = MathUtil.RoundToInt(15f * scale);
            mLabelStyle.normal.textColor = new Color(0.8f, 0.85f, 0.95f);

            mOverlayTitleStyle = new GUIStyle(GUI.skin.label);
            mOverlayTitleStyle.fontSize = MathUtil.RoundToInt(34f * scale);
            mOverlayTitleStyle.alignment = TextAnchor.MiddleCenter;

            mOverlayBodyStyle = new GUIStyle(GUI.skin.label);
            mOverlayBodyStyle.fontSize = MathUtil.RoundToInt(17f * scale);
            mOverlayBodyStyle.alignment = TextAnchor.MiddleCenter;
            mOverlayBodyStyle.wordWrap = true;
            mOverlayBodyStyle.normal.textColor = Color.white;
        }

        /// <summary>
        /// 顶部条与图例
        /// </summary>
        /// <param name="scale">缩放</param>
        private void DrawHud(float scale)
        {
            var board = mModule.Sim.Board;
            var taken = mModule.InitialCells - board.TotalRemaining;

            var text = $"车道 {mModule.Sim.Lane.Count}/{mModule.LaneCapacity}    剩余 {board.TotalRemaining} 格    已取 {100f * taken / mModule.InitialCells:F0}%";
            GUI.Label(new Rect(12f * scale, 8f * scale, 720f * scale, 26f * scale), text, mTitleStyle);

            var y = 32f * scale;
            for (var i = 0; i < mModule.Palette.Length; ++i)
            {
                var x = 12f * scale + i * 96f * scale;
                GUI.color = mModule.Palette[i];
                GUI.DrawTexture(new Rect(x, y, 16f * scale, 16f * scale), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(x + 22f * scale, y - 3f * scale, 72f * scale, 20f * scale), board.Remaining[i].ToString(), mLabelStyle);
            }

            if (mModule.MessageTimer > 0f)
            {
                GUI.Label(new Rect(12f * scale, Screen.height - 46f * scale, Screen.width - 24f * scale, 26f * scale), mModule.Message, mLabelStyle);
            }

            GUI.Label(new Rect(12f * scale, Screen.height - 26f * scale, Screen.width - 24f * scale, 22f * scale), "点下面的车 → 放进车道；点车道上的车 → 移除（调试）", mLabelStyle);
        }

        /// <summary>
        /// 失败 / 过关弹窗
        /// </summary>
        /// <param name="scale">缩放</param>
        private void DrawOverlay(float scale)
        {
            if (!mModule.Failed && !mModule.Cleared)
            {
                return;
            }

            var width = 470f * scale;
            var height = 262f * scale;
            var rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            var oldColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            GUI.Box(rect, "");

            mOverlayTitleStyle.normal.textColor = mModule.Cleared ? new Color(0.62f, 0.95f, 0.62f) : new Color(1f, 0.5f, 0.45f);
            GUI.Label(new Rect(rect.x + 16f * scale, rect.y + 16f * scale, width - 32f * scale, 46f * scale), mModule.Cleared ? "过关！" : "本关失败", mOverlayTitleStyle);

            var body = BuildOverlayBody();
            GUI.Label(new Rect(rect.x + 28f * scale, rect.y + 72f * scale, width - 56f * scale, 104f * scale), body, mOverlayBodyStyle);

            var buttonWidth = 190f * scale;
            var buttonHeight = 46f * scale;
            var buttonRect = new Rect(rect.x + (width - buttonWidth) * 0.5f, rect.y + height - buttonHeight - 24f * scale, buttonWidth, buttonHeight);
            if (GUI.Button(buttonRect, "重开本关"))
            {
                ButtonSoundListener.PlayClick();
                mRestartRequested = true;
            }
        }

        /// <summary>
        /// 弹窗正文
        /// </summary>
        /// <returns>正文</returns>
        private string BuildOverlayBody()
        {
            if (mModule.Cleared)
            {
                return $"全部色块都运走了，共 {mModule.Sim.CarsCompleted} 辆车。";
            }

            var board = mModule.Sim.Board;
            var taken = mModule.InitialCells - board.TotalRemaining;
            var percent = 100f * taken / MathUtil.Max(1, mModule.InitialCells);
            return $"车道上的车都等不到自己的颜色，待命区也没有车能拉到色块。\n\n已取 {percent:F0}%　还剩 {board.TotalRemaining} 格";
        }
    }
}
