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
        private static readonly string[] sPercents = BuildPercents();

        private GUIStyle mLabelStyle;
        private GUIStyle mBadgeStyle;
        private GUIStyle mPercentStyle;
        private GUIStyle mOverlayTitleStyle;
        private GUIStyle mOverlayBodyStyle;
        private float mStyleScale = -1f;
        private bool mRestartRequested;
        private string mBadgeText = "0/5";
        private int mBadgeCount = -1;
        private int mBadgeCapacity = -1;

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

            mLabelStyle = new GUIStyle(GUI.skin.label);
            mLabelStyle.fontSize = MathUtil.RoundToInt(15f * scale);
            mLabelStyle.normal.textColor = new Color(0.8f, 0.85f, 0.95f);

            mBadgeStyle = new GUIStyle(GUI.skin.box);
            mBadgeStyle.fontSize = MathUtil.RoundToInt(22f * scale);
            mBadgeStyle.alignment = TextAnchor.MiddleCenter;
            mBadgeStyle.fontStyle = FontStyle.Bold;
            mBadgeStyle.normal.textColor = Color.white;

            mPercentStyle = new GUIStyle(GUI.skin.label);
            mPercentStyle.fontSize = MathUtil.RoundToInt(14f * scale);
            mPercentStyle.alignment = TextAnchor.MiddleCenter;
            mPercentStyle.fontStyle = FontStyle.Bold;
            mPercentStyle.normal.textColor = new Color(1f, 0.96f, 0.86f);

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
        /// 车道名额和每辆车上的百分比
        /// </summary>
        /// <param name="scale">缩放</param>
        private void DrawHud(float scale)
        {
            var camera = Camera.main;
            if (!camera)
            {
                return;
            }

            DrawBadge(camera, scale);
            DrawPercents(camera, scale);

            if (mModule.MessageTimer > 0f)
            {
                GUI.Label(new Rect(12f * scale, Screen.height - 36f * scale, Screen.width - 24f * scale, 26f * scale), mModule.Message, mLabelStyle);
            }
        }

        /// <summary>
        /// 路中央的 n/5
        /// </summary>
        /// <param name="camera">主相机</param>
        /// <param name="scale">缩放</param>
        private void DrawBadge(Camera camera, float scale)
        {
            var screen = camera.WorldToScreenPoint(mModule.BadgeWorld);
            if (screen.z < 0f)
            {
                return;
            }

            var width = 92f * scale;
            var height = 36f * scale;
            var rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - height * 0.5f, width, height);
            var old = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.18f, 0.45f, 0.86f, 0.94f);
            GUI.Box(rect, BadgeText(), mBadgeStyle);
            GUI.backgroundColor = old;
        }

        /// <summary>
        /// 车道上每辆车下面的装载百分比
        /// </summary>
        /// <param name="camera">主相机</param>
        /// <param name="scale">缩放</param>
        private void DrawPercents(Camera camera, float scale)
        {
            var lane = mModule.Sim.Lane;
            var width = 64f * scale;
            var height = 22f * scale;
            for (var i = 0; i < lane.Count; ++i)
            {
                var car = lane[i];
                if (car.Phase == ECarPhase.Exiting)
                {
                    continue;
                }

                var screen = camera.WorldToScreenPoint(mModule.LanePercentWorld(car));
                if (screen.z < 0f)
                {
                    continue;
                }

                var percent = MathUtil.Clamp(MathUtil.RoundToInt(car.Fill * 100f), 0, 100);
                var rect = new Rect(screen.x - width * 0.5f, Screen.height - screen.y - height * 0.5f, width, height);
                GUI.Label(rect, sPercents[percent], mPercentStyle);
            }
        }

        /// <summary>
        /// 名额文字。人数或容量变了才重新拼。
        /// </summary>
        /// <returns>例如 2/5</returns>
        private string BadgeText()
        {
            var count = mModule.Sim.Lane.Count;
            var capacity = mModule.LaneCapacity;
            if (count == mBadgeCount && capacity == mBadgeCapacity)
            {
                return mBadgeText;
            }

            mBadgeCount = count;
            mBadgeCapacity = capacity;
            mBadgeText = count.ToString() + "/" + capacity.ToString();
            return mBadgeText;
        }

        /// <summary>
        /// 0% 到 100% 的文字，绘制时不再拼字符串。
        /// </summary>
        /// <returns>百分比文字</returns>
        private static string[] BuildPercents()
        {
            var values = new string[101];
            for (var i = 0; i < values.Length; ++i)
            {
                values[i] = i.ToString() + "%";
            }

            return values;
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
