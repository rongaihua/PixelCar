//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-30
// Desc: 按钮和开关点击时播放音效
//---------------------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace PixelCar.Client
{
    /// <summary>
    /// 挂在按钮或开关上。点击按钮、或开关状态变化时，播放 <see cref="SoundPath"/>。
    /// </summary>
    public class ButtonSoundListener : MonoBehaviour
    {
        /// <summary>
        /// 通用按钮点击声
        /// </summary>
        public const string ClickPath = "Assets/Res/audio/sound/btn_click.mp3";

        /// <summary>
        /// 点击时播放的音效路径。为空则不播。
        /// </summary>
        public string SoundPath = ClickPath;

        /// <summary>
        /// 为 true 时使用通用点击声。单独配置的音效保持原路径。
        /// </summary>
        public bool IsCommon = true;

        /// <summary>
        /// 所在按钮
        /// </summary>
        private Button mButton;

        /// <summary>
        /// 所在开关
        /// </summary>
        private Toggle mToggle;

        /// <summary>
        /// 播放通用点击声
        /// </summary>
        public static void PlayClick()
        {
            Play(ClickPath);
        }

        /// <summary>
        /// 给根节点下带图的按钮和全部开关挂上通用点击声
        /// </summary>
        /// <param name="root">要扫描的根节点</param>
        public static void BindAll(GameObject root)
        {
            if (!root)
            {
                return;
            }

            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var i = 0; i < buttons.Length; i++)
            {
                BindButton(buttons[i]);
            }

            var toggles = root.GetComponentsInChildren<Toggle>(true);
            for (var i = 0; i < toggles.Length; i++)
            {
                BindToggle(toggles[i]);
            }
        }

        /// <summary>
        /// 接上按钮或开关
        /// </summary>
        private void OnEnable()
        {
            mButton = GetComponent<Button>();
            if (mButton)
            {
                mButton.onClick.AddListener(PlaySound);
                return;
            }

            mToggle = GetComponent<Toggle>();
            if (mToggle)
            {
                mToggle.onValueChanged.AddListener(OnValueChanged);
            }
        }

        /// <summary>
        /// 卸下按钮或开关
        /// </summary>
        private void OnDisable()
        {
            if (mButton)
            {
                mButton.onClick.RemoveListener(PlaySound);
            }

            if (mToggle)
            {
                mToggle.onValueChanged.RemoveListener(OnValueChanged);
            }

            mButton = null;
            mToggle = null;
        }

        /// <summary>
        /// 给一个按钮挂上通用点击声。没有图片，或图片是空的，不播。
        /// </summary>
        /// <param name="button">要处理的按钮</param>
        private static void BindButton(Button button)
        {
            if (!button)
            {
                return;
            }

            var listener = button.GetComponent<ButtonSoundListener>();
            if (listener && !listener.IsCommon)
            {
                return;
            }

            var image = button.GetComponentInChildren<Image>(true);
            if (!image || !image.sprite)
            {
                if (listener)
                {
                    Destroy(listener);
                }

                return;
            }

            if (!listener)
            {
                listener = button.gameObject.AddComponent<ButtonSoundListener>();
                listener.IsCommon = true;
            }

            listener.SoundPath = ClickPath;
        }

        /// <summary>
        /// 给一个开关挂上通用点击声
        /// </summary>
        /// <param name="toggle">要处理的开关</param>
        private static void BindToggle(Toggle toggle)
        {
            if (!toggle)
            {
                return;
            }

            var listener = toggle.GetComponent<ButtonSoundListener>();
            if (listener && !listener.IsCommon)
            {
                return;
            }

            if (!listener)
            {
                listener = toggle.gameObject.AddComponent<ButtonSoundListener>();
                listener.IsCommon = true;
            }

            listener.SoundPath = ClickPath;
        }

        /// <summary>
        /// 开关状态变了就播一声
        /// </summary>
        /// <param name="isOn">当前是否打开</param>
        private void OnValueChanged(bool isOn)
        {
            PlaySound();
        }

        /// <summary>
        /// 播放当前路径
        /// </summary>
        private void PlaySound()
        {
            Play(SoundPath);
        }

        /// <summary>
        /// 让音效管理器播放指定路径
        /// </summary>
        /// <param name="path">剪辑路径</param>
        private static void Play(string path)
        {
            if (string.IsNullOrEmpty(path) || MyGame.G.SoundMgr == null)
            {
                return;
            }

            MyGame.G.SoundMgr.Play2D(path);
        }
    }
}
