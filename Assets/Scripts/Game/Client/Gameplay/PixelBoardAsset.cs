//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 色块盘资产（把关卡数据存成 Unity 资产）
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 色块盘资产
    /// </summary>
    [CreateAssetMenu(fileName = "PixelBoard", menuName = "PixelCar/色块盘资产")]
    public class PixelBoardAsset : ScriptableObject
    {
        /// <summary>
        /// 关卡数据
        /// </summary>
        public PixelBoardLevel Level = new();
    }
}
