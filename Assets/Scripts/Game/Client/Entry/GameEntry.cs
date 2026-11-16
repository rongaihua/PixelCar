//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: rongaihua
// Date: 2026-10-28
// Desc: 客户端入口
//---------------------------------------------------------------------------------------

using PixelCar.Common;
using TMPro;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 客户端入口，挂在 entry 场景的 Entry 物体上
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        private static GameEntry sInstance;

        /// <summary>
        /// 默认关卡资产路径。留空 BoardAsset 时按它动态加载。
        /// </summary>
        public const string DefaultBoardPath = "Assets/Res/PixelBoards/level1.asset";

        [Header("关卡数据（可留空）")]
        [Tooltip("指定了就用它；留空则按下面的路径动态加载。")]
        public PixelBoardAsset BoardAsset;

        [Tooltip("动态加载的关卡资产路径（编辑器下走 AssetDatabase）。")]
        public string BoardAssetPath = DefaultBoardPath;

        [Tooltip("编辑器导出的关卡 JSON。都留空则自动生成测试关。")]
        public TextAsset LevelJson;

        [Header("测试关参数（关卡资产和 JSON 都为空时生效）")]
        public int TestWidth = 80;

        public int TestHeight = 50;

        public int TestColors = 6;

        public int SmallCar = 50;

        public int MidCar = 100;

        public int LargeCar = 180;

        [Tooltip("车数配比 小/中/大")]
        public Vector3 TestShares = new(30f, 40f, 30f);

        [Header("规则参数")]
        public int LaneCapacity = 5;

        [Tooltip("车道速度倍率。1 表示空车大约 5 秒开过整张图")]
        public float DriveSpeed = 1f;

        [Tooltip("每装一格之间的间隔（秒）")]
        public float LoadInterval = 0.05f;

        [Tooltip("没装满的车开到出口后怎么处理")]
        public ENotFullBehavior NotFullBehavior = ENotFullBehavior.Loop;

        [Header("界面")]
        [Tooltip("UI 字体（TMP），直接引用 libra 包里的字体资产，不拷贝")]
        public TMP_FontAsset UiFont;

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
        /// 动态加载到的关卡资产
        /// </summary>
        private PixelBoardAsset mLoadedBoard;

        /// <summary>
        /// 关卡资产的加载持有，销毁时归还
        /// </summary>
        private MyGame.ResHandle mBoardHandle;

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
        /// 取关卡资产：挂了引用就用引用，否则按路径动态加载（只加载一次）。
        /// </summary>
        /// <returns>关卡资产。加载不到时为空。</returns>
        public PixelBoardAsset ResolveBoardAsset()
        {
            if (BoardAsset)
            {
                return BoardAsset;
            }

            if (mLoadedBoard)
            {
                return mLoadedBoard;
            }

            if (string.IsNullOrEmpty(BoardAssetPath))
            {
                return null;
            }

            var resMgr = MyGame.G.ResMgr;
            if (resMgr == null)
            {
                MyGame.Logger.Warn($"资源管理器还没初始化，关卡资产加载不了：{BoardAssetPath}");
                return null;
            }

            mBoardHandle = resMgr.LoadResSync<PixelBoardAsset>(BoardAssetPath);
            mLoadedBoard = mBoardHandle?.Asset as PixelBoardAsset;
            if (!mLoadedBoard)
            {
                MyGame.Logger.Warn($"关卡资产加载失败：{BoardAssetPath}");
            }

            return mLoadedBoard;
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

            if (mBoardHandle != null)
            {
                MyGame.G.ResMgr?.Release(mBoardHandle);
                mBoardHandle = null;
            }

            mLoadedBoard = null;
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
