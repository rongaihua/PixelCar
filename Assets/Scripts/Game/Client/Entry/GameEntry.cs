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
        /// 关卡资产目录。第 N 关的资产是 levelN.asset。
        /// </summary>
        public const string BoardDirectory = "Assets/Res/PixelBoards/";

        /// <summary>
        /// 第一关的关卡号。
        /// </summary>
        public const int FirstLevel = 1;

        [Header("关卡数据")]
        [Tooltip("当前关卡号，从 1 起。第 N 关加载 Assets/Res/PixelBoards/levelN.asset，过关后自动 +1。")]
        public int Level = FirstLevel;

        [Tooltip("编辑器导出的关卡 JSON。关卡资产和它都为空时自动生成测试关。")]
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
        /// 当前加载进来的关卡号，0 表示还没加载
        /// </summary>
        private int mLoadedLevel;

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
                StartWindow.Level = Level;
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
        /// 第 N 关的关卡资产路径。
        /// </summary>
        /// <param name="level">关卡号，从 1 起</param>
        /// <returns>资产路径</returns>
        public static string BoardPathOf(int level)
        {
            return BoardDirectory + "level" + Mathf.Max(FirstLevel, level) + ".asset";
        }

        /// <summary>
        /// 按当前关卡号动态加载关卡资产（加载一次，切关时换新的）。
        /// </summary>
        /// <returns>关卡资产。加载不到时为空。</returns>
        public PixelBoardAsset ResolveBoardAsset()
        {
            if (mLoadedBoard && mLoadedLevel == Level)
            {
                return mLoadedBoard;
            }

            return LoadBoard(Level) ? mLoadedBoard : null;
        }

        /// <summary>
        /// 进下一关：先把 level{N+1} 加载进来，成功了才把关卡号 +1。
        /// </summary>
        /// <returns>下一关可用时为 true；资产不存在时返回 false，关卡号不变</returns>
        public bool NextLevel()
        {
            return SetLevel(Level + 1);
        }

        /// <summary>
        /// 切到指定关卡。
        /// </summary>
        /// <param name="level">关卡号，从 1 起</param>
        /// <returns>切关成功时为 true</returns>
        public bool SetLevel(int level)
        {
            if (level < FirstLevel)
            {
                return false;
            }

            if (level == Level && mLoadedBoard)
            {
                return true;
            }

            if (!LoadBoard(level))
            {
                MyGame.Logger.Warn($"第 {level} 关的关卡资产不存在：{BoardPathOf(level)}");
                return false;
            }

            Level = level;
            return true;
        }

        /// <summary>
        /// 加载某一关的关卡资产，成功后才把旧的还掉。
        /// </summary>
        /// <param name="level">关卡号，从 1 起</param>
        /// <returns>加载成功时为 true</returns>
        private bool LoadBoard(int level)
        {
            var path = BoardPathOf(level);
            var resMgr = MyGame.G.ResMgr;
            if (resMgr == null)
            {
                MyGame.Logger.Warn($"资源管理器还没初始化，关卡资产加载不了：{path}");
                return false;
            }

            var handle = resMgr.LoadResSync<PixelBoardAsset>(path);
            var asset = handle?.Asset as PixelBoardAsset;
            if (!asset)
            {
                resMgr.Release(handle);
                MyGame.Logger.Warn($"关卡资产加载失败：{path}");
                return false;
            }

            ReleaseBoard();
            mBoardHandle = handle;
            mLoadedBoard = asset;
            mLoadedLevel = level;
            return true;
        }

        /// <summary>
        /// 归还当前关卡资产。
        /// </summary>
        private void ReleaseBoard()
        {
            if (mBoardHandle != null)
            {
                MyGame.G.ResMgr?.Release(mBoardHandle);
            }

            mBoardHandle = null;
            mLoadedBoard = null;
            mLoadedLevel = 0;
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

            if (mBoardHandle != null || mLoadedBoard)
            {
                ReleaseBoard();
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
