//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 开始界面排版预览抓图
//---------------------------------------------------------------------------------------

using System.IO;
using PixelCar.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCar.Editor
{
    /// <summary>
    /// 开始界面排版预览抓图：把 Overlay 画布临时切到相机模式渲染到贴图，用来核对排版
    /// </summary>
    public static class UiPreviewCapture
    {
        private const int Width = 1080;
        private const int Height = 1920;
        private const string OutputPath = "Assets/Res/UI/preview.png";

        /// <summary>
        /// 抓图
        /// </summary>
        [MenuItem("Tools/PixelCar/抓图预览开始界面")]
        public static void Capture()
        {
            EditorSceneManager.OpenScene(EntrySceneMenu.EntryScenePath, OpenSceneMode.Single);

            var window = Object.FindObjectOfType<StartWindow>();
            if (!window)
            {
                Debug.LogError("场景里找不到 StartWindow");
                return;
            }

            var canvas = window.GetComponentInParent<Canvas>();
            if (!canvas)
            {
                Debug.LogError("StartWindow 不在 Canvas 下");
                return;
            }

            var cameraObject = new GameObject("PreviewCamera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = Height * 0.5f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var renderTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;

            var previousMode = canvas.renderMode;
            var previousCamera = canvas.worldCamera;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5f;

            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture.active = renderTexture;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;

            File.WriteAllBytes(OutputPath, texture.EncodeToPNG());
            AssetDatabase.Refresh();

            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;

            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(texture);

            Debug.Log($"预览已输出：{OutputPath}");
        }
    }
}
