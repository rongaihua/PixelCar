//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-29
// Desc: 单辆车显示（中灰模板换色 + 装载条）
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 单辆车显示。车身用素材包的中灰模板按车辆颜色实时换色，
    /// 箭头、车窗、描边都保留在模板里，不再单独叠箭头。
    /// </summary>
    public class CarView
    {
        private const float FadeDuration = 0.22f;
        private const float FadeMinAlpha = 0.12f;

        private readonly GameObject mRoot;
        private readonly SpriteRenderer mBody;
        private readonly Transform mStreamTransform;
        private readonly SpriteRenderer mStream;
        private readonly Color mColor;
        private readonly ECarSize mSize;

        private Vector3 mSpriteSize = Vector3.one;
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
        /// <param name="parent">父节点</param>
        /// <param name="color">车身颜色</param>
        public CarView(CarState car, Transform parent, Color color)
        {
            Car = car;
            mColor = color;
            mSize = SizeOf(car);

            mRoot = new GameObject("Car");
            mRoot.transform.SetParent(parent, false);

            mBody = mRoot.AddComponent<SpriteRenderer>();
            mBody.color = Color.white;
            mBody.sortingOrder = 10;

            var streamObject = new GameObject("Stream");
            streamObject.transform.SetParent(mRoot.transform, false);
            mStream = streamObject.AddComponent<SpriteRenderer>();
            mStream.sprite = SpriteLibrary.BodySprite;
            mStream.sortingOrder = 8;
            mStream.enabled = false;
            mStreamTransform = streamObject.transform;

            ApplyDirection(false);
        }

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

            mRoot.transform.position = center;
            mRoot.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>
        /// 吸色时从车顶拉一条色带到像素图底边。不吸时关掉。
        /// </summary>
        /// <param name="visible">是否在吸</param>
        /// <param name="topY">像素图底边的世界高度</param>
        /// <param name="color">色带颜色</param>
        public void SetStream(bool visible, float topY, Color color)
        {
            mStream.enabled = visible;
            if (!visible)
            {
                return;
            }

            var scale = MathUtil.MaxFloat(0.0001f, mRoot.transform.localScale.y);
            var center = mRoot.transform.position;
            var height = MathUtil.MaxFloat(0.05f, topY - center.y);
            mStreamTransform.localPosition = new Vector3(0f, height * 0.5f / scale, 0f);
            mStreamTransform.localScale = new Vector3(0.14f / scale, height / scale, 1f);
            color.a = 0.8f;
            mStream.color = color;
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
            var bounds = mBody.bounds;
            bounds.Expand(0.06f);
            return bounds.Contains(new Vector3(world.x, world.y, bounds.center.z));
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void Destroy()
        {
            Object.Destroy(mRoot);
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
