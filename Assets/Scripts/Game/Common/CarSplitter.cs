//---------------------------------------------------------------------------------------
// Copyright (c) WithMe8 2023-2030
// Author: WYF
// Date: 2026-10-28
// Desc: 把某种颜色的格数拆成若干辆车
//---------------------------------------------------------------------------------------

using System.Collections.Generic;

namespace PixelCar.Common
{
    /// <summary>
    /// 把某种颜色的格数拆成若干辆车。
    ///
    /// 两种拆法：配比模式按 小/中/大 的车数占比凑，让车流有层次；
    /// 配比凑不出时退回“大车优先”的完全背包求法。
    ///
    /// 50/100/180 能拼出的数很稀疏（60/70/110/120/130/140 都拼不出），
    /// 所以实际关卡几乎不可能让每种颜色都整拆，必须允许尾车装不满。
    /// </summary>
    public static class CarSplitter
    {
        /// <summary>
        /// 拆分一种颜色的格数，返回每辆车的额定载量列表
        /// </summary>
        /// <param name="count">该颜色的总格数</param>
        /// <param name="sizes">车辆规格，按 小 → 大 排列</param>
        /// <param name="shares">车数配比，与 sizes 一一对应</param>
        /// <param name="allowTail">是否允许尾车装不满</param>
        /// <param name="tailLoad">输出尾车实际装载量，0 表示全部装满</param>
        /// <returns>每辆车的额定载量；无法整拆且不允许尾车时返回 null</returns>
        public static List<int> Split(int count, IList<int> sizes, IList<float> shares, bool allowTail, out int tailLoad)
        {
            tailLoad = 0;
            var plan = new List<int>();
            if (count <= 0)
            {
                return plan;
            }

            var distinct = new List<int>();
            if (sizes != null)
            {
                foreach (var size in sizes)
                {
                    if (size > 0 && !distinct.Contains(size))
                    {
                        distinct.Add(size);
                    }
                }
            }

            distinct.Sort();
            if (distinct.Count == 0)
            {
                plan.Add(count);
                return plan;
            }

            if (distinct.Count == 3 && shares != null && shares.Count == 3)
            {
                var mixed = SplitByMix(count, distinct[0], distinct[1], distinct[2], shares[0], shares[1], shares[2], allowTail, out tailLoad);
                if (mixed != null)
                {
                    return mixed;
                }

                tailLoad = 0;
            }

            return SplitLargeFirst(count, distinct, allowTail, out tailLoad);
        }

