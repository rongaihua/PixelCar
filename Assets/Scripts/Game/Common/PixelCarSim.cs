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
    /// 1. 车只能取每列最底下一格；
    /// 2. 车沿车道行驶，停在自己颜色能取的那一列；
    /// 3. 停住后按节拍吸，底部同色连着几格吸几格，底部换色就继续往前开；
    /// 4. 装满计划量后开向出口消失；
    /// 5. 没装满就开到出口的车按 OnNotFull 处理。
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
        /// 入口位置
        /// </summary>
        public float EntryX = -1.6f;

        /// <summary>
        /// 出口位置
        /// </summary>
        public float ExitX = 10f;

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
            ExitX = board.Width + 1.6f;
        }

        /// <summary>
        /// 把待命区的车放进车道，车道满了返回 false
        /// </summary>
        /// <param name="car">要放的车</param>
        /// <returns>是否成功</returns>
        public bool Admit(CarState car)
        {
            if (car == null || Lane.Count >= LaneCapacity)
            {
                return false;
            }

            if (!Pool.Remove(car))
            {
                return false;
            }

            car.Phase = ECarPhase.Driving;
            car.X = EntryX;
            car.TargetColumn = -1;
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

            var pullable = new bool[Board.Remaining.Length];
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

            var travel = DriveSpeed * deltaTime;
            while (travel > 0.0001f)
            {
                var step = MathUtil.MinFloat(travel, 0.2f);
                travel -= step;
                car.X += step;

                var column = MathUtil.FloorToInt(car.X);
                if (column < 0)
                {
                    continue;
                }

                if (column >= Board.Width)
                {
                    car.X = ExitX;
                    break;
                }

                if (Board.ColumnCount(column) > 0 && Board.BottomColor(column) == car.Color)
                {
                    car.X = column + 0.5f;
                    car.TargetColumn = column;
                    car.Phase = ECarPhase.Loading;
                    car.LoadTimer = 0f;
                    return;
                }
            }

            if (car.X < ExitX)
            {
                return;
            }

            HandleReachExit(car);
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
                    car.X = EntryX;
                    car.TargetColumn = -1;
                    ++CarsLooped;
                    return;

                case ENotFullBehavior.LoopOrReturn:
                    // 这一圈一格都没装到，就别再绕了，先回待命区，免得占死车位
                    if (car.Loaded == car.LoadedAtLapStart)
                    {
                        ReturnToPool(car);
                        return;
                    }

                    car.LoadedAtLapStart = car.Loaded;
                    car.X = EntryX;
                    car.TargetColumn = -1;
                    ++CarsLooped;
                    return;

                case ENotFullBehavior.ReturnToPool:
                    ReturnToPool(car);
                    return;

                default:
                    car.X = ExitX;
                    car.Phase = ECarPhase.Parked;
                    ++CarsParked;
                    Notify?.Invoke("有车没装满就开到了出口，占着车位");
                    return;
            }
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
                car.X = MathUtil.MaxFloat(car.X, column + 0.55f);
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
            car.X += DriveSpeed * 1.7f * deltaTime;
            if (car.X <= ExitX + 1.6f)
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
    }
}
