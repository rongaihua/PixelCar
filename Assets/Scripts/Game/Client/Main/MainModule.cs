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
        private const float Cell = 1f;
        private const float CrossSeconds = 5f;
        private const float DeadlockConfirmSeconds = 0.6f;

        private readonly Dictionary<CarState, CarView> mCarViews = new();

        private PixelBoardLevel mLevel;
        private PixelCarSim mSim;
        private Color32[] mPalette;
        private BoardView mBoardView;
        private HudWindow mHud;
        private GameObject mRoot;

        private int mInitialCells;
        private int mLotCols = 8;
        private int mLotRows = 1;
        private float mBoardOriginX;
        private float mBoardWorldWidth = 1f;
        private float mBoardWorldHeight = 1f;
        private float mBoardBottomY;
        private int mLastPoolCount = -1;

        private float mLaneY;
        private float mPoolTopY;
        private Vector3 mBadgeWorld;
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
        /// 车道名额牌的世界坐标，画在路中央。
        /// </summary>
        /// <summary>
        /// 取某辆车当前的世界坐标，停车场里的车也适用。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <param name="world">世界坐标</param>
        /// <returns>取到时为 true</returns>
        public bool TryGetCarWorld(CarState car, out Vector3 world)
        {
            world = Vector3.zero;
            if (car == null)
            {
                return false;
            }

            CarView view;
            if (!mCarViews.TryGetValue(car, out view) || view == null)
            {
                return false;
            }

            world = view.Position;
            return true;
        }
        public Vector3 BadgeWorld => mBadgeWorld;

        /// <summary>
        /// 车道上某辆车的百分比文字位置。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>世界坐标</returns>
        public Vector3 LanePercentWorld(CarState car)
        {
            var x = car == null ? mBadgeWorld.x : car.X;
            return new Vector3(x, mLaneY - Cell * 0.62f, 0f);
        }

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
            var pace = Game.DriveSpeed < 0.2f ? 1f : Game.DriveSpeed;
            mSim.LaneCapacity = Game.LaneCapacity;
            mSim.DriveSpeed = mBoardWorldWidth / CrossSeconds * pace;
            mSim.LoadInterval = Game.LoadInterval;
            mSim.OnNotFull = Game.NotFullBehavior;
            mSim.CellSize = Cell;
            mSim.BoardOriginX = mBoardOriginX;
            mSim.BoardWorldWidth = mBoardWorldWidth;
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
        }

        /// <summary>
        /// 建立场景物体
        /// </summary>
        private void BuildScene()
        {
            mRoot = new GameObject("GameRoot");
            mRoot.transform.SetParent(Game.transform, false);

            BuildCars();
            FinalizeLot();

            CreateParkingPad();
            CreateBoardFrame();
            CreateLaneStrip();
            CreateRoadDashes();
            mBoardView = new BoardView(mRoot.transform, mSim.Board, mPalette);
            mBoardView.Place(new Vector3(mBoardOriginX, mBoardBottomY, 0f), mBoardWorldWidth, mBoardWorldHeight);

            var hudObject = new GameObject("Hud");
            hudObject.transform.SetParent(mRoot.transform, false);
            mHud = hudObject.AddComponent<HudWindow>();
            mHud.Bind(this);
            hudObject.AddComponent<BoosterBar>();

            mBoardView.Refresh();
            mLastPoolCount = mSim.Pool.Count;
            LayoutPool();
            FitCamera();
        }

        /// <summary>
        /// 停车场底色
        /// </summary>
        private void CreateParkingPad()
        {
            var pad = new GameObject("Lot");
            pad.transform.SetParent(mRoot.transform, false);
            var width = mLotCols * Cell;
            var height = mLotRows * Cell;
            pad.transform.position = new Vector3(width * 0.5f, mPoolTopY - height * 0.5f, 0f);
            pad.transform.localScale = new Vector3(width + 0.35f, height + 0.35f, 1f);

            var renderer = pad.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteLibrary.BodySprite;
            renderer.color = new Color(0.74f, 0.78f, 0.86f);
            renderer.sortingOrder = -3;
        }

        /// <summary>
        /// 像素图衬底，比图略大一圈
        /// </summary>
        private void CreateBoardFrame()
        {
            var frame = new GameObject("BoardFrame");
            frame.transform.SetParent(mRoot.transform, false);
            frame.transform.position = new Vector3(mBoardOriginX + mBoardWorldWidth * 0.5f, mBoardBottomY + mBoardWorldHeight * 0.5f, 0f);
            frame.transform.localScale = new Vector3(mBoardWorldWidth + 0.28f, mBoardWorldHeight + 0.28f, 1f);

            var renderer = frame.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteLibrary.BodySprite;
            renderer.color = new Color(0.93f, 0.91f, 0.86f);
            renderer.sortingOrder = -2;
        }

        /// <summary>
        /// 车道底板
        /// </summary>
        private void CreateLaneStrip()
        {
            var laneObject = new GameObject("Lane");
            laneObject.transform.SetParent(mRoot.transform, false);
            laneObject.transform.position = new Vector3(mBoardOriginX + mBoardWorldWidth * 0.5f, mLaneY, 0f);
            laneObject.transform.localScale = new Vector3(mBoardWorldWidth + 0.2f, 0.84f, 1f);

            var renderer = laneObject.AddComponent<SpriteRenderer>();
            renderer.sprite = SpriteLibrary.BodySprite;
            renderer.color = new Color(0.62f, 0.66f, 0.74f);
            renderer.sortingOrder = -1;
        }

        /// <summary>
        /// 车道中央的白色虚线
        /// </summary>
        private void CreateRoadDashes()
        {
            var left = mBoardOriginX + 0.35f;
            var right = mBoardOriginX + mBoardWorldWidth - 0.35f;
            for (var x = left; x < right; x += 0.72f)
            {
                var dashObject = new GameObject("Dash");
                dashObject.transform.SetParent(mRoot.transform, false);
                dashObject.transform.position = new Vector3(x, mLaneY, 0f);
                dashObject.transform.localScale = new Vector3(0.34f, 0.07f, 1f);

                var dash = dashObject.AddComponent<SpriteRenderer>();
                dash.sprite = SpriteLibrary.BodySprite;
                dash.color = new Color(1f, 1f, 1f, 0.72f);
                dash.sortingOrder = 1;
            }
        }

        /// <summary>
        /// 把车按朝向和车长排进停车场，并算出像素图、车道、停车场的上下位置。
        /// </summary>
        private void FinalizeLot()
        {
            var seed = 17;
            for (var i = 0; i < mSim.Pool.Count; ++i)
            {
                var car = mSim.Pool[i];
                seed = seed * 31 + car.Color * 131 + car.Capacity;
            }

            int cols;
            int rows;
            LotPacker.Pack(mSim.Pool, seed, out cols, out rows);
            mLotCols = cols;
            mLotRows = rows;
            mSim.BindLot(cols, rows);

            var lotWidth = cols * Cell;
            var lotHeight = rows * Cell;
            mBoardWorldWidth = lotWidth;
            mBoardWorldHeight = lotWidth * mSim.Board.Height / MathUtil.Max(1, mSim.Board.Width);

            var maxBoardHeight = MathUtil.MaxFloat(lotHeight * 0.55f, lotWidth * 0.42f);
            if (mBoardWorldHeight > maxBoardHeight)
            {
                var scale = maxBoardHeight / mBoardWorldHeight;
                mBoardWorldHeight *= scale;
                mBoardWorldWidth *= scale;
            }

            mBoardOriginX = (lotWidth - mBoardWorldWidth) * 0.5f;

            var laneHalf = 0.42f;
            var gap = 0.22f;
            mPoolTopY = 0f;
            mLaneY = gap + laneHalf;
            mBoardBottomY = mLaneY + laneHalf + gap;
            mBadgeWorld = new Vector3(mBoardOriginX + mBoardWorldWidth * 0.5f, mLaneY - 0.08f, 0f);

            mSim.BoardOriginX = mBoardOriginX;
            mSim.BoardWorldWidth = mBoardWorldWidth;
            mSim.CellSize = Cell;
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
            for (var i = 0; i < mSim.Pool.Count; ++i)
            {
                PlacePoolCar(mSim.Pool[i]);
            }

            FitCamera();
        }

        /// <summary>
        /// 把停车场里的一辆车摆到它占的格子中心。
        /// </summary>
        /// <param name="car">车辆</param>
        private void PlacePoolCar(CarState car)
        {
            var view = mCarViews[car];
            var vertical = car.Dir == ECarDir.Up || car.Dir == ECarDir.Down;
            view.SetOnLane(false);
            view.SetFootprint(FootprintCenter(car), MathUtil.Max(1, car.Span) * Cell * 0.9f, Cell * 0.9f, vertical);
            view.SetStream(false, 0f, Color.white);
            view.SetTint(Color.white, 1f);
        }

        /// <summary>
        /// 车身中心。车头在 Col/Row，车尾沿朝向反方向延伸。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>世界坐标</returns>
        private Vector3 FootprintCenter(CarState car)
        {
            var along = (MathUtil.Max(1, car.Span) - 1) * 0.5f;
            var col = (float)car.Col;
            var row = (float)car.Row;
            switch (car.Dir)
            {
                case ECarDir.Up:
                    row += along;
                    break;
                case ECarDir.Down:
                    row -= along;
                    break;
                case ECarDir.Left:
                    col += along;
                    break;
                default:
                    col -= along;
                    break;
            }

            return new Vector3((col + 0.5f) * Cell, mPoolTopY - (row + 0.5f) * Cell, 0f);
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

            var top = mBoardBottomY + mBoardWorldHeight + 0.35f;
            var bottom = mPoolTopY - mLotRows * Cell - 1.4f;
            var midX = mLotCols * Cell * 0.5f;

            var halfHeight = (top - bottom) * 0.5f;
            var halfWidth = mLotCols * Cell * 0.5f + 0.4f;
            var aspect = MathUtil.MaxFloat(0.3f, camera.aspect);
            var size = MathUtil.MaxFloat(halfHeight, halfWidth / aspect);

            camera.orthographicSize = size;
            camera.transform.position = new Vector3(midX, (top + bottom) * 0.5f, -10f);
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

            for (var i = 0; i < mSim.Pool.Count; ++i)
            {
                var car = mSim.Pool[i];
                if (!mCarViews[car].Hit(world))
                {
                    continue;
                }

                if (mSim.IsBlocked(car))
                {
                    ShowMessage("这辆车被挡住了，先清开前面的车");
                    return;
                }

                if (!mSim.Admit(car))
                {
                    ShowMessage(mSim.Lane.Count >= Game.LaneCapacity
                        ? "车道满了，等车装满开走"
                        : "入口还有车，等它往前开一点");
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
            for (var i = 0; i < mSim.Pool.Count; ++i)
            {
                PlacePoolCar(mSim.Pool[i]);
            }

            for (var i = 0; i < mSim.Lane.Count; ++i)
            {
                var car = mSim.Lane[i];
                var view = mCarViews[car];
                var alpha = view.AdvanceFade(car.X, mBoardWorldWidth, deltaTime);
                view.SetOnLane(true);
                view.SetFootprint(new Vector3(car.X, mLaneY, 0f), MathUtil.Max(1, car.Span) * Cell * 0.9f, Cell * 0.72f, false);

                var tint = car.Phase == ECarPhase.Parked ? new Color(0.62f, 0.62f, 0.62f, 1f) : Color.white;
                view.SetTint(tint, alpha);

                var streaming = car.Phase == ECarPhase.Loading && car.Color >= 0 && car.Color < mPalette.Length;
                var color = streaming ? (Color)mPalette[car.Color] : Color.white;
                view.SetStream(streaming, mBoardBottomY, color);
            }
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