        /// <summary>
        /// 按车数配比拆分
        /// </summary>
        /// <param name="count">总格数</param>
        /// <param name="s0">小车规格</param>
        /// <param name="s1">中车规格</param>
        /// <param name="s2">大车规格</param>
        /// <param name="t0">小车配比</param>
        /// <param name="t1">中车配比</param>
        /// <param name="t2">大车配比</param>
        /// <param name="allowTail">是否允许尾车装不满</param>
        /// <param name="tailLoad">输出尾车实际装载量</param>
        /// <returns>载量列表；无解返回 null</returns>
        private static List<int> SplitByMix(int count, int s0, int s1, int s2, float t0, float t1, float t2, bool allowTail, out int tailLoad)
        {
            tailLoad = 0;
            if (!(s0 <= s1 && s1 <= s2))
            {
                return null;
            }

            var sum = t0 + t1 + t2;
            if (sum <= 0.0001f)
            {
                t0 = 1f;
                t1 = 1f;
                t2 = 1f;
                sum = 3f;
            }

            t0 /= sum;
            t1 /= sum;
            t2 /= sum;

            var bestA = 0;
            var bestB = 0;
            var bestC = 0;
            var bestScore = double.MaxValue;
            var found = false;

            var maxC = count / s2;
            var abAverage = t0 * s0 + t1 * s1;
            var abShare = t0 + t1;

            for (var c = 0; c <= maxC; ++c)
            {
                var rest = count - c * s2;

                // 剩余部分按 t0:t1 分给小/中，估个中车数量，再左右各试几档
                var targetB = 0;
                if (abShare > 0.0001f && abAverage > 0f)
                {
                    var abCount = MathUtil.RoundToInt(rest * abShare / abAverage);
                    targetB = MathUtil.RoundToInt(abCount * t1 / abShare);
                }

                for (var delta = -3; delta <= 3; ++delta)
                {
                    var b = targetB + delta;
                    if (b < 0)
                    {
                        continue;
                    }

                    var remain = rest - b * s1;
                    if (remain < 0 || remain % s0 != 0)
                    {
                        continue;
                    }

                    var a = remain / s0;
                    var total = a + b + c;
                    if (total <= 0)
                    {
                        continue;
                    }

                    var filled = a * s0 + b * s1 + c * s2;
                    var tail = count - filled;
                    if (tail > 0 && !allowTail)
                    {
                        continue;
                    }

                    var score = Score(a, b, c, tail, s0, t0, t1, t2);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestA = a;
                        bestB = b;
                        bestC = c;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                return null;
            }

            tailLoad = count - (bestA * s0 + bestB * s1 + bestC * s2);
            return BuildInterleaved(bestA, bestB, bestC, s0, s1, s2, tailLoad);
        }

        /// <summary>
        /// 给一组拆分方案打分，越小越好
        /// </summary>
        /// <param name="a">小车数</param>
        /// <param name="b">中车数</param>
        /// <param name="c">大车数</param>
        /// <param name="tail">尾车余量</param>
        /// <param name="smallSize">小车规格</param>
        /// <param name="t0">小车目标占比</param>
        /// <param name="t1">中车目标占比</param>
        /// <param name="t2">大车目标占比</param>
        /// <returns>分数</returns>
        private static double Score(int a, int b, int c, int tail, int smallSize, float t0, float t1, float t2)
        {
            var total = a + b + c;
            if (total == 0)
            {
                return double.MaxValue;
            }

            var e0 = System.Math.Abs((double)a / total - t0);
            var e1 = System.Math.Abs((double)b / total - t1);
            var e2 = System.Math.Abs((double)c / total - t2);

            // 尾车：能整拆最好，其次能塞进小车也可以接受
            var tailPenalty = 0d;
            if (tail != 0)
            {
                tailPenalty = 2d;
            }

            if (tail > smallSize)
            {
                tailPenalty = 6d;
            }

            return (e0 + e1 + e2) * 20d + tailPenalty;
        }

        /// <summary>
        /// 把三种车按固定节奏交错排开，让车流里尺寸自然混在一起
        /// </summary>
        /// <param name="a">小车数</param>
        /// <param name="b">中车数</param>
        /// <param name="c">大车数</param>
        /// <param name="s0">小车规格</param>
        /// <param name="s1">中车规格</param>
        /// <param name="s2">大车规格</param>
        /// <param name="tailLoad">尾车实际装载量</param>
        /// <returns>载量列表</returns>
        private static List<int> BuildInterleaved(int a, int b, int c, int s0, int s1, int s2, int tailLoad)
        {
            var plan = new List<int>(a + b + c + (tailLoad > 0 ? 1 : 0));
            var order = new int[] { 2, 1, 0, 1, 2, 0 };
            var cursor = 0;
            var guard = 0;

            while (a + b + c > 0 && guard < 1000000)
            {
                var pick = order[cursor % order.Length];
                ++cursor;
                ++guard;

                if (pick == 0 && a > 0)
                {
                    plan.Add(s0);
                    --a;
                }
                else if (pick == 1 && b > 0)
                {
                    plan.Add(s1);
                    --b;
                }
                else if (pick == 2 && c > 0)
                {
                    plan.Add(s2);
                    --c;
                }
            }

            if (tailLoad > 0)
            {
                plan.Add(TailCapacity(tailLoad, s0, s1, s2));
            }

            return plan;
        }

        /// <summary>
        /// 大车优先的兜底拆法
        /// </summary>
        /// <param name="count">总格数</param>
        /// <param name="ascending">升序规格</param>
        /// <param name="allowTail">是否允许尾车装不满</param>
        /// <param name="tailLoad">输出尾车实际装载量</param>
        /// <returns>载量列表；无解返回 null</returns>
        private static List<int> SplitLargeFirst(int count, IList<int> ascending, bool allowTail, out int tailLoad)
        {
            tailLoad = 0;
            var plan = new List<int>();

            var descending = new List<int>(ascending);
            descending.Sort();
            descending.Reverse();

            var reachable = new bool[count + 1];
            var used = new int[count + 1];
            reachable[0] = true;

            for (var value = 0; value <= count; ++value)
            {
                if (!reachable[value])
                {
                    continue;
                }

                foreach (var size in descending)
                {
                    var next = value + size;
                    if (next > count)
                    {
                        continue;
                    }

                    if (!reachable[next])
                    {
                        reachable[next] = true;
                        used[next] = size;
                    }
                }
            }

            var best = count;
            while (best > 0 && !reachable[best])
            {
                --best;
            }

            if (best < count)
            {
                if (!allowTail)
                {
                    return null;
                }

                tailLoad = count - best;
            }

            var cursor = best;
            while (cursor > 0)
            {
                var size = used[cursor];
                if (size <= 0)
                {
                    break;
                }

                plan.Add(size);
                cursor -= size;
            }

            if (tailLoad > 0)
            {
                var small = ascending[0];
                var large = ascending[ascending.Count - 1];
                plan.Add(TailCapacity(tailLoad, small, small, large));
            }

            return plan;
        }

        /// <summary>
        /// 尾车用能装下余量的最小规格，都装不下就用最大规格
        /// </summary>
        /// <param name="tailLoad">尾车实际装载量</param>
        /// <param name="s0">小车规格</param>
        /// <param name="s1">中车规格</param>
        /// <param name="s2">大车规格</param>
        /// <returns>尾车额定载量</returns>
        private static int TailCapacity(int tailLoad, int s0, int s1, int s2)
        {
            if (tailLoad <= s0)
            {
                return s0;
            }

            if (tailLoad <= s1)
            {
                return s1;
            }

            return s2;
        }
    }
}
