//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 客户端入口
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 客户端入口，挂在 entry 场景的 Entry 物体上
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        private static GameEntry sInstance;

        [Header("关卡数据（可留空）")]
        [Tooltip("编辑器导出的关卡 JSON。留空则自动生成测试关。")]
        public TextAsset LevelJson;

        [Header("测试关参数（LevelJson 为空时生效）")]
        public int TestWidth = 40;

        public int TestHeight = 44;

        public int TestColors = 6;

        public int SmallCar = 50;

        public int MidCar = 100;

        public int LargeCar = 180;

        [Tooltip("车数配比 小/中/大")]
        public Vector3 TestShares = new(30f, 40f, 30f);

        [Header("规则参数")]
        public int LaneCapacity = 5;

        public float DriveSpeed = 6f;

        [Tooltip("每装一格之间的间隔（秒）")]
        public float LoadInterval = 0.07f;

        [Tooltip("没装满的车开到出口后怎么处理")]
        public ENotFullBehavior NotFullBehavior = ENotFullBehavior.LoopOrReturn;

        [Header("界面")]
        [Tooltip("开始界面，场景里摆好的实例；留空则直接进游戏")]
        public StartWindow StartWindow;

        /// <summary>
        /// 当前实例
        /// </summary>
        public static GameEntry Instance => sInstance;

        /// <summary>
        /// 主相机
        /// </summary>
        public Camera MainCamera { get; private set; }

        /// <summary>
        /// 主模块
        /// </summary>
        public MainModule Main { get; private set; }

        /// <summary>
        /// 唤醒
        /// </summary>
        private void Awake()
        {
            sInstance = this;
            MyGame.G.Init();
            SetupCamera();

            if (StartWindow)
            {
                StartWindow.StartClicked += OnStartClicked;
                StartWindow.Show();
                return;
            }

            OnCreate();
        }

        /// <summary>
        /// 开始界面点了「第 N 关」
        /// </summary>
        private void OnStartClicked()
        {
            if (StartWindow)
            {
                StartWindow.StartClicked -= OnStartClicked;
                StartWindow.Hide();
            }

            OnCreate();
        }

        /// <summary>
        /// 每帧
        /// </summary>
        private void Update()
        {
            if (MyGame.G.IsReady)
            {
                MyGame.G.App.Update(Mathf.RoundToInt(Time.deltaTime * 1000f));
            }

            if (Main != null)
            {
                Main.Tick(Time.deltaTime);
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void OnDestroy()
        {
            if (StartWindow)
            {
                StartWindow.StartClicked -= OnStartClicked;
            }

            if (Main != null)
            {
                Main.Dispose();
                Main = null;
            }

            sInstance = null;
            MyGame.G.Destroy();
        }

        /// <summary>
        /// 准备正交相机
        /// </summary>
        private void SetupCamera()
        {
            var mainCamera = Camera.main;
            if (!mainCamera)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                mainCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            mainCamera.orthographic = true;
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 100f;
            MainCamera = mainCamera;
        }
    }
}
