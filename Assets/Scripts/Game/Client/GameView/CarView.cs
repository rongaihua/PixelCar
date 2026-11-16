//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-11-02
// Desc: 单辆车显示（UGUI Image + 中灰模板换色）
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCar.Client
{
    /// <summary>
    /// 单辆车显示。车身用素材包的中灰模板按车辆颜色实时换色（见 <see cref="CarSpriteFactory"/>），
    /// 箭头、车窗、描边都保留在模板里，不再单独叠箭头。
    ///
    /// 显示层是 UGUI：一个 <see cref="Image"/> 挂在世界空间画布上，用 preserveAspect
    /// 按精灵比例缩放，不拉伸。
    /// </summary>
    public class CarView
    {
        private const float FadeDuration = 0.22f;
        private const float FadeMinAlpha = 0.12f;

        private readonly RectTransform mRoot;
        private readonly Image mBody;
        private readonly RectTransform mStream;
        private readonly Image mStreamImage;
        private readonly Color mColor;
        private readonly ECarSize mSize;

        private Vector2 mSpriteSize = Vector2.one;
        private bool mOnLane;
        private float mLastX;
        private float mFade;

        /// <summary>
        /// 车辆数据
        /// </summary>
        public CarState Car { get; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="car">车辆数据</param>
        /// <param name="carParent">车辆层</param>
        /// <param name="streamParent">色带层</param>
        /// <param name="color">车身颜色</param>
        public CarView(CarState car, Transform carParent, Transform streamParent, Color color)
        {
            Car = car;
            mColor = color;
            mSize = SizeOf(car);

            mRoot = GameCanvas.NewRect("Car", carParent);
            mBody = mRoot.gameObject.AddComponent<Image>();
            mBody.sprite = null;
            mBody.type = Image.Type.Simple;
            mBody.preserveAspect = true;
            mBody.raycastTarget = false;
            mBody.color = Color.white;

            mStream = GameCanvas.NewRect("Stream", streamParent);
            mStreamImage = mStream.gameObject.AddComponent<Image>();
            mStreamImage.sprite = null;
            mStreamImage.raycastTarget = false;
            mStreamImage.enabled = false;

            ApplyDirection(false);
        }

        /// <summary>
        /// 当前世界坐标
        /// </summary>
        public Vector3 Position => mRoot ? mRoot.position : Vector3.zero;

        /// <summary>
        /// 切换停车场 / 车道上的朝向
        /// </summary>
        /// <param name="onLane">是否在车道上（车道朝右，停车场朝上）</param>
        public void SetOnLane(bool onLane)
        {
            if (mOnLane == onLane && mBody.sprite)
            {
                return;
            }

            ApplyDirection(onLane);
        }

        /// <summary>
        /// 更新绕回入口的淡入，返回当前不透明度
        /// </summary>
        /// <param name="x">当前车道坐标</param>
        /// <param name="boardWidth">盘面宽度</param>
        /// <param name="deltaTime">帧间隔</param>
        /// <returns>不透明度</returns>
        public float AdvanceFade(float x, float boardWidth, float deltaTime)
        {
            // 从右边开出、再从左边驶入：位置会突然往回跳，用一次淡入标出来
            if (mFade <= 0f && x < mLastX - boardWidth * 0.5f)
            {
                mFade = FadeDuration;
            }

            mLastX = x;
            if (mFade > 0f)
            {
                mFade = MathUtil.MaxFloat(0f, mFade - deltaTime);
            }

            if (mFade <= 0f)
            {
                return 1f;
            }

            return FadeMinAlpha + (1f - FadeMinAlpha) * (1f - mFade / FadeDuration);
        }

        /// <summary>
        /// 把车放进一个长宽盒子里，按精灵比例缩放，不拉伸。
        /// </summary>
        /// <param name="center">车身中心</param>
        /// <param name="along">车头方向上的长度</param>
        /// <param name="across">垂直车头方向的宽度</param>
        /// <param name="vertical">车头是否朝上或朝下</param>
        public void SetFootprint(Vector3 center, float along, float across, bool vertical)
        {
            var spriteAlong = vertical ? mSpriteSize.y : mSpriteSize.x;
            var spriteAcross = vertical ? mSpriteSize.x : mSpriteSize.y;
            var scale = MathUtil.MinFloat(
                along / MathUtil.MaxFloat(0.0001f, spriteAlong),
                across / MathUtil.MaxFloat(0.0001f, spriteAcross));

            GameCanvas.SetLocal(
                mRoot,
                GameCanvas.ToLocal(mRoot.parent, new Vector2(center.x, center.y)),
                mSpriteSize * scale);
        }

        /// <summary>
        /// 吸色时从车顶拉一条色带到像素图底边。不吸时关掉。
        /// </summary>
        /// <param name="visible">是否在吸</param>
        /// <param name="topY">像素图底边的世界高度</param>
        /// <param name="color">色带颜色</param>
        public void SetStream(bool visible, float topY, Color color)
        {
            if (mStreamImage.enabled != visible)
            {
                mStreamImage.enabled = visible;
            }

            if (!visible)
            {
                return;
            }

            var center = mRoot.position;
            var height = MathUtil.MaxFloat(0.05f, topY - center.y);
            GameCanvas.Place(
                mStream,
                new Vector2(center.x, center.y + height * 0.5f),
                new Vector2(0.14f, height));

            color.a = 0.8f;
            mStreamImage.color = color;
        }

        /// <summary>
        /// 设置整体色调。车身颜色已经在精灵里，这里只做压暗之类的整体调整。
        /// </summary>
        /// <param name="tint">乘算颜色，正常传白色</param>
        /// <param name="alpha">不透明度</param>
        public void SetTint(Color tint, float alpha)
        {
            tint.a = alpha;
            mBody.color = tint;
        }

        /// <summary>
        /// 点选命中测试
        /// </summary>
        /// <param name="world">世界坐标</param>
        /// <returns>是否命中</returns>
        public bool Hit(Vector3 world)
        {
            var local = mRoot.InverseTransformPoint(world);
            var bounds = mRoot.rect;
            bounds.xMin -= 0.06f;
            bounds.xMax += 0.06f;
            bounds.yMin -= 0.06f;
            bounds.yMax += 0.06f;
            return bounds.Contains(new Vector2(local.x, local.y));
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Destroy()
        {
            if (mRoot)
            {
                Object.Destroy(mRoot.gameObject);
            }

            if (mStream)
            {
                Object.Destroy(mStream.gameObject);
            }
        }

        /// <summary>
        /// 应用朝向对应的模板
        /// </summary>
        /// <param name="onLane">是否在车道上</param>
        private void ApplyDirection(bool onLane)
        {
            mOnLane = onLane;
            var dir = onLane ? ECarDir.Right : Car.Dir;
            var sprite = CarSpriteFactory.Get(mSize, dir, mColor);

            // 素材缺失时 sprite 为空；Image 空精灵会画成白块，所以直接关掉
            mBody.enabled = sprite != null;
            if (!sprite)
            {
                return;
            }

            mBody.sprite = sprite;
            mSpriteSize = sprite.bounds.size;
        }

        /// <summary>
        /// 按载量判尺寸
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>尺寸</returns>
        private static ECarSize SizeOf(CarState car)
        {
            if (car.Capacity <= 60)
            {
                return ECarSize.Small;
            }

            if (car.Capacity <= 120)
            {
                return ECarSize.Mid;
            }

            return ECarSize.Big;
        }
    }
}
