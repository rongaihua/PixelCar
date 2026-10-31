//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 生成开始界面预制并摆进入口场景
//---------------------------------------------------------------------------------------

using PixelCar.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PixelCar.Editor
{
    /// <summary>
    /// 生成开始界面预制并摆进入口场景
    /// </summary>
    public static class StartWindowBuilder
    {
        /// <summary>
        /// 预制路径
        /// </summary>
        public const string PrefabPath = "Assets/Res/UI/StartWindow.prefab";

        /// <summary>
        /// 界面字体（libra 包内）
        /// </summary>
        public const string FontPath = "Packages/com.rongaihua.my-game-libra/Res/libra_font/AlimamaFangYuanTiVF_Thin.ttf";

        private const string FontPathByFolder = "Packages/my-game-libra/Res/libra_font/AlimamaFangYuanTiVF_Thin.ttf";

        private const string ArtDir = "Assets/Res/UI/Generated";

        private const float Scale = 1080f / 840f;

        private static readonly Color CreamText = new(0.29f, 0.16f, 0.06f);

        /// <summary>
        /// 生成并摆放
        /// </summary>
        [MenuItem("Tools/PixelCar/生成开始界面")]
        public static void Build()
        {
            var prefab = BuildPrefab();
            if (!prefab)
            {
                return;
            }

            AddToEntryScene(prefab);
        }

        /// <summary>
        /// 生成预制
        /// </summary>
        /// <returns>预制对象</returns>
        private static GameObject BuildPrefab()
        {
            EnsureFolder("Assets/Res/UI");

            var window = new GameObject("StartWindow", typeof(RectTransform));
            var root = (RectTransform)window.transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            BuildBackdrop(root);
            BuildTopBar(root);
            var startButton = BuildStartButton(root);

            var view = window.AddComponent<StartWindow>();
            view.LevelText = startButton.GetComponentInChildren<Text>();
            view.StartButton = startButton;
            view.CoinText = FindTextByName(root, "CoinText");
            view.HeartText = FindTextByName(root, "HeartText");
            view.HeartCapacityText = FindTextByName(root, "HeartCapacityText");
            view.TimerText = FindTextByName(root, "TimerText");
            view.PauseButton = FindButton(root, "PauseButton");

            var prefab = PrefabUtility.SaveAsPrefabAsset(window, PrefabPath);
            Object.DestroyImmediate(window);
            AssetDatabase.SaveAssets();
            Debug.Log($"开始界面预制已生成：{PrefabPath}");
            return prefab;
        }

        /// <summary>
        /// 背景（占位图，正式美术替换 backdrop 即可）
        /// </summary>
        /// <param name="root">父节点</param>
        private static void BuildBackdrop(RectTransform root)
        {
            var backdrop = CreateImage("Backdrop", root, LoadSprite("backdrop", UiSpriteFactory.Gradient()), Color.white);
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = Vector2.zero;
            backdrop.rectTransform.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 顶栏（头像 / 体力 / 倒计时 / 金币 / 暂停）
        /// </summary>
        /// <param name="root">父节点</param>
        private static void BuildTopBar(RectTransform root)
        {
            var frame = CreateImage("AvatarFrame", root, LoadSprite("pill_cream", UiSpriteFactory.RoundedRect()), new Color(0.77f, 0.21f, 0.36f));
            frame.type = Image.Type.Sliced;
            PlaceTopLeft(frame.rectTransform, 100f, 92f, 150f, 150f);

            var avatar = CreateImage("Avatar", root, LoadSprite("avatar", UiSpriteFactory.RoundedRect()), Color.white);
            avatar.type = Image.Type.Simple;
            PlaceTopLeft(avatar.rectTransform, 100f, 96f, 116f, 116f);

            var panel = CreateImage("TopPanel", root, LoadSprite("pill_cream", UiSpriteFactory.RoundedRect()), Color.white);
            panel.type = Image.Type.Sliced;
            PlaceTopLeft(panel.rectTransform, 432f, 66f, 470f, 74f);

            var heart = CreateImage("HeartIcon", root, LoadSprite("heart", UiSpriteFactory.Circle()), Color.white);
            PlaceTopLeft(heart.rectTransform, 232f, 66f, 56f, 56f);

            var heartText = CreateText("HeartText", root, "1", 40, CreamText, TextAnchor.MiddleLeft);
            heartText.fontStyle = FontStyle.Bold;
            PlaceTopLeftEdge(heartText.rectTransform, 264f, 68f, 70f, 50f);

            var timerText = CreateText("TimerText", root, "19:12", 34, CreamText, TextAnchor.MiddleCenter);
            timerText.fontStyle = FontStyle.Bold;
            PlaceTopLeft(timerText.rectTransform, 352f, 68f, 120f, 50f);

            var badge = CreateImage("HeartCapacity", root, LoadSprite("badge_red", UiSpriteFactory.Circle()), Color.white);
            PlaceTopLeft(badge.rectTransform, 406f, 30f, 40f, 40f);

            var badgeText = CreateText("HeartCapacityText", root, "2", 24, Color.white, TextAnchor.MiddleCenter);
            badgeText.fontStyle = FontStyle.Bold;
            AddOutline(badgeText.gameObject, new Color(0.59f, 0.10f, 0.07f), 1f);
            PlaceTopLeft(badgeText.rectTransform, 406f, 30f, 40f, 40f);

            var coin = CreateImage("CoinIcon", root, LoadSprite("coin", UiSpriteFactory.Circle()), Color.white);
            PlaceTopLeft(coin.rectTransform, 452f, 66f, 56f, 56f);

            var coinText = CreateText("CoinText", root, "1460", 38, CreamText, TextAnchor.MiddleLeft);
            coinText.fontStyle = FontStyle.Bold;
            PlaceTopLeftEdge(coinText.rectTransform, 488f, 66f, 120f, 50f);

            var plus = CreateImage("CoinPlus", root, LoadSprite("plus_green", UiSpriteFactory.Circle()), Color.white);
            plus.raycastTarget = true;
            PlaceTopLeft(plus.rectTransform, 634f, 100f, 38f, 38f);

            var pause = CreateImage("PauseButton", root, LoadSprite("pause", UiSpriteFactory.Circle()), Color.white);
            pause.raycastTarget = true;
            PlaceTopLeft(pause.rectTransform, 762f, 214f, 92f, 92f);

            var pauseButton = pause.gameObject.AddComponent<Button>();
            pauseButton.targetGraphic = pause;
        }

        /// <summary>
        /// 第 N 关按钮
        /// </summary>
        /// <param name="root">父节点</param>
        /// <returns>按钮</returns>
        private static Button BuildStartButton(RectTransform root)
        {
            var background = CreateImage("StartButton", root, LoadSprite("btn_green", UiSpriteFactory.RoundedRect()), Color.white);
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;
            PlaceBottom(background.rectTransform, 420f, 331f, 520f, 120f);

            var button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;

            var label = CreateText("LevelText", background.transform, "第7关", 60, Color.white, TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            AddOutline(label.gameObject, new Color(0.23f, 0.46f, 0.05f), 3f);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        /// <summary>
        /// 摆进入口场景
        /// </summary>
        /// <param name="prefab">预制</param>
        private static void AddToEntryScene(GameObject prefab)
        {
            // 批处理模式没人可以应答保存提示，直接往下走
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(EntrySceneMenu.EntryScenePath, OpenSceneMode.Single);
            var canvas = FindOrCreateCanvas();
            EnsureEventSystem();

            var existing = Object.FindObjectOfType<StartWindow>();
            if (existing)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
            var view = instance.GetComponent<StartWindow>();

            var entry = Object.FindObjectOfType<GameEntry>();
            if (entry)
            {
                entry.StartWindow = view;
                EditorUtility.SetDirty(entry);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = instance;
            Debug.Log($"开始界面已放入场景：{EntrySceneMenu.EntryScenePath}");
        }

        /// <summary>
        /// 找或建 Canvas
        /// </summary>
        /// <returns>Canvas</returns>
        private static Canvas FindOrCreateCanvas()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas)
            {
                return canvas;
            }

            var canvasObject = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 1f;

            return canvas;
        }

        /// <summary>
        /// 保证有 EventSystem
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>())
            {
                return;
            }

            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// 取 libra 包里的界面字体
        /// </summary>
        /// <returns>字体</returns>
        private static Font LibraFont()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font)
            {
                return font;
            }

            font = AssetDatabase.LoadAssetAtPath<Font>(FontPathByFolder);
            if (font)
            {
                return font;
            }

            Debug.LogWarning($"找不到界面字体 {FontPath}，回退到内置字体（中文会显示成方块）");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        /// <summary>
        /// 载入生成好的界面图，缺图时退回程序化图元
        /// </summary>
        /// <param name="name">图名</param>
        /// <param name="fallback">兜底图</param>
        /// <returns>精灵</returns>
        private static Sprite LoadSprite(string name, Sprite fallback)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtDir}/{name}.png");
            if (sprite)
            {
                return sprite;
            }

            Debug.LogWarning($"缺少界面图 {ArtDir}/{name}.png，使用程序化图元代替");
            return fallback;
        }

        /// <summary>
        /// 给文字加描边
        /// </summary>
        /// <param name="target">目标</param>
        /// <param name="color">描边色</param>
        /// <param name="distance">距离</param>
        private static void AddOutline(GameObject target, Color color, float distance)
        {
            var outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(distance, -distance);
            outline.useGraphicAlpha = true;
        }

        /// <summary>
        /// 建一张图片
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="sprite">精灵</param>
        /// <param name="color">颜色</param>
        /// <returns>图片</returns>
        private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);

            var image = gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>
        /// 建一段文字
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="content">内容</param>
        /// <param name="fontSize">字号</param>
        /// <param name="color">颜色</param>
        /// <param name="anchor">对齐</param>
        /// <returns>文字</returns>
        private static Text CreateText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor anchor)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);

            var text = gameObject.AddComponent<Text>();
            text.text = content;
            text.font = LibraFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        /// <summary>
        /// 按「左上角参考系」摆放
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <param name="x">设计稿中心 x</param>
        /// <param name="y">设计稿中心 y</param>
        /// <param name="width">设计稿宽</param>
        /// <param name="height">设计稿高</param>
        private static void PlaceTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width * Scale, height * Scale);
            rect.anchoredPosition = new Vector2(x * Scale, -y * Scale);
        }

        /// <summary>
        /// 按「左上角参考系」摆放，左边缘对齐
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <param name="left">设计稿左边缘 x</param>
        /// <param name="centerY">设计稿中心 y</param>
        /// <param name="width">设计稿宽</param>
        /// <param name="height">设计稿高</param>
        private static void PlaceTopLeftEdge(RectTransform rect, float left, float centerY, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(width * Scale, height * Scale);
            rect.anchoredPosition = new Vector2(left * Scale, -centerY * Scale);
        }

        /// <summary>
        /// 按「底部居中参考系」摆放
        /// </summary>
        /// <param name="rect">矩形</param>
        /// <param name="x">设计稿中心 x</param>
        /// <param name="y">距底高度（设计稿）</param>
        /// <param name="width">设计稿宽</param>
        /// <param name="height">设计稿高</param>
        private static void PlaceBottom(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width * Scale, height * Scale);
            rect.anchoredPosition = new Vector2((x - 420f) * Scale, y * Scale);
        }

        /// <summary>
        /// 按名字找文字
        /// </summary>
        /// <param name="root">根</param>
        /// <param name="name">名字</param>
        /// <returns>文字</returns>
        private static Text FindTextByName(RectTransform root, string name)
        {
            var found = root.GetComponentsInChildren<Text>(true);
            foreach (var text in found)
            {
                if (text.name == name)
                {
                    return text;
                }
            }

            return null;
        }

        /// <summary>
        /// 按名字找按钮
        /// </summary>
        /// <param name="root">根</param>
        /// <param name="name">名字</param>
        /// <returns>按钮</returns>
        private static Button FindButton(RectTransform root, string name)
        {
            var found = root.GetComponentsInChildren<Button>(true);
            foreach (var button in found)
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            return null;
        }

        /// <summary>
        /// 建资产目录
        /// </summary>
        /// <param name="path">资产路径</param>
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
