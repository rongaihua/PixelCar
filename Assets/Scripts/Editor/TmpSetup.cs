//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-11-02
// Desc: TMP 基础资源导入
//---------------------------------------------------------------------------------------

using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace PixelCar.Editor
{
    /// <summary>
    /// TMP 基础资源（TMP Settings、着色器、默认字体）的导入。
    ///
    /// TextMeshPro 的组件在 OnEnable 里会读 TMP Settings，这份资源不在工程里时会空引用，
    /// 中文更是没有字体可用。这些资源由 TMP 包自带的 unitypackage 提供，
    /// 第一次进工程时缺了就静默导入一次，也可以手动点菜单。
    /// </summary>
    [InitializeOnLoad]
    public static class TmpSetup
    {
        private const string EssentialPackage = "Package Resources/TMP Essential Resources.unitypackage";

        private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private const string SessionKey = "PixelCar.TmpEssentialsImported";

        static TmpSetup()
        {
            EditorApplication.delayCall += EnsureEssentials;
        }

        /// <summary>
        /// 缺资源就导入一次
        /// </summary>
        private static void EnsureEssentials()
        {
            if (SessionState.GetBool(SessionKey, false) || HasSettings())
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            ImportEssentials();
        }

        /// <summary>
        /// 导入 TMP 基础资源
        /// </summary>
        [MenuItem("Tools/PixelCar/导入 TMP 基础资源")]
        public static void ImportEssentials()
        {
            if (HasSettings())
            {
                Debug.Log("[TMP] 基础资源已存在，跳过导入");
                return;
            }

            var path = ResolvePackageFile();
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError("[TMP] 找不到 TMP 基础资源 unitypackage，请手动执行 Window > TextMeshPro > Import TMP Essential Resources");
                return;
            }

            Debug.Log($"[TMP] 正在导入基础资源：{path}");
            AssetDatabase.ImportPackage(path, false);
        }

        /// <summary>
        /// 工程里是否已经有 TMP Settings
        /// </summary>
        /// <returns>是否已导入</returns>
        private static bool HasSettings()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath) != null;
        }

        /// <summary>
        /// 定位 TMP 包里的 unitypackage
        /// </summary>
        /// <returns>磁盘路径，找不到为空</returns>
        private static string ResolvePackageFile()
        {
            var info = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.textmeshpro/package.json");
            if (info != null)
            {
                var resolved = Path.Combine(info.resolvedPath, EssentialPackage);
                if (File.Exists(resolved))
                {
                    return resolved;
                }
            }

            // 包目录名带版本号，兜底直接扫一遍
            var cache = Path.Combine(Directory.GetCurrentDirectory(), "Library/PackageCache");
            if (!Directory.Exists(cache))
            {
                return null;
            }

            foreach (var directory in Directory.GetDirectories(cache, "com.unity.textmeshpro@*"))
            {
                var candidate = Path.Combine(directory, EssentialPackage);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
