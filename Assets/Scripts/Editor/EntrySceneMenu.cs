//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 入口场景维护
//---------------------------------------------------------------------------------------

using PixelCar.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PixelCar.Editor
{
    /// <summary>
    /// 入口场景维护：重建 entry 场景并设为启动场景
    /// </summary>
    public static class EntrySceneMenu
    {
        /// <summary>
        /// 入口场景路径
        /// </summary>
        public const string EntryScenePath = "Assets/Scenes/entry.unity";

        /// <summary>
        /// 重建入口场景
        /// </summary>
        [MenuItem("Tools/PixelCar/重建入口场景")]
        public static void Rebuild()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var entryObject = new GameObject("Entry");
            entryObject.AddComponent<GameEntry>();

            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            cameraObject.AddComponent<AudioListener>();

            EditorSceneManager.SaveScene(scene, EntryScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EntryScenePath, true) };
            AssetDatabase.Refresh();

            var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(EntryScenePath);
            Selection.activeObject = sceneAsset;
            EditorGUIUtility.PingObject(sceneAsset);
            Debug.Log($"入口场景已重建：{EntryScenePath}，按 Play 启动");
        }

        /// <summary>
        /// 把关口场景设为启动场景
        /// </summary>
        [MenuItem("Tools/PixelCar/设为启动场景")]
        public static void SetAsStartup()
        {
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(EntryScenePath, true) };
            Debug.Log($"已设为启动场景：{EntryScenePath}");
        }
    }
}
