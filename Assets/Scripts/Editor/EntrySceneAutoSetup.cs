//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-11-02
// Desc: 入口场景自动补赋值
//---------------------------------------------------------------------------------------

using UnityEditor;
using UnityEngine.SceneManagement;

namespace PixelCar.Editor
{
    /// <summary>
    /// 脚本重载后自动检查入口场景：色块盘资产、界面字体没挂上就自己挂好并保存。
    ///
    /// 编辑器里开着的场景不会跟着磁盘上的 .unity 文件变，所以在外部改过场景之后
    /// 直接 Play 跑的还是旧数据。这里在域重载后补一次，省得手动重载场景。
    /// 只在「当前场景就是入口场景、且确实缺值」时动手，其它情况一概不碰。
    /// </summary>
    [InitializeOnLoad]
    public static class EntrySceneAutoSetup
    {
        private const string SessionKey = "PixelCar.EntrySceneAutoSetup";

        /// <summary>
        /// 等编译 / 导入 / 退出播放等状态稳定下来再动手的时限
        /// </summary>
        private const double WaitSeconds = 30.0;

        private static double sDeadline;

        static EntrySceneAutoSetup()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Arm();
        }

        /// <summary>
        /// 退出播放后再检查一次（编辑脚本时如果正在 Play，要等退出编辑态）
        /// </summary>
        /// <param name="state">播放状态</param>
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                Arm();
            }
        }

        /// <summary>
        /// 挂上逐帧检查
        /// </summary>
        private static void Arm()
        {
            sDeadline = EditorApplication.timeSinceStartup + WaitSeconds;
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        /// <summary>
        /// 等编辑器空闲下来再做一次检查
        /// </summary>
        private static void OnUpdate()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (EditorApplication.timeSinceStartup > sDeadline)
                {
                    EditorApplication.update -= OnUpdate;
                }

                return;
            }

            EditorApplication.update -= OnUpdate;
            EditorApplication.delayCall += Run;
        }

        /// <summary>
        /// 检查并补赋值
        /// </summary>
        private static void Run()
        {
            if (SessionState.GetBool(SessionKey, false) || EditorApplication.isPlaying)
            {
                return;
            }

            if (SceneManager.GetActiveScene().path != EntrySceneMenu.EntryScenePath)
            {
                return;
            }

            if (!EntrySceneMenu.NeedsAssignment())
            {
                return;
            }

            // 一个编辑器会话只自动补一次，避免和手动改动打架
            SessionState.SetBool(SessionKey, true);
            EntrySceneMenu.ReloadAndAssign(false);
        }
    }
}
