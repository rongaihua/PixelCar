//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 车道 + 取色的纯逻辑模拟（不碰渲染，可脱离 Unity 跑批量测试）
//---------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace PixelCar.Common
{
    /// <summary>
    /// 车道 + 取色 的纯逻辑模拟。
    ///
    /// 规则：
    /// 1. 车只能取每列最底下一格，取完上面的格子落下；
    /// 2. 车从车道左侧驶入，沿图往右开，前车挡路就排队，不能叠在一起；
    /// 3. 开到自己颜色的那一列就停下，按节拍把这一列底部同色吸完，再继续往前；
    /// 4. 装满后开出右侧消失；没装满就从左侧再绕进来，继续占着车道名额。
    /// </summary>
    public class PixelCarSim
    {
        /// <summary>
        /// 待命区
        /// </summary>
        public readonly List<CarState> Pool = new();

        /// <summary>
        /// 车道
        /// </summary>
        public readonly List<CarState> Lane = new();

        /// <summary>
        /// 停车场列数
        /// </summary>
        public int LotCols { get; private set; }

        /// <summary>
        /// 停车场行数
        /// </summary>
        public int LotRows { get; private set; }

        /// <summary>
        /// 停车场占位表，索引 = Row * LotCols + Col
        /// </summary>
        private CarState[] mOccupancy = new CarState[0];

        /// <summary>
        /// 收集一辆车占格时用的缓冲，避免每次扫描都分配。
        /// </summary>
        private readonly int[] mCellBuffer = new int[4];

        /// <summary>
        /// 死局判断用的颜色表，长度跟调色板一致。
        /// </summary>
        private bool[] mPullable = new bool[0];
        /// <summary>
        /// 车道最多同时容纳几辆车
        /// </summary>
        public int LaneCapacity = 5;

        /// <summary>
        /// 行驶速度
        /// </summary>
        public float DriveSpeed = 6f;

        /// <summary>
        /// 每装一格之间的间隔（秒）
        /// </summary>
        public float LoadInterval = 0.07f;

        /// <summary>
        /// 像素图左边缘的世界坐标。车道和列号都换算到这个坐标系。
        /// </summary>
        public float BoardOriginX;

        /// <summary>
        /// 像素图的世界宽度。
        /// </summary>
        public float BoardWorldWidth = 1f;

        /// <summary>
        /// 停车场一格的世界尺寸。车长、跟车距离都按它算。
        /// </summary>
        public float CellSize = 1f;

        /// <summary>
        /// 没装满就开到出口的车怎么处理
        /// </summary>
        public ENotFullBehavior OnNotFull = ENotFullBehavior.LoopOrReturn;

        /// <summary>
        /// 盘面有变化，需要重绘
        /// </summary>
        public bool Dirty;

        /// <summary>
        /// 装满离场的车数
        /// </summary>
        public int CarsCompleted;

        /// <summary>
        /// 卡在出口的车数
        /// </summary>
        public int CarsParked;

        /// <summary>
        /// 绕圈次数
        /// </summary>
        public int CarsLooped;

        /// <summary>
        /// 累计装进车里多少格，应当恒等于盘面被取走的格数
        /// </summary>
        public int CellsLoaded;

        /// <summary>
        /// 提示回调
        /// </summary>
        public System.Action<string> Notify;

        /// <summary>
        /// 一辆车彻底离开车道时回调
        /// </summary>
        public System.Action<CarState> OnCarRemoved;

        /// <summary>
        /// 盘面
        /// </summary>
        public BoardState Board { get; }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="board">盘面</param>
        public PixelCarSim(BoardState board)
        {
            Board = board;
        }

        /// <summary>
        /// 把待命区的车放进车道，车道满了返回 false
        /// </summary>
        /// <param name="car">要放的车</param>
        /// <returns>是否成功</returns>
        public bool Admit(CarState car)
        {
            if (IsBlocked(car))
            {
                return false;
            }

            if (car == null || Lane.Count >= LaneCapacity || !CanWrap(car))
            {
                return false;
            }

            if (!Pool.Remove(car))
            {
                return false;
            }

            Vacate(car);
            car.Phase = ECarPhase.Driving;
            car.X = EntryOf(car);
            car.TargetColumn = -1;
            car.LoadedAtLapStart = car.Loaded;
            Lane.Add(car);
            return true;
        }

        /// <summary>
        /// 把一辆车从车道移走
        /// </summary>
        /// <param name="car">要移走的车</param>
        public void RemoveFromLane(CarState car)
        {
            if (car == null)
            {
                return;
            }

            Lane.Remove(car);
            car.Phase = ECarPhase.Done;
        }

        /// <summary>
        /// 推进一帧
        /// </summary>
        /// <param name="deltaTime">帧间隔</param>
        public void Tick(float deltaTime)
        {
            for (var i = Lane.Count - 1; i >= 0; --i)
            {
                var car = Lane[i];
                switch (car.Phase)
                {
                    case ECarPhase.Driving:
                        TickDriving(car, deltaTime);
                        break;
                    case ECarPhase.Loading:
                        TickLoading(car, deltaTime);
                        break;
                    case ECarPhase.Exiting:
                        TickExiting(car, deltaTime);
                        break;
                }

                if (car.Phase == ECarPhase.Done)
                {
                    Lane.RemoveAt(i);
                    OnCarRemoved?.Invoke(car);
                }
                else if (car.Recycled)
                {
                    car.Recycled = false;
                    Lane.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 是否已经无法再推进
        ///
        /// 判据是“全场没有任何一辆车能取到色块”，而不是“车道满了”：
        /// LoopOrReturn / ReturnToPool 下装不满的车会自己离开车道，车位迟早空出来。
        /// </summary>
        /// <returns>是否死局</returns>
        public bool IsDeadlocked()
        {
            if (Board.IsCleared || Board.TotalRemaining <= 0)
            {
                return false;
            }

            foreach (var car in Lane)
            {
                if (car.Phase is ECarPhase.Loading or ECarPhase.Exiting)
                {
                    return false;
                }

                if (car.Loaded >= car.Planned)
                {
                    return false;
                }
            }

            var laneFreesItself = OnNotFull is ENotFullBehavior.LoopOrReturn or ENotFullBehavior.ReturnToPool;
            if (!laneFreesItself && Lane.Count >= LaneCapacity)
            {
                return true;
            }

            if (mPullable.Length != Board.Remaining.Length)
            {
                mPullable = new bool[Board.Remaining.Length];
            }

            for (var i = 0; i < mPullable.Length; ++i)
            {
                mPullable[i] = false;
            }

            var pullable = mPullable;
            for (var x = 0; x < Board.Width; ++x)
            {
                var color = Board.BottomColor(x);
                if (color >= 0 && color < pullable.Length)
                {
                    pullable[color] = true;
                }
            }

            foreach (var car in Lane)
            {
                if (car.Loaded < car.Planned && car.Color >= 0 && car.Color < pullable.Length && pullable[car.Color])
                {
                    return false;
                }
            }

            foreach (var car in Pool)
            {
                if (car.Loaded < car.Planned && car.Color >= 0 && car.Color < pullable.Length && pullable[car.Color])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 推进行驶中的车
        /// </summary>
        /// <param name="car">车辆</param>
        /// <param name="deltaTime">帧间隔</param>
        private void TickDriving(CarState car, float deltaTime)
        {
            if (car.IsFull)
            {
                StartExiting(car);
                return;
            }

            if (car.X >= ExitOf(car) - 0.001f)
            {
                HandleReachExit(car);
                return;
            }

            var fromX = car.X;
            var travel = DriveSpeed * deltaTime;
            while (travel > 0.0001f)
            {
                var step = MathUtil.MinFloat(travel, ColumnWidth * 0.45f);
                if (step < 0.0001f)
                {
                    step = travel;
                }

                travel -= step;
                var moved = MoveForward(car, step);
                if (TryStartLoad(car, fromX))
                {
                    return;
                }

                if (moved <= 0.0001f)
                {
                    return;
                }

                fromX = car.X;
                if (car.X >= ExitOf(car) - 0.001f)
                {
                    HandleReachExit(car);
                    return;
                }
            }
        }

        /// <summary>
        /// 处理车开到出口
        /// </summary>
        /// <param name="car">车辆</param>
        private void HandleReachExit(CarState car)
        {
            switch (OnNotFull)
            {
                case ENotFullBehavior.Loop:
                    WrapToEntry(car);
                    return;

                case ENotFullBehavior.LoopOrReturn:
                    // 这一圈一格都没装到，就别再绕了，先回待命区，免得占死车位
                    if (car.Loaded == car.LoadedAtLapStart)
                    {
                        ReturnToPool(car);
                        return;
                    }

                    WrapToEntry(car);
                    return;

                case ENotFullBehavior.ReturnToPool:
                    ReturnToPool(car);
                    return;

                default:
                    car.X = ExitOf(car);
                    car.Phase = ECarPhase.Parked;
                    ++CarsParked;
                    Notify?.Invoke("有车没装满就开到了出口，占着车位");
                    return;
            }
        }

        /// <summary>
        /// 从右侧绕回左侧入口。入口被别的车占着就停在出口等，避免叠车。
        /// </summary>
        /// <param name="car">车辆</param>
        private void WrapToEntry(CarState car)
        {
            if (!CanWrap(car))
            {
                car.X = ExitOf(car);
                return;
            }

            car.LoadedAtLapStart = car.Loaded;
            car.X = EntryOf(car);
            car.TargetColumn = -1;
            ++CarsLooped;
        }

        /// <summary>
        /// 入口处有没有够这辆车插入的空档
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>可以绕回时为 true</returns>
        private bool CanWrap(CarState car)
        {
            var entry = EntryOf(car);
            var need = HalfLength(car) + CellSize * 0.2f;
            for (var i = 0; i < Lane.Count; ++i)
            {
                var other = Lane[i];
                if (other == car)
                {
                    continue;
                }

                if (other.X < entry + need + HalfLength(other))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 把车退回待命区
        /// </summary>
        /// <param name="car">车辆</param>
        private void ReturnToPool(CarState car)
        {
            car.Phase = ECarPhase.Pool;
            car.X = 0f;
            car.TargetColumn = -1;
            car.Recycled = true;
            Pool.Add(car);

            Occupy(car);
        }

        /// <summary>
        /// 推进装色中的车
        /// </summary>
        /// <param name="car">车辆</param>
        /// <param name="deltaTime">帧间隔</param>
        private void TickLoading(CarState car, float deltaTime)
        {
            if (car.IsFull)
            {
                StartExiting(car);
                return;
            }

            car.LoadTimer += deltaTime;
            if (car.LoadTimer < LoadInterval)
            {
                return;
            }

            car.LoadTimer = 0f;

            var column = car.TargetColumn;
            if (column < 0 || column >= Board.Width || Board.BottomColor(column) != car.Color)
            {
                // 这列底部换色了（或已取空），继续往前找
                car.Phase = ECarPhase.Driving;
                car.X = MathUtil.MaxFloat(car.X, ColumnCenter(column) + ColumnWidth * 0.55f);
                car.TargetColumn = -1;
                return;
            }

            if (Board.TryTake(column, car.Color))
            {
                ++car.Loaded;
                ++CellsLoaded;
                Dirty = true;
            }

            if (car.IsFull)
            {
                StartExiting(car);
            }
        }

        /// <summary>
        /// 推进开向出口的车
        /// </summary>
        /// <param name="car">车辆</param>
        /// <param name="deltaTime">帧间隔</param>
        private void TickExiting(CarState car, float deltaTime)
        {
            MoveForward(car, DriveSpeed * 1.45f * deltaTime);
            if (car.X < ExitOf(car) + CellSize * 0.9f)
            {
                return;
            }

            car.Phase = ECarPhase.Done;
            ++CarsCompleted;
        }

        /// <summary>
        /// 开始驶向出口
        /// </summary>
        /// <param name="car">车辆</param>
        private static void StartExiting(CarState car)
        {
            car.Phase = ECarPhase.Exiting;
            car.TargetColumn = -1;
        }
        /// <summary>
        /// 用已经写好的车头、朝向和车长重建占位表。行 0 是最靠近车道的一排。
        /// </summary>
        /// <param name="cols">列数</param>
        /// <param name="rows">行数</param>
        public void BindLot(int cols, int rows)
        {
            LotCols = MathUtil.Max(1, cols);
            LotRows = MathUtil.Max(1, rows);
            mOccupancy = new CarState[LotCols * LotRows];

            for (var i = 0; i < Pool.Count; ++i)
            {
                Occupy(Pool[i]);
            }
        }

        /// <summary>
        /// 车前方是否被别的车挡住。沿箭头方向扫到停车场边界，撞到车就算挡住。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>被挡住时为 true</returns>
        public bool IsBlocked(CarState car)
        {
            if (car == null || car.Phase != ECarPhase.Pool || mOccupancy.Length == 0)
            {
                return false;
            }

            return LotPacker.PathBlocked(car.Col, car.Row, car.Dir, LotCols, LotRows, mOccupancy);
        }

        /// <summary>
        /// 往前开一小段，不会开进前车的车身里。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <param name="distance">想开的距离</param>
        /// <returns>实际开出的距离</returns>
        private float MoveForward(CarState car, float distance)
        {
            var limit = ForwardLimit(car);
            var next = MathUtil.MinFloat(car.X + distance, limit);
            if (next < car.X)
            {
                next = car.X;
            }

            var moved = next - car.X;
            car.X = next;
            return moved;
        }

        /// <summary>
        /// 这辆车往前最多开到哪。出口和前车车尾都算。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>世界坐标上限</returns>
        private float ForwardLimit(CarState car)
        {
            // 装满离场的车要再开出一截才算离开，普通行驶停在出口等绕回
            var limit = car.Phase == ECarPhase.Exiting ? ExitOf(car) + CellSize * 1.4f : ExitOf(car);
            var myHalf = HalfLength(car);
            for (var i = 0; i < Lane.Count; ++i)
            {
                var other = Lane[i];
                if (other == car || other.X <= car.X)
                {
                    continue;
                }

                var gap = myHalf + HalfLength(other) + CellSize * 0.18f;
                var stop = other.X - gap;
                if (stop < limit)
                {
                    limit = stop;
                }
            }

            if (limit < car.X)
            {
                limit = car.X;
            }

            return limit;
        }

        /// <summary>
        /// 这一小段有没有开到自己能吸的列上。开到了就停在列中心开始吸。
        /// </summary>
        /// <param name="car">车辆</param>
        /// <param name="fromX">这一小段的起点</param>
        /// <returns>开始吸色时为 true</returns>
        private bool TryStartLoad(CarState car, float fromX)
        {
            var column = ColumnAt(car.X);
            if (column < 0 && fromX < car.X)
            {
                column = ColumnAt((fromX + car.X) * 0.5f);
            }

            if (column < 0 || column >= Board.Width || Board.ColumnCount(column) <= 0 || Board.BottomColor(column) != car.Color)
            {
                return false;
            }

            var center = ColumnCenter(column);
            if (center + ColumnWidth * 0.2f < fromX)
            {
                return false;
            }

            var limit = ForwardLimit(car);
            if (center > limit + 0.02f)
            {
                return false;
            }

            if (car.X + 0.001f < center - ColumnWidth * 0.45f)
            {
                return false;
            }

            car.X = MathUtil.MinFloat(center, limit);
            car.TargetColumn = column;
            car.Phase = ECarPhase.Loading;
            car.LoadTimer = 0f;
            return true;
        }

        /// <summary>
        /// 把车占上的格子记进占位表。
        /// </summary>
        /// <param name="car">车辆</param>
        private void Occupy(CarState car)
        {
            if (car == null || mOccupancy.Length == 0)
            {
                return;
            }

            var count = LotPacker.Collect(car.Col, car.Row, MathUtil.Clamp(car.Span, 1, mCellBuffer.Length), car.Dir, LotCols, LotRows, mCellBuffer);
            if (count < 0)
            {
                return;
            }

            for (var i = 0; i < count; ++i)
            {
                var index = mCellBuffer[i];
                if (index >= 0 && index < mOccupancy.Length)
                {
                    mOccupancy[index] = car;
                }
            }
        }

        /// <summary>
        /// 把车占上的格子从占位表清掉。
        /// </summary>
        /// <param name="car">车辆</param>
        private void Vacate(CarState car)
        {
            if (car == null || mOccupancy.Length == 0)
            {
                return;
            }

            var count = LotPacker.Collect(car.Col, car.Row, MathUtil.Clamp(car.Span, 1, mCellBuffer.Length), car.Dir, LotCols, LotRows, mCellBuffer);
            if (count < 0)
            {
                return;
            }

            for (var i = 0; i < count; ++i)
            {
                var index = mCellBuffer[i];
                if (index >= 0 && index < mOccupancy.Length && mOccupancy[index] == car)
                {
                    mOccupancy[index] = null;
                }
            }
        }

        /// <summary>
        /// 车身半长
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>世界单位</returns>
        private float HalfLength(CarState car)
        {
            return MathUtil.Max(1, car.Span) * CellSize * 0.46f;
        }

        /// <summary>
        /// 这辆车从画面左侧进入的位置
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>世界坐标</returns>
        private float EntryOf(CarState car)
        {
            return BoardOriginX - HalfLength(car);
        }

        /// <summary>
        /// 这辆车车身完全开出画面右侧的位置
        /// </summary>
        /// <param name="car">车辆</param>
        /// <returns>世界坐标</returns>
        private float ExitOf(CarState car)
        {
            return BoardOriginX + BoardWorldWidth + HalfLength(car);
        }

        /// <summary>
        /// 一列像素的世界宽度
        /// </summary>
        private float ColumnWidth
        {
            get
            {
                var width = BoardWorldWidth < 0.001f ? 1f : BoardWorldWidth;
                return width / MathUtil.Max(1, Board.Width);
            }
        }

        /// <summary>
        /// 世界坐标落在哪一列。图外返回 -1。
        /// </summary>
        /// <param name="worldX">世界坐标</param>
        /// <returns>列号</returns>
        private int ColumnAt(float worldX)
        {
            var local = worldX - BoardOriginX;
            if (local < 0f || local >= BoardWorldWidth)
            {
                return -1;
            }

            return MathUtil.FloorToInt(local / ColumnWidth);
        }

        /// <summary>
        /// 某一列中心的世界坐标
        /// </summary>
        /// <param name="column">列号</param>
        /// <returns>世界坐标</returns>
        private float ColumnCenter(int column)
        {
            return BoardOriginX + (column + 0.5f) * ColumnWidth;
        }

    }
}
