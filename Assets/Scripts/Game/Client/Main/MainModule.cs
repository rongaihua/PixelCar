//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 主模块：盘面 / 车道 / 车辆 / 输入 / 判负 的总装
//---------------------------------------------------------------------------------------

using System.Collections.Generic;
using PixelCar.Common;
using UnityEngine;

namespace PixelCar.Client
{
    /// <summary>
    /// 主模块
    /// </summary>
    public class MainModule : GameModule
    {
        private const float PoolRowGap = 0.86f;
        private const float DeadlockConfirmSeconds = 0.6f;

        private readonly Dictionary<CarState, CarView> mCarViews = new();

        private PixelBoardLevel mLevel;
        private PixelCarSim mSim;
        private Color32[] mPalette;
        private BoardView mBoardView;
        private HudWindow mHud;
        private GameObject mRoot;

        private int mInitialCells;
        private int mPoolCols;
        private int mLastPoolCount = -1;

        private float mLaneY;
        private float mPoolTopY;
        private string mMessage = "";
        private float mMessageTimer;
        private bool mCleared;
        private bool mFailed;
        private float mDeadlockTimer;

        /// <summary>
        /// 盘面
        /// </summary>
        public PixelCarSim Sim => mSim;

        /// <summary>
        /// 调色板
        /// </summary>
        public Color32[] Palette => mPalette;

        /// <summary>
        /// 关卡初始总格数
        /// </summary>
        public int InitialCells => mInitialCells;

        /// <summary>
        /// 车道容量
        /// </summary>
        public int LaneCapacity => Game.LaneCapacity;

        /// <summary>
        /// 是否已过关
        /// </summary>
        public bool Cleared => mCleared;

        /// <summary>
        /// 是否已判负
        /// </summary>
        public bool Failed => mFailed;

        /// <summary>
        /// 当前提示
        /// </summary>
        public string Message => mMessage;

