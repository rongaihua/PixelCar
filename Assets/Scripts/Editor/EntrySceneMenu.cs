//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 入口场景维护
//---------------------------------------------------------------------------------------

using PixelCar.Client;
using TMPro;
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
        /// 默认关卡资产
        /// </summary>
        public const string BoardAssetPath = "Assets/Generated/PixelBoards/level1.asset";

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

        /// <summary>
        /// 从磁盘重载入口场景，并补上关卡资产 / 界面字体。
        ///
        /// 编辑器里开着的场景不会跟着磁盘上的 .unity 文件变，
        /// 所以在外部改过场景之后点一下这个，才看得到新字段的值。
        /// </summary>
        [MenuItem("Tools/PixelCar/重载入口场景并补赋值")]
        public static void ReloadAndAssign()
        {
            ReloadAndAssign(true);
        }

        /// <summary>
        /// 从磁盘重载入口场景，并补上关卡资产 / 界面字体。
        /// </summary>
        /// <param name="interactive">是否弹「保存场景」询问；自动执行时直接保存当前场景</param>
        public static void ReloadAndAssign(bool interactive)
        {
            if (interactive)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    return;
                }
            }
            else
            {
                // 自动跑的时候没人应答询问，先把当前场景存下来，免得丢掉手上的改动
                var current = EditorSceneManager.GetActiveScene();
                if (current.IsValid() && current.isDirty)
                {
                    EditorSceneManager.SaveScene(current);
                }
            }

            var scene = EditorSceneManager.OpenScene(EntryScenePath, OpenSceneMode.Single);

            var entry = Object.FindObjectOfType<GameEntry>();
            if (!entry)
            {
                Debug.LogError($"[入口] {EntryScenePath} 里没有 GameEntry");
                return;
            }

            var changed = false;

            var board = AssetDatabase.LoadAssetAtPath<PixelBoardAsset>(BoardAssetPath);
            if (board)
            {
                if (entry.BoardAsset != board)
                {
                    entry.BoardAsset = board;
                    changed = true;
                }
            }
            else
            {
                Debug.LogError($"[入口] 找不到关卡资产 {BoardAssetPath}");
            }

            if (!entry.UiFont)
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(StartWindowBuilder.FontPath);
                if (font)
                {
                    entry.UiFont = font;
                    changed = true;
                }
                else
                {
                    Debug.LogWarning($"[入口] 找不到界面字体 {StartWindowBuilder.FontPath}");
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(entry);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[入口] 场景已重载：关卡={entry.BoardAsset?.name}，字体={entry.UiFont?.name}");
        }

        /// <summary>
        /// 入口场景里的 GameEntry 是否还没挂上关卡资产 / 界面字体
        /// </summary>
        /// <returns>需要补赋值时为 true</returns>
        public static bool NeedsAssignment()
        {
            var entry = Object.FindObjectOfType<GameEntry>();
            return entry != null && (entry.BoardAsset == null || entry.UiFont == null);
        }
    }
}
