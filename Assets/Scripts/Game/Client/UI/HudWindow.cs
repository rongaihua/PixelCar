//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-31
// Desc: 游戏内 HUD（UGUI，Update 驱动）
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using PixelCar.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCar.Client
{
    /// <summary>
    /// 游戏内 HUD。全部用 UGUI 搭，每帧在 <see cref="Update"/> 里刷新，
    /// 不再使用 OnGUI。包含：顶部状态、车道徽章、装载百分比、底部道具栏、提示与结算弹窗。
    /// </summary>
    public class HudWindow : MonoBehaviour
    {
        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;
        private const int PercentPoolSize = 20;
        private const float PercentRise = 0.55f;

        private static readonly string[] sBoosterLabels = { "磁铁", "钩子", "刷新" };

        private readonly List<Text> mPercentPool = new List<Text>();
        private readonly List<RectTransform> mBoosterCards = new List<RectTransform>();

        private MainModule mModule;
        private Camera mCamera;
        private Font mFont;
        private RectTransform mCanvasRect;

        private Text mStatusText;
        private RectTransform mBadgeRect;
        private Text mBadgeText;
        private Text mMessageText;
        private GameObject mOverlay;
        private Text mOverlayTitle;
        private Text mOverlayBody;
        private bool mRestartRequested;

        /// <summary>
        /// 绑定主模块并搭好界面。
        /// </summary>
        /// <param name="module">主模块</param>
        /// <param name="camera">主相机，用于世界坐标转屏幕</param>
        public void Bind(MainModule module, Camera camera)
        {
            mModule = module;
            mCamera = camera;
            mFont = Resources.Load<Font>("PixelCar/fonts/game");
            if (!mFont)
            {
                mFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            BuildCanvas();
            BuildStatus();
            BuildBadge();
            BuildPercents();
            BuildBoosters();
            BuildMessage();
            BuildOverlay();
        }

        /// <summary>
        /// 每帧刷新。
        /// </summary>
        private void Update()
        {
            if (mModule == null || mModule.Sim == null)
            {
                return;
            }

            if (mRestartRequested)
            {
                mRestartRequested = false;
                mModule.Restart();
                return;
            }

            RefreshStatus();
            RefreshBadge();
            RefreshPercents();
            RefreshMessage();
            RefreshOverlay();
        }

        /// <summary>
        /// 建画布。
        /// </summary>
        private void BuildCanvas()
        {
            var canvasObject = new GameObject("HudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
            scaler.matchWidthOrHeight = 1f;

            mCanvasRect = (RectTransform)canvasObject.transform;
        }

        /// <summary>
        /// 顶部状态行。
        /// </summary>
        private void BuildStatus()
        {
            mStatusText = CreateText("Status", mCanvasRect, 30, TextAnchor.UpperLeft, Color.white);
            var rect = mStatusText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(760f, 60f);
            rect.anchoredPosition = new Vector2(24f, -20f);
        }

        /// <summary>
        /// 车道数量徽章。
        /// </summary>
        private void BuildBadge()
        {
            var panel = CreatePanel("Badge", mCanvasRect, new Color(0.18f, 0.45f, 0.86f, 0.95f));
            mBadgeRect = panel.rectTransform;
            mBadgeRect.sizeDelta = new Vector2(132f, 54f);

            mBadgeText = CreateText("BadgeText", mBadgeRect, 30, TextAnchor.MiddleCenter, Color.white);
            Stretch(mBadgeText.rectTransform);
        }

        /// <summary>
        /// 百分比文字池。
        /// </summary>
        private void BuildPercents()
        {
            for (var i = 0; i < PercentPoolSize; ++i)
            {
                var text = CreateText($"Percent{i}", mCanvasRect, 26, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.86f));
                text.rectTransform.sizeDelta = new Vector2(120f, 40f);
                text.gameObject.SetActive(false);
                mPercentPool.Add(text);
            }
        }

        /// <summary>
        /// 底部道具栏。
        /// </summary>
        private void BuildBoosters()
        {
            for (var i = 0; i < sBoosterLabels.Length; ++i)
            {
                var card = CreatePanel($"Booster{i}", mCanvasRect, new Color(0.93f, 0.94f, 0.97f, 0.96f));
                var rect = card.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(300f, 170f);
                rect.anchoredPosition = new Vector2(30f + i * 330f, 34f);
                mBoosterCards.Add(rect);

                var label = CreateText("Label", rect, 30, TextAnchor.LowerCenter, new Color(0.28f, 0.30f, 0.36f));
                label.text = sBoosterLabels[i];
                label.rectTransform.anchorMin = new Vector2(0f, 0f);
                label.rectTransform.anchorMax = new Vector2(1f, 0f);
                label.rectTransform.pivot = new Vector2(0.5f, 0f);
                label.rectTransform.sizeDelta = new Vector2(0f, 50f);
                label.rectTransform.anchoredPosition = new Vector2(0f, 18f);

                var plus = CreatePanel("Plus", rect, new Color(0.48f, 0.80f, 0.18f, 1f));
                var plusRect = plus.rectTransform;
                plusRect.anchorMin = new Vector2(1f, 1f);
                plusRect.anchorMax = new Vector2(1f, 1f);
                plusRect.pivot = new Vector2(1f, 1f);
                plusRect.sizeDelta = new Vector2(46f, 46f);
                plusRect.anchoredPosition = new Vector2(10f, 10f);

                var plusText = CreateText("PlusText", plusRect, 28, TextAnchor.MiddleCenter, Color.white);
                plusText.text = "+";
                Stretch(plusText.rectTransform);
            }
        }

        /// <summary>
        /// 提示行。
        /// </summary>
        private void BuildMessage()
        {
            mMessageText = CreateText("Message", mCanvasRect, 26, TextAnchor.MiddleCenter, Color.white);
            var rect = mMessageText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(900f, 60f);
            rect.anchoredPosition = new Vector2(0f, 230f);
        }

        /// <summary>
        /// 结算弹窗。
        /// </summary>
        private void BuildOverlay()
        {
            var mask = CreatePanel("Overlay", mCanvasRect, new Color(0f, 0f, 0f, 0.62f));
            Stretch(mask.rectTransform);
            mOverlay = mask.gameObject;

            var box = CreatePanel("Box", mask.rectTransform, new Color(0.98f, 0.98f, 0.99f, 1f));
            var boxRect = box.rectTransform;
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(840f, 620f);

            mOverlayTitle = CreateText("Title", boxRect, 60, TextAnchor.UpperCenter, new Color(0.2f, 0.22f, 0.28f));
            var titleRect = mOverlayTitle.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 90f);
            titleRect.anchoredPosition = new Vector2(0f, -50f);

            mOverlayBody = CreateText("Body", boxRect, 32, TextAnchor.UpperCenter, new Color(0.3f, 0.32f, 0.4f));
            var bodyRect = mOverlayBody.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.sizeDelta = new Vector2(-80f, 260f);
            bodyRect.anchoredPosition = new Vector2(0f, -150f);

            var button = CreatePanel("Restart", boxRect, new Color(0.49f, 0.83f, 0.13f, 1f));
            var buttonRect = button.rectTransform;
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(380f, 110f);
            buttonRect.anchoredPosition = new Vector2(0f, 46f);

            var buttonImage = button.gameObject.AddComponent<Button>();
            buttonImage.targetGraphic = button;
            buttonImage.onClick.AddListener(OnRestartClicked);

            var label = CreateText("Label", buttonRect, 40, TextAnchor.MiddleCenter, Color.white);
            label.text = "重开本关";
            Stretch(label.rectTransform);

            mOverlay.SetActive(false);
        }

        /// <summary>
        /// 刷新顶部状态。
        /// </summary>
        private void RefreshStatus()
        {
            var board = mModule.Sim.Board;
            var taken = mModule.InitialCells - board.TotalRemaining;
            var percent = mModule.InitialCells <= 0 ? 0 : MathUtil.RoundToInt(taken * 100f / mModule.InitialCells);
            mStatusText.text = $"车道 {mModule.Sim.Lane.Count}/{mModule.LaneCapacity}    剩余 {board.TotalRemaining} 格    {percent}%";
        }

        /// <summary>
        /// 刷新徽章位置与文字。
        /// </summary>
        private void RefreshBadge()
        {
            mBadgeRect.anchoredPosition = ToCanvas(mModule.BadgeWorld);
            mBadgeText.text = $"{mModule.Sim.Lane.Count}/{mModule.LaneCapacity}";
        }

        /// <summary>
        /// 刷新百分比文字。
        /// </summary>
        private void RefreshPercents()
        {
            var used = 0;

            var lane = mModule.Sim.Lane;
            for (var i = 0; i < lane.Count; ++i)
            {
                var car = lane[i];
                if (car.Phase == ECarPhase.Exiting)
                {
                    continue;
                }

                used = PlacePercent(used, mModule.LanePercentWorld(car), car.Fill);
            }

            var pool = mModule.Sim.Pool;
            for (var i = 0; i < pool.Count; ++i)
            {
                var car = pool[i];
                if (car.Fill <= 0.001f)
                {
                    continue;
                }

                Vector3 world;
                if (!mModule.TryGetCarWorld(car, out world))
                {
                    continue;
                }

                used = PlacePercent(used, world + new Vector3(0f, PercentRise, 0f), car.Fill);
            }

            for (var i = used; i < mPercentPool.Count; ++i)
            {
                if (mPercentPool[i].gameObject.activeSelf)
                {
                    mPercentPool[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 把第 index 个百分比文字摆到指定世界坐标上方。
        /// </summary>
        /// <param name="index">池下标</param>
        /// <param name="world">世界坐标</param>
        /// <param name="fill">进度</param>
        /// <returns>下一个可用下标</returns>
        private int PlacePercent(int index, Vector3 world, float fill)
        {
            if (index >= mPercentPool.Count)
            {
                return index;
            }

            var text = mPercentPool[index];
            text.text = $"{MathUtil.Clamp(MathUtil.RoundToInt(fill * 100f), 0, 100)}%";
            text.rectTransform.anchoredPosition = ToCanvas(world);
            if (!text.gameObject.activeSelf)
            {
                text.gameObject.SetActive(true);
            }

            return index + 1;
        }

        /// <summary>
        /// 刷新提示行。
        /// </summary>
        private void RefreshMessage()
        {
            var visible = mModule.MessageTimer > 0f && !string.IsNullOrEmpty(mModule.Message);
            if (mMessageText.gameObject.activeSelf != visible)
            {
                mMessageText.gameObject.SetActive(visible);
            }

            if (visible)
            {
                mMessageText.text = mModule.Message;
            }
        }

        /// <summary>
        /// 刷新结算弹窗。
        /// </summary>
        private void RefreshOverlay()
        {
            var show = mModule.Failed || mModule.Cleared;
            if (mOverlay.activeSelf != show)
            {
                mOverlay.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            var board = mModule.Sim.Board;
            if (mModule.Cleared)
            {
                mOverlayTitle.text = "过关！";
                mOverlayBody.text = $"全部色块都运走了，共 {mModule.Sim.CarsCompleted} 辆车。";
                return;
            }

            var taken = mModule.InitialCells - board.TotalRemaining;
            var percent = MathUtil.RoundToInt(taken * 100f / MathUtil.Max(1, mModule.InitialCells));
            mOverlayTitle.text = "本关失败";
            mOverlayBody.text = $"车道上的车都等不到自己的颜色，待命区也没有车能拉到色块。\n\n已取 {percent}%　还剩 {board.TotalRemaining} 格";
        }

        /// <summary>
        /// 世界坐标转画布坐标。
        /// </summary>
        /// <param name="world">世界坐标</param>
        /// <returns>画布内的局部坐标</returns>
        private Vector2 ToCanvas(Vector3 world)
        {
            if (!mCamera)
            {
                return Vector2.zero;
            }

            var screen = mCamera.WorldToScreenPoint(world);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(mCanvasRect, screen, null, out local);
            return local;
        }

        /// <summary>
        /// 重开按钮。
        /// </summary>
        private void OnRestartClicked()
        {
            mRestartRequested = true;
        }

        /// <summary>
        /// 建一段文字。
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="size">字号</param>
        /// <param name="anchor">对齐</param>
        /// <param name="color">颜色</param>
        /// <returns>文字</returns>
        private Text CreateText(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);

            var text = gameObject.AddComponent<Text>();
            text.font = mFont;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// 建一张九宫格圆角面板。
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="color">颜色</param>
        /// <returns>图片</returns>
        private static Image CreatePanel(string name, Transform parent, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);

            var image = gameObject.AddComponent<Image>();
            image.sprite = SpriteLibrary.PanelSprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// 铺满父节点。
        /// </summary>
        /// <param name="rect">目标</param>
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
