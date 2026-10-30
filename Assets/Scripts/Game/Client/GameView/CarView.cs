//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 单辆车显示
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 单辆车显示
    /// </summary>
    public class CarView
    {
        private const float FadeDuration = 0.22f;
        private const float FadeMinAlpha = 0.12f;

        private readonly GameObject mRoot;
        private readonly SpriteRenderer mBody;
        private readonly Transform mFillTransform;
        private readonly SpriteRenderer mFill;

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

            mRoot = new GameObject("Car");
            mRoot.transform.SetParent(parent, false);

            mBody = mRoot.AddComponent<SpriteRenderer>();
            mBody.sprite = SpriteLibrary.BodySprite;
            mBody.color = color;
            mBody.sortingOrder = 10;

            var fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(mRoot.transform, false);
            mFill = fillObject.AddComponent<SpriteRenderer>();
            mFill.sprite = SpriteLibrary.FillSprite;
            mFill.color = new Color(1f, 1f, 1f, 0.5f);
            mFill.sortingOrder = 11;
            mFillTransform = fillObject.transform;
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
        /// 设置位置与尺寸
        /// </summary>
        /// <param name="position">世界坐标</param>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public void SetPose(Vector3 position, float width, float height)
        {
            mRoot.transform.position = position;
            mRoot.transform.localScale = new Vector3(width, height, 1f);
            // 进度条挂在车身上，本地坐标用单位尺寸，避免和父节点缩放乘两次
            mFillTransform.localPosition = new Vector3(-0.5f, 0f, 0f);
        }

        /// <summary>
        /// 设置装载进度条
        /// </summary>
        /// <param name="fill">进度 [0,1]</param>
        /// <param name="alpha">不透明度</param>
        public void SetFill(float fill, float alpha)
        {
            mFillTransform.localScale = new Vector3(MathUtil.MaxFloat(0.001f, fill), 1f, 1f);
            mFill.color = new Color(1f, 1f, 1f, 0.5f * alpha);
        }

        /// <summary>
        /// 设置车身颜色
        /// </summary>
        /// <param name="color">颜色</param>
        /// <param name="alpha">不透明度</param>
        public void SetTint(Color color, float alpha)
        {
            color.a = alpha;
            mBody.color = color;
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
    }
}
