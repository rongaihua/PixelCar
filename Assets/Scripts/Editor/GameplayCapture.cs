//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-30
// Desc: 关卡画面抓图（编辑模式下直接构建并渲染）
//---------------------------------------------------------------------------------------

using System.IO;
using System.Reflection;
using PixelCar.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCar.Editor
{
    /// <summary>
    /// 关卡画面抓图：编辑模式下调一次入口的创建流程，再把主相机渲染成 PNG。
    /// </summary>
    public static class GameplayCapture
    {
        private const int Width = 720;
        private const int Height = 1280;
        private const string OutputPath = "Assets/Res/UI/gameplay.png";

        /// <summary>
        /// 抓图。
        /// </summary>
        [MenuItem("Tools/PixelCar/抓图预览关卡")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(EntrySceneMenu.EntryScenePath, OpenSceneMode.Single);

            var entry = Object.FindObjectOfType<GameEntry>();
            if (!entry)
            {
                Debug.LogError("[抓图] 场景里没有 GameEntry");
                return;
            }

            var setup = typeof(GameEntry).GetMethod("SetupCamera", BindingFlags.NonPublic | BindingFlags.Instance);
            setup?.Invoke(entry, null);

            var create = typeof(GameEntry).GetMethod("OnCreate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (create == null)
            {
                Debug.LogError("[抓图] 找不到 OnCreate");
                return;
            }

            create.Invoke(entry, null);

            var camera = entry.MainCamera;
            if (!camera)
            {
                Debug.LogError("[抓图] 没有主相机");
                return;
            }

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();

            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
            AssetDatabase.Refresh();

            camera.targetTexture = previousTarget;
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(texture);

            Debug.Log($"[抓图] 已输出 {OutputPath}");
        }
    }
}