        /// <summary>
        /// 提示剩余显示时间
        /// </summary>
        public float MessageTimer => mMessageTimer;

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="game">客户端入口</param>
        public MainModule(GameEntry game) : base(game)
        {
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public override void Init()
        {
            BuildLevel();
            BuildScene();
        }

        /// <summary>
        /// 每帧推进
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        public override void Tick(float deltaTime)
        {
            if (mSim == null)
            {
                return;
            }

            var step = MathUtil.Clamp(deltaTime, 0f, 0.05f);
            ApplyConfig();

            if (!mFailed)
            {
                mSim.Tick(step);
            }

            if (mLastPoolCount != mSim.Pool.Count)
            {
                mLastPoolCount = mSim.Pool.Count;
                LayoutPool();
            }

            if (!mFailed)
            {
                HandleInput();
            }

            SyncViews(step);
            RefreshBoard();
            CheckCleared();
            CheckDeadlock(step);

            if (mMessageTimer > 0f)
            {
                mMessageTimer -= deltaTime;
            }
        }

        /// <summary>
        /// 重开本关
        /// </summary>
        public void Restart()
        {
            DestroyScene();
            mCleared = false;
            mFailed = false;
            mDeadlockTimer = 0f;
            mMessage = "";
            mMessageTimer = 0f;
            mLastPoolCount = -1;

            Init();
        }

        /// <summary>
        /// 释放
        /// </summary>
        public override void Dispose()
        {
            DestroyScene();
        }

        /// <summary>
        /// 把入口配置同步给模拟器
        /// </summary>
        private void ApplyConfig()
        {
            mSim.LaneCapacity = Game.LaneCapacity;
            mSim.DriveSpeed = Game.DriveSpeed;
            mSim.LoadInterval = Game.LoadInterval;
            mSim.OnNotFull = Game.NotFullBehavior;
        }

        /// <summary>
        /// 载入关卡数据
        /// </summary>
        private void BuildLevel()
        {
            mLevel = PixelCarLevelBuilder.Load(Game);

            mPalette = new Color32[mLevel.Palette.Length];
            for (var i = 0; i < mLevel.Palette.Length; ++i)
            {
                var value = mLevel.Palette[i];
                mPalette[i] = new Color32((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)(value & 0xFF), 255);
            }

            mSim = new PixelCarSim(new BoardState(mLevel, mPalette.Length));
            mSim.Notify = OnSimNotify;
            mSim.OnCarRemoved = OnCarRemoved;

            mInitialCells = MathUtil.Max(1, mSim.Board.TotalRemaining);
            mLaneY = -1.5f;
            mPoolTopY = mLaneY - 2.6f;
            mPoolCols = MathUtil.Clamp(mSim.Board.Width / 5, 6, 12);
        }

        /// <summary>
        /// 建立场景物体
        /// </summary>
        private void BuildScene()
        {
            mRoot = new GameObject("GameRoot");
            mRoot.transform.SetParent(Game.transform, false);

            CreateLaneStrip();
            mBoardView = new BoardView(mRoot.transform, mSim.Board, mPalette);
            BuildCars();

            var hudObject = new GameObject("Hud");
            hudObject.transform.SetParent(mRoot.transform, false);
            mHud = hudObject.AddComponent<HudWindow>();
            mHud.Bind(this);

            mBoardView.Refresh();
            mLastPoolCount = mSim.Pool.Count;
            LayoutPool();
            FitCamera();
        }

        /// <summary>
        /// 车道底板
        /// </summary>
        private void CreateLaneStrip()
        {
            var laneObject = new GameObject("Lane");
            laneObject.transform.SetParent(mRoot.transform, false);
            laneObject.transform.position = new Vector3(mSim.Board.Width * 0.5f, mLaneY, 0f);
            laneObject.transform.localScale = new Vector3(mSim.Board.Width + 4f, 1.6f, 1f);

            var renderer = laneObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteLibrary.BodySprite;
            renderer.color = new Color(0.16f, 0.17f, 0.22f);
            renderer.sortingOrder = -1;
        }

        /// <summary>
        /// 建立全部车辆
        /// </summary>
        private void BuildCars()
        {
            if (mLevel.Cars != null && mLevel.Cars.Length > 0)
            {
                foreach (var entry in mLevel.Cars)
                {
                    var car = new CarState
                    {
                        Color = MathUtil.Clamp(entry.Color, 0, mPalette.Length - 1),
                        Capacity = entry.Capacity > 0 ? entry.Capacity : Game.SmallCar,
                        Planned = entry.Load > 0 ? entry.Load : entry.Capacity,
                    };

                    mSim.Pool.Add(car);
                    mCarViews.Add(car, new CarView(car, mRoot.transform, mPalette[car.Color]));
                }

                return;
            }

            // 兜底：按盘面格数现拆
            var sizes = new int[] { Game.SmallCar, Game.MidCar, Game.LargeCar };
            var shares = new float[] { Game.TestShares.x, Game.TestShares.y, Game.TestShares.z };

            for (var color = 0; color < mPalette.Length; ++color)
            {
                var count = mSim.Board.Remaining[color];
                if (count <= 0)
                {
                    continue;
                }

                int tail;
                var plan = CarSplitter.Split(count, sizes, shares, true, out tail);
                var remain = count;
                foreach (var capacity in plan)
                {
                    var car = new CarState
                    {
                        Color = color,
                        Capacity = capacity,
                        Planned = MathUtil.Min(capacity, remain),
                    };

                    remain -= car.Planned;
                    mSim.Pool.Add(car);
                    mCarViews.Add(car, new CarView(car, mRoot.transform, mPalette[car.Color]));
                }
            }
        }

        /// <summary>
        /// 排布待命区
        /// </summary>
        private void LayoutPool()
        {
            var index = 0;
            foreach (var car in mSim.Pool)
            {
                var column = index % mPoolCols;
                var row = index / mPoolCols;
                var view = mCarViews[car];
                view.SetPose(PoolPosition(column, row), 0.82f, 0.54f);
                view.SetFill(car.Fill, 0.54f, 1f);
                ++index;
            }

            FitCamera();
        }

        /// <summary>
        /// 待命区某格的世界坐标
        /// </summary>
        /// <param name="column">列</param>
        /// <param name="row">行</param>
        /// <returns>世界坐标</returns>
        private Vector3 PoolPosition(int column, int row)
        {
            var x = column - (mPoolCols - 1) * 0.5f + mSim.Board.Width * 0.5f;
            var y = mPoolTopY - row * PoolRowGap;
            return new Vector3(x, y, 0f);
        }

        /// <summary>
        /// 相机取景
        /// </summary>
        private void FitCamera()
        {
            var camera = Game.MainCamera;
            if (!camera || mSim == null)
            {
                return;
            }

            var top = mSim.Board.Height + 1.2f;
            var rows = MathUtil.Max(1, MathUtil.CeilToInt(mSim.Pool.Count / (float)mPoolCols));
            var bottom = mSim.Pool.Count > 0 ? mPoolTopY - (rows - 1) * PoolRowGap - 1.4f : mLaneY - 2.5f;

            var halfHeight = (top - bottom) * 0.5f;
            var halfWidth = mSim.Board.Width * 0.5f + 1.2f;
            var aspect = MathUtil.MaxFloat(0.3f, camera.aspect);
            var size = MathUtil.MaxFloat(halfHeight, halfWidth / aspect);

            camera.orthographicSize = size;
            camera.transform.position = new Vector3(mSim.Board.Width * 0.5f, (top + bottom) * 0.5f, -10f);
        }

        /// <summary>
        /// 处理点击
        /// </summary>
        private void HandleInput()
        {
            var camera = Game.MainCamera;
            if (!camera || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            var world = camera.ScreenToWorldPoint(Input.mousePosition);
            world.z = 0f;

            for (var i = 0; i < mSim.Lane.Count; ++i)
            {
                var car = mSim.Lane[i];
                if (!mCarViews[car].Hit(world))
                {
                    continue;
                }

                mSim.RemoveFromLane(car);
                DestroyCarView(car);
                ShowMessage("已移除一辆车道上的车");
                return;
            }

            for (var i = 0; i < mSim.Pool.Count; ++i)
            {
                var car = mSim.Pool[i];
                if (!mCarViews[car].Hit(world))
                {
                    continue;
                }

                if (!mSim.Admit(car))
                {
                    ShowMessage("车道满了，等车装满开走");
                    return;
                }

                LayoutPool();
                return;
            }
        }

        /// <summary>
        /// 同步所有车辆显示
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        private void SyncViews(float deltaTime)
        {
            var index = 0;
            foreach (var car in mSim.Pool)
            {
                var view = mCarViews[car];
                var column = index % mPoolCols;
                var row = index / mPoolCols;
                view.SetPose(PoolPosition(column, row), 0.82f, 0.54f);
                view.SetFill(car.Fill, 0.54f, 1f);
                ++index;
            }

            foreach (var car in mSim.Lane)
            {
                var view = mCarViews[car];
                var length = CarLength(car);
                var alpha = view.AdvanceFade(car.X, mSim.Board.Width, deltaTime);
                view.SetPose(new Vector3(car.X, mLaneY, 0f), length, 0.86f);
                view.SetFill(car.Fill, 0.86f, alpha);

                var color = mPalette[car.Color];
                if (car.Phase == ECarPhase.Parked)
                {
                    color = Color.Lerp(color, Color.black, 0.45f);
                }

                view.SetTint(color, alpha);
            }
        }

        /// <summary>
        /// 车身长度
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>长度</returns>
        private static float CarLength(CarState car)
        {
            return 0.75f + 1.15f * MathUtil.Clamp01(car.Capacity / 180f);
        }

        /// <summary>
        /// 重绘盘面
        /// </summary>
        private void RefreshBoard()
        {
            if (!mSim.Dirty)
            {
                return;
            }

            mBoardView.Refresh();
            mSim.Dirty = false;
        }

        /// <summary>
        /// 过关判定
        /// </summary>
        private void CheckCleared()
        {
            if (mCleared || !mSim.Board.IsCleared)
            {
                return;
            }

            mCleared = true;
            ShowMessage("全部取完，过关！");
        }

        /// <summary>
        /// 死局判定
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        private void CheckDeadlock(float deltaTime)
        {
            if (mCleared || mFailed)
            {
                return;
            }

            if (!mSim.IsDeadlocked())
            {
                mDeadlockTimer = 0f;
                return;
            }

            mDeadlockTimer += deltaTime;
            if (mDeadlockTimer <= DeadlockConfirmSeconds)
            {
                return;
            }

            mFailed = true;
            ShowMessage("没有可拉取的色块了，本关失败");
        }

        /// <summary>
        /// 显示提示
        /// </summary>
        /// <param name="message">提示内容</param>
        private void ShowMessage(string message)
        {
            mMessage = message;
            mMessageTimer = 3.5f;
        }

        /// <summary>
        /// 模拟器提示回调
        /// </summary>
        /// <param name="message">提示内容</param>
        private void OnSimNotify(string message)
        {
            ShowMessage(message);
        }

        /// <summary>
        /// 车离场回调
        /// </summary>
        /// <param name="car">车辆</param>
        private void OnCarRemoved(CarState car)
        {
            DestroyCarView(car);
        }

        /// <summary>
        /// 销毁一辆车的显示
        /// </summary>
        /// <param name="car">车辆</param>
        private void DestroyCarView(CarState car)
        {
            if (!mCarViews.Remove(car, out var view))
            {
                return;
            }

            view.Destroy();
        }

        /// <summary>
        /// 销毁场景
        /// </summary>
        private void DestroyScene()
        {
            if (mRoot)
            {
                Object.Destroy(mRoot);
                mRoot = null;
            }

            if (mBoardView != null)
            {
                mBoardView.Destroy();
                mBoardView = null;
            }

            mCarViews.Clear();
            mHud = null;
        }
    }
}
