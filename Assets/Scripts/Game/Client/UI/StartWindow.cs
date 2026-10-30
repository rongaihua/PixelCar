//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 开始界面
//---------------------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UI;

namespace PixelCar.Client
{
    /// <summary>
    /// 开始界面：显示头像、体力、倒计时、金币与「第 N 关」按钮
    /// </summary>
    public class StartWindow : MonoBehaviour
    {
        [Header("关卡数据")]
        public int Level = 7;

        public int Coins = 1460;

        public int Hearts = 1;

        public int HeartCapacity = 2;

        public string RemainTime = "19:12";

        [Header("引用")]
        public Text LevelText;

        public Text CoinText;

        public Text HeartText;

        public Text HeartCapacityText;

        public Text TimerText;

        public Button StartButton;

        public Button PauseButton;

        /// <summary>
        /// 点击开始
        /// </summary>
        public event Action StartClicked;

        /// <summary>
        /// 点击暂停
        /// </summary>
        public event Action PauseClicked;

        /// <summary>
        /// 唤醒
        /// </summary>
        private void Awake()
        {
            if (StartButton)
            {
                StartButton.onClick.AddListener(OnStartButtonClicked);
            }

            if (PauseButton)
            {
                PauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            ButtonSoundListener.BindAll(gameObject);
            Refresh();
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            if (StartButton)
            {
                StartButton.onClick.RemoveListener(OnStartButtonClicked);
            }

            if (PauseButton)
            {
                PauseButton.onClick.RemoveListener(OnPauseButtonClicked);
            }
        }

        /// <summary>
        /// 显示
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>
        /// 隐藏
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void Refresh()
        {
            if (LevelText)
            {
                LevelText.text = $"第{Level}关";
            }

            if (CoinText)
            {
                CoinText.text = Coins.ToString();
            }

            if (HeartText)
            {
                HeartText.text = Hearts.ToString();
            }

            if (HeartCapacityText)
            {
                HeartCapacityText.text = HeartCapacity.ToString();
            }

            if (TimerText)
            {
                TimerText.text = RemainTime;
            }
        }

        /// <summary>
        /// 开始按钮
        /// </summary>
        private void OnStartButtonClicked()
        {
            StartClicked?.Invoke();
        }

        /// <summary>
        /// 暂停按钮
        /// </summary>
        private void OnPauseButtonClicked()
        {
            PauseClicked?.Invoke();
        }
    }
}
