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
        /// 背景音乐目录。开场从 music_01 到 music_14 里随机一首。
        /// </summary>
        private const string MusicDirectory = "Assets/Res/audio/music/";

        /// <summary>
        /// 背景音乐编号下限，含
        /// </summary>
        private const int MusicMin = 1;

        /// <summary>
        /// 背景音乐编号上限，含
        /// </summary>
        private const int MusicMax = 14;

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
            PlayBackgroundMusic();

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
        /// 随机播放一首循环背景音乐
        /// </summary>
        private static void PlayBackgroundMusic()
        {
            var music = MyGame.G.MusicMgr;
            if (music == null)
            {
                return;
            }

            music.Play(PickMusic());
        }

        /// <summary>
        /// 在 music_01 到 music_14 里随机一首
        /// </summary>
        /// <returns>曲目路径</returns>
        private static string PickMusic()
        {
            var index = Random.Range(MusicMin, MusicMax + 1);
            return MusicPath(index);
        }

        /// <summary>
        /// 拼出一首背景音乐的路径
        /// </summary>
        /// <param name="index">曲目编号，从 1 起</param>
        /// <returns>曲目路径</returns>
        private static string MusicPath(int index)
        {
            return MusicDirectory + "music_" + index.ToString("00") + ".mp3";
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
