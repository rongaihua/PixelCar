//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-11-02
// Desc: 游戏画面的 UGUI 画布
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace PixelCar.Client
{
    /// <summary>
    /// 游戏画面的 UGUI 画布。用世界空间渲染：画布挂在世界原点，一个画布单位就是
    /// 一个世界单位，所以摆元件时可以直接用世界坐标，相机取景和点击换算都不用改。
    ///
    /// 画面里所有元件都是纯色 Image（sprite 留空，Image 会拿内置白图直接刷满矩形），
    /// 不生成也不加载任何贴图。层次从下到上：底图 → 色块盘 → 装色色带 → 车辆。
    ///
    /// 摆放一律走 <see cref="SetLocal"/>：坐标是「父节点局部坐标」，内部写的是
    /// localPosition，不受父矩形尺寸和轴点影响。
    /// </summary>
    public class GameCanvas
    {
        /// <summary>
        /// 画布矩形的边长。元件可以画到矩形外面，不会被裁掉。
        /// </summary>
        private const float Extent = 200f;

        /// <summary>
        /// 画布本体
        /// </summary>
        public Canvas Canvas { get; }

        /// <summary>
        /// 画布根节点
        /// </summary>
        public RectTransform Root { get; }

        /// <summary>
        /// 停车场铺装、盘面衬底、车道这些底图
        /// </summary>
        public RectTransform Background { get; }

        /// <summary>
        /// 色块盘
        /// </summary>
        public RectTransform Board { get; }

        /// <summary>
        /// 装色时从车顶拉下来的色带
        /// </summary>
        public RectTransform Streams { get; }

        /// <summary>
        /// 车辆
        /// </summary>
        public RectTransform Cars { get; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="camera">主相机</param>
        public GameCanvas(Transform parent, Camera camera)
        {
            var gameObject = new GameObject("GameCanvas", typeof(RectTransform), typeof(Canvas));
            Root = (RectTransform)gameObject.transform;
            Root.SetParent(parent, false);

            Canvas = gameObject.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.WorldSpace;
            Canvas.worldCamera = camera;
            Canvas.sortingOrder = 0;

            Root.anchorMin = Vector2.zero;
            Root.anchorMax = Vector2.zero;
            Root.pivot = Vector2.zero;
            Root.sizeDelta = new Vector2(Extent, Extent);
            Root.localPosition = Vector3.zero;
            Root.localRotation = Quaternion.identity;
            Root.localScale = Vector3.one;

            Background = NewRect("Background", Root);
            Board = NewRect("Board", Root);
            Streams = NewRect("Streams", Root);
            Cars = NewRect("Cars", Root);
        }

        /// <summary>
        /// 建一个空矩形节点：锚点固定在父节点左下角，轴点在中心。
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="parent">父节点</param>
        /// <returns>矩形</returns>
        public static RectTransform NewRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localPosition = Vector3.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        /// <summary>
        /// 建一个纯色四边形。sprite 留空时 Image 走 Graphic 的默认四边形，
        /// 用内置白图乘上 color 画，所以不需要任何贴图资源。
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="color">颜色</param>
        /// <returns>图片</returns>
        public static Image NewImage(string name, Transform parent, Color color)
        {
            var rect = NewRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = null;
            image.raycastTarget = false;
            image.color = color;
            return image;
        }

        /// <summary>
        /// 建一个纯色四边形，只要矩形（颜色之后不再改时用这个）
        /// </summary>
        /// <param name="name">名字</param>
        /// <param name="parent">父节点</param>
        /// <param name="color">颜色</param>
        /// <returns>矩形</returns>
        public static RectTransform NewQuad(string name, Transform parent, Color color)
        {
            return NewImage(name, parent, color).rectTransform;
        }

        /// <summary>
        /// 按中心点和尺寸摆放，坐标是世界坐标。
        /// </summary>
        /// <param name="rect">目标</param>
        /// <param name="worldCenter">中心点世界坐标</param>
        /// <param name="size">世界尺寸</param>
        public static void Place(RectTransform rect, Vector2 worldCenter, Vector2 size)
        {
            SetLocal(rect, ToLocal(rect.parent, worldCenter), size);
        }

        /// <summary>
        /// 按中心点和尺寸摆放，坐标已经是父节点的局部坐标。
        /// 这里写 localPosition 而不是 anchoredPosition：anchoredPosition 是相对父矩形
        /// 某个角算的，父节点一有尺寸就会整体偏掉，localPosition 直接就是局部坐标。
        /// </summary>
        /// <param name="rect">目标</param>
        /// <param name="position">中心点局部坐标</param>
        /// <param name="size">尺寸</param>
        public static void SetLocal(RectTransform rect, Vector2 position, Vector2 size)
        {
            var current = rect.localPosition;
            if (current.x != position.x || current.y != position.y)
            {
                rect.localPosition = new Vector3(position.x, position.y, 0f);
            }

            if (rect.sizeDelta != size)
            {
                rect.sizeDelta = size;
            }
        }

        /// <summary>
        /// 世界坐标换成父节点的局部坐标，可以直接交给 <see cref="SetLocal"/>。
        /// </summary>
        /// <param name="parent">父节点</param>
        /// <param name="world">世界坐标</param>
        /// <returns>局部坐标</returns>
        public static Vector2 ToLocal(Transform parent, Vector2 world)
        {
            if (!parent)
            {
                return world;
            }

            return parent.InverseTransformPoint(new Vector3(world.x, world.y, 0f));
        }
    }
}
