//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 基础图元精灵
//---------------------------------------------------------------------------------------

using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 基础图元精灵：项目暂时没有美术资源，全部用 1x1 白图拉伸
    /// </summary>
    public static class SpriteLibrary
    {
        private static Sprite sBodySprite;
        private static Sprite sFillSprite;

        /// <summary>
        /// 车身用精灵（中心轴点）
        /// </summary>
        public static Sprite BodySprite
        {
            get
            {
                if (!sBodySprite)
                {
                    sBodySprite = Create(new Vector2(0.5f, 0.5f));
                }

                return sBodySprite;
            }
        }

        /// <summary>
        /// 进度条用精灵（左轴点）
        /// </summary>
        public static Sprite FillSprite
        {
            get
            {
                if (!sFillSprite)
                {
                    sFillSprite = Create(new Vector2(0f, 0.5f));
                }

                return sFillSprite;
            }
        }

        /// <summary>
        /// 用白色贴图生成 1x1 精灵
        /// </summary>
        /// <param name="pivot">轴点</param>
        /// <returns>精灵</returns>
        private static Sprite Create(Vector2 pivot)
        {
            return Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), pivot, 1f);
        }
    }
}
