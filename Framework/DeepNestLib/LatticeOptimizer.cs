using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib
{
    /// <summary>
    /// 晶格优化器：针对同种形状搜索最优的周期性排列参数（单晶格或双晶格）
    /// 参考论文：Costa et al., "Heuristic approaches to large-scale periodic packing of irregular shapes on a rectangular sheet"
    /// </summary>
    public class LatticeOptimizer
    {
        private readonly SvgNestConfig _config;
        private readonly Random _random = new Random();

        // 优化参数
        public int MaxIterations { get; set; } = 80;          // 最大迭代次数
        public int RestartCount { get; set; } = 3;             // 随机重启次数
        public float InitialStepScale { get; set; } = 0.15f;   // 初始步长相对于形状尺寸的比例
        public float StepDecay { get; set; } = 0.85f;          // 步长衰减因子

        public LatticeOptimizer(SvgNestConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// 单晶格优化结果
        /// </summary>
        public class SingleLatticeResult
        {
            public SvgPoint U { get; set; }          // 横向晶格向量
            public SvgPoint V { get; set; }          // 纵向晶格向量
            public float Angle { get; set; }         // 统一旋转角度
            public double Density { get; set; }      // 面积利用率 (0~1)
            public double UnitArea { get; set; }     // 晶格单元面积
        }

        /// <summary>
        /// 双晶格优化结果
        /// </summary>
        public class DoubleLatticeResult
        {
            public SvgPoint U { get; set; }
            public SvgPoint V { get; set; }
            public float Angle1 { get; set; }
            public float Angle2 { get; set; }
            public double Density { get; set; }
            public double UnitArea { get; set; }
        }

        /// <summary>
        /// 优化单晶格（所有形状采用相同旋转角度）
        /// </summary>
        public SingleLatticeResult OptimizeSingleLattice(NFP shape)
        {
            var bounds = GeometryUtil.getPolygonBounds(shape);
            double shapeArea = Math.Abs(GeometryUtil.polygonArea(shape));

            // ✅ 限制候选角度数量，只取最有希望的几个
            var promisingAngles = GetPromisingAngles(shape);
            var candidateAngles = promisingAngles
                .Concat(new[] { 0f, 90f, 180f, 270f })
                .Distinct()
                .OrderBy(a => a) // 可选：按角度排序以便观察
                .Take(8)          // ✅ 最多只尝试 8 个初始角度
                .ToList();

            SingleLatticeResult bestResult = null;
            double bestDensity = 0;

            // ✅ 添加总超时控制
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(3); // 最多优化3秒

            foreach (var startAngle in candidateAngles)
            {
                // ✅ 每个 restart 也控制迭代次数
                int effectiveRestarts = Math.Min(RestartCount, 2); // 减少重启次数

                for (int restart = 0; restart < effectiveRestarts; restart++)
                {
                    if ((DateTime.Now - startTime) > timeout)
                    {
                        // 超时则直接返回当前最优
                        return bestResult ?? new SingleLatticeResult { Density = 0 };
                    }

                    var result = OptimizeSingleLatticeFromStart(shape, bounds, shapeArea, startAngle);
                    if (result.Density > bestDensity)
                    {
                        bestDensity = result.Density;
                        bestResult = result;
                    }
                }
            }

            return bestResult;
        }

        private SingleLatticeResult OptimizeSingleLatticeFromStart(NFP shape, PolygonBounds bounds, double shapeArea, float startAngle)
        {
            // 初始晶格向量：基于旋转后形状的包围盒，并加入随机扰动
            var rotated = Background.rotatePolygon(shape, startAngle);
            var rotBounds = GeometryUtil.getPolygonBounds(rotated);

            // 初始 U 沿 X 轴，V 沿 Y 轴（简单矩形晶格）
            var currentU = new SvgPoint(rotBounds.width * 1.1, 0);
            var currentV = new SvgPoint(0, rotBounds.height * 1.1);
            float currentAngle = startAngle;

            // 评估初始解
            double currentDensity = EvaluateSingleLattice(shape, currentU, currentV, currentAngle, shapeArea, out double currentUnitArea);

            // 步长初始化（相对于形状尺寸）
            double stepU = Math.Max(rotBounds.width, rotBounds.height) * InitialStepScale;
            double stepV = stepU;
            double stepAngle = 15.0; // 角度步长（度）

            var bestU = currentU;
            var bestV = currentV;
            float bestAngle = currentAngle;
            double bestDensity = currentDensity;
            double bestUnitArea = currentUnitArea;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                bool improved = false;

                // 探索 U 向量的扰动
                foreach (var delta in GetPerturbations(stepU))
                {
                    var newU = new SvgPoint(currentU.x + delta.Item1, currentU.y + delta.Item2);
                    if (newU.x == 0 && newU.y == 0) continue;

                    double density = EvaluateSingleLattice(shape, newU, currentV, currentAngle, shapeArea, out double unitArea);
                    if (density > bestDensity)
                    {
                        bestDensity = density;
                        bestU = newU;
                        bestUnitArea = unitArea;
                        improved = true;
                    }
                }

                // 探索 V 向量的扰动
                foreach (var delta in GetPerturbations(stepV))
                {
                    var newV = new SvgPoint(currentV.x + delta.Item1, currentV.y + delta.Item2);
                    if (newV.x == 0 && newV.y == 0) continue;

                    double density = EvaluateSingleLattice(shape, bestU, newV, currentAngle, shapeArea, out double unitArea);
                    if (density > bestDensity)
                    {
                        bestDensity = density;
                        bestV = newV;
                        bestUnitArea = unitArea;
                        improved = true;
                    }
                }

                // 探索角度的扰动
                foreach (var deltaAngle in new[] { -stepAngle, stepAngle })
                {
                    float newAngle = (currentAngle + (float)deltaAngle) % 360f;
                    if (newAngle < 0) newAngle += 360f;

                    double density = EvaluateSingleLattice(shape, bestU, bestV, newAngle, shapeArea, out double unitArea);
                    if (density > bestDensity)
                    {
                        bestDensity = density;
                        bestAngle = newAngle;
                        bestUnitArea = unitArea;
                        improved = true;
                    }
                }

                // 更新当前解
                if (improved)
                {
                    currentU = bestU;
                    currentV = bestV;
                    currentAngle = bestAngle;
                    currentDensity = bestDensity;
                }
                else
                {
                    // 未改进则缩小步长
                    stepU *= StepDecay;
                    stepV *= StepDecay;
                    stepAngle *= StepDecay;
                }

                // 随机扰动以避免陷入局部极值（模拟退火思想）
                if (_random.NextDouble() < 0.2)
                {
                    currentU = new SvgPoint(
                        currentU.x + (_random.NextDouble() - 0.5) * stepU * 2,
                        currentU.y + (_random.NextDouble() - 0.5) * stepU * 2);
                    currentV = new SvgPoint(
                        currentV.x + (_random.NextDouble() - 0.5) * stepV * 2,
                        currentV.y + (_random.NextDouble() - 0.5) * stepV * 2);
                    currentAngle = (currentAngle + (float)((_random.NextDouble() - 0.5) * 10)) % 360f;
                    if (currentAngle < 0) currentAngle += 360f;

                    double density = EvaluateSingleLattice(shape, currentU, currentV, currentAngle, shapeArea, out _);
                    if (density > bestDensity)
                    {
                        bestDensity = density;
                        bestU = currentU;
                        bestV = currentV;
                        bestAngle = currentAngle;
                    }
                }
            }

            return new SingleLatticeResult
            {
                U = bestU,
                V = bestV,
                Angle = bestAngle,
                Density = bestDensity,
                UnitArea = bestUnitArea
            };
        }

        /// <summary>
        /// 评估单晶格的面积利用率
        /// </summary>
        private double EvaluateSingleLattice(NFP shape, SvgPoint u, SvgPoint v, float angle, double shapeArea, out double unitArea)
        {
            // 旋转形状
            var rotated = Background.rotatePolygon(shape, angle);

            // 计算晶格单元的平行四边形面积 = |u.x * v.y - u.y * v.x|
            unitArea = Math.Abs(u.x * v.y - u.y * v.x);
            if (unitArea < 1e-6) return 0;

            // 检测晶格内相邻形状是否重叠（只需检查与 (0,0) 的 NFP）
            // 相邻单元偏移向量集合：±u, ±v, ±(u+v), ±(u-v) 等
            var offsets = new[]
            {
                u, v,
                new SvgPoint(u.x + v.x, u.y + v.y),
                new SvgPoint(u.x - v.x, u.y - v.y)
            };

            // 获取形状的自交 NFP（旋转后形状与自身的 NFP）
            var selfNfp = GetSelfNFP(rotated);

            // 检查每个偏移量是否与 NFP 相交（若偏移向量落在 NFP 内部则重叠）
            foreach (var offset in offsets)
            {
                if (IsPointInNFP(selfNfp, offset))
                    return 0; // 重叠，无效晶格
            }

            // 利用率 = 形状面积 / 单元面积
            return shapeArea / unitArea;
        }

        /// <summary>
        /// 优化双晶格（交替两种旋转角度）
        /// </summary>
        public DoubleLatticeResult OptimizeDoubleLattice(NFP shape)
        {
            var bounds = GeometryUtil.getPolygonBounds(shape);
            double shapeArea = Math.Abs(GeometryUtil.polygonArea(shape));

            var promisingAngles = GetPromisingAngles(shape);
            var candidateAngles = promisingAngles
                .Concat(new[] { 0f, 90f, 180f, 270f })
                .Distinct()
                .Take(6)               // ✅ 角度池限制为6个
                .ToList();

            DoubleLatticeResult bestResult = null;
            double bestDensity = 0;
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(5);

            // ✅ 只尝试最互补的角度组合（相差180°或90°优先）
            var anglePairs = new List<(float, float)>();
            foreach (var a1 in candidateAngles)
            {
                foreach (var a2 in candidateAngles)
                {
                    if (a1 == a2) continue;
                    float diff = Math.Abs(a1 - a2);
                    if (diff > 180) diff = 360 - diff;
                    if (diff > 80 && diff < 100) anglePairs.Insert(0, (a1, a2)); // 90°优先
                    else if (diff > 170) anglePairs.Insert(0, (a1, a2));         // 180°优先
                    else anglePairs.Add((a1, a2));
                }
            }

            foreach (var (angle1, angle2) in anglePairs.Take(20)) // 最多尝试20对组合
            {
                if ((DateTime.Now - startTime) > timeout) break;

                var result = OptimizeDoubleLatticeFromStart(shape, bounds, shapeArea, angle1, angle2);
                if (result.Density > bestDensity)
                {
                    bestDensity = result.Density;
                    bestResult = result;
                }
            }

            return bestResult ?? new DoubleLatticeResult { Density = 0 };
        }

        private DoubleLatticeResult OptimizeDoubleLatticeFromStart(NFP shape, PolygonBounds bounds, double shapeArea, float angle1, float angle2)
        {
            var rotated1 = Background.rotatePolygon(shape, angle1);
            var rotated2 = Background.rotatePolygon(shape, angle2);
            var rotBounds1 = GeometryUtil.getPolygonBounds(rotated1);
            var rotBounds2 = GeometryUtil.getPolygonBounds(rotated2);

            // 初始晶格向量：取两种旋转中较大的包围盒
            double maxW = Math.Max(rotBounds1.width, rotBounds2.width);
            double maxH = Math.Max(rotBounds1.height, rotBounds2.height);

            var currentU = new SvgPoint(maxW * 1.2, 0);
            var currentV = new SvgPoint(0, maxH * 1.2);
            float currentAngle1 = angle1;
            float currentAngle2 = angle2;

            double currentDensity = EvaluateDoubleLattice(shape, currentU, currentV, currentAngle1, currentAngle2, shapeArea, out double currentUnitArea);

            var bestU = currentU;
            var bestV = currentV;
            float bestA1 = currentAngle1;
            float bestA2 = currentAngle2;
            double bestDensity = currentDensity;
            double bestUnitArea = currentUnitArea;

            double stepU = maxW * InitialStepScale;
            double stepV = maxH * InitialStepScale;
            double stepAngle = 15.0;

            for (int iter = 0; iter < MaxIterations; iter++)
            {
                bool improved = false;

                // 扰动 U
                foreach (var delta in GetPerturbations(stepU))
                {
                    var newU = new SvgPoint(currentU.x + delta.Item1, currentU.y + delta.Item2);
                    if (newU.x == 0 && newU.y == 0) continue;
                    double d = EvaluateDoubleLattice(shape, newU, currentV, currentAngle1, currentAngle2, shapeArea, out double ua);
                    if (d > bestDensity) { bestDensity = d; bestU = newU; bestUnitArea = ua; improved = true; }
                }

                // 扰动 V
                foreach (var delta in GetPerturbations(stepV))
                {
                    var newV = new SvgPoint(currentV.x + delta.Item1, currentV.y + delta.Item2);
                    if (newV.x == 0 && newV.y == 0) continue;
                    double d = EvaluateDoubleLattice(shape, bestU, newV, currentAngle1, currentAngle2, shapeArea, out double ua);
                    if (d > bestDensity) { bestDensity = d; bestV = newV; bestUnitArea = ua; improved = true; }
                }

                // 扰动角度1
                foreach (var da in new[] { -stepAngle, stepAngle })
                {
                    float na = (currentAngle1 + (float)da) % 360f; if (na < 0) na += 360f;
                    double d = EvaluateDoubleLattice(shape, bestU, bestV, na, currentAngle2, shapeArea, out double ua);
                    if (d > bestDensity) { bestDensity = d; bestA1 = na; bestUnitArea = ua; improved = true; }
                }

                // 扰动角度2
                foreach (var da in new[] { -stepAngle, stepAngle })
                {
                    float na = (currentAngle2 + (float)da) % 360f; if (na < 0) na += 360f;
                    double d = EvaluateDoubleLattice(shape, bestU, bestV, bestA1, na, shapeArea, out double ua);
                    if (d > bestDensity) { bestDensity = d; bestA2 = na; bestUnitArea = ua; improved = true; }
                }

                if (improved)
                {
                    currentU = bestU;
                    currentV = bestV;
                    currentAngle1 = bestA1;
                    currentAngle2 = bestA2;
                    currentDensity = bestDensity;
                }
                else
                {
                    stepU *= StepDecay;
                    stepV *= StepDecay;
                    stepAngle *= StepDecay;
                }

                // 随机跳跃
                if (_random.NextDouble() < 0.15)
                {
                    currentU = new SvgPoint(currentU.x + (_random.NextDouble() - 0.5) * stepU * 2,
                                            currentU.y + (_random.NextDouble() - 0.5) * stepU * 2);
                    currentV = new SvgPoint(currentV.x + (_random.NextDouble() - 0.5) * stepV * 2,
                                            currentV.y + (_random.NextDouble() - 0.5) * stepV * 2);
                    currentAngle1 = (currentAngle1 + (float)((_random.NextDouble() - 0.5) * 10)) % 360f;
                    if (currentAngle1 < 0) currentAngle1 += 360f;
                    currentAngle2 = (currentAngle2 + (float)((_random.NextDouble() - 0.5) * 10)) % 360f;
                    if (currentAngle2 < 0) currentAngle2 += 360f;
                    double d = EvaluateDoubleLattice(shape, currentU, currentV, currentAngle1, currentAngle2, shapeArea, out _);
                    if (d > bestDensity)
                    {
                        bestDensity = d;
                        bestU = currentU;
                        bestV = currentV;
                        bestA1 = currentAngle1;
                        bestA2 = currentAngle2;
                    }
                }
            }

            return new DoubleLatticeResult
            {
                U = bestU,
                V = bestV,
                Angle1 = bestA1,
                Angle2 = bestA2,
                Density = bestDensity,
                UnitArea = bestUnitArea
            };
        }

        /// <summary>
        /// 评估双晶格：两个交替形状的晶格单元（包含两个形状）
        /// 晶格单元面积 = |u.x * v.y - u.y * v.x|
        /// 需要检测四个重叠情况：angle1与angle2在(0,0)和(u,0)以及(0,v)等
        /// </summary>
        private double EvaluateDoubleLattice(NFP shape, SvgPoint u, SvgPoint v, float angle1, float angle2, double shapeArea, out double unitArea)
        {
            unitArea = Math.Abs(u.x * v.y - u.y * v.x);
            if (unitArea < 1e-6) return 0;

            var rotated1 = Background.rotatePolygon(shape, angle1);
            var rotated2 = Background.rotatePolygon(shape, angle2);

            // 需要检测的重叠关系：
            // 1) rotated1 与 rotated2 (在原点)
            // 2) rotated1 与 rotated2 偏移 u
            // 3) rotated1 与 rotated2 偏移 v
            // 4) rotated1 与 rotated1 偏移 u (同一角度内)
            // 5) rotated2 与 rotated2 偏移 v

            // 检测 rotated1 与 rotated2 在原点是否重叠
            if (CheckOverlap(rotated1, rotated2, new SvgPoint(0, 0)))
                return 0;

            // 检测各种偏移
            if (CheckOverlap(rotated1, rotated2, u)) return 0;
            if (CheckOverlap(rotated1, rotated2, v)) return 0;
            if (CheckOverlap(rotated1, rotated1, u)) return 0;
            if (CheckOverlap(rotated2, rotated2, v)) return 0;
            // 也可加 u+v 偏移检测

            // 利用率 = (2 * shapeArea) / unitArea
            return (2 * shapeArea) / unitArea;
        }

        /// <summary>
        /// 检测两个形状在给定偏移下是否重叠（利用 NFP）
        /// </summary>
        private bool CheckOverlap(NFP shapeA, NFP shapeB, SvgPoint offset)
        {
            // 获取 shapeA 和 shapeB 之间的 NFP
            var nfp = GetNFPBetween(shapeA, shapeB);
            // 如果 offset 落在 NFP 内部，则重叠
            return IsPointInNFP(nfp, offset);
        }

        #region NFP 辅助方法（利用现有缓存）

        /// <summary>
        /// 获取形状自身的 NFP（用于单晶格自重叠检测）
        /// </summary>
        private NFP GetSelfNFP(NFP shape)
        {
            // 利用现有的 getOuterNfp，但需传入相同形状且旋转角相同
            // 注意：现有 getOuterNfp 返回的是 NFP 对象，需要保证 shape.rotation 设置正确
            var shapeCopy = shape.Clone(); // 假设 NFP 有 Clone 方法
            shapeCopy.rotation = shape.rotation;
            var nfp = Background.getOuterNfp(shapeCopy, shapeCopy, 0);
            return nfp;
        }

        /// <summary>
        /// 获取两个不同旋转形状之间的 NFP
        /// </summary>
        private NFP GetNFPBetween(NFP shapeA, NFP shapeB)
        {
            // 临时设置旋转角
            var aCopy = shapeA.Clone();
            var bCopy = shapeB.Clone();
            aCopy.rotation = shapeA.rotation;
            bCopy.rotation = shapeB.rotation;
            return Background.getOuterNfp(aCopy, bCopy, 0);
        }

        /// <summary>
        /// 判断点是否在 NFP 内部（包括边界）
        /// </summary>
        private bool IsPointInNFP(NFP nfp, SvgPoint point)
        {
            if (nfp == null || nfp.length == 0) return false;

            // 利用射线法判断点是否在多边形内部
            // 简化实现：调用 GeometryUtil 中类似方法（假设存在 PointInPolygon）
            return GeometryUtil.PointInPolygon(point, nfp);

            // 如果没有 PointInPolygon，可用 Clipper 判断：
            // var intPoint = new IntPoint(point.x * scale, point.y * scale);
            // return Clipper.PointInPolygon(intPoint, nfpToIntPointArray(nfp)) != 0;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取扰动量集合（上下左右及对角线）
        /// </summary>
        private IEnumerable<(double, double)> GetPerturbations(double step)
        {
            yield return (step, 0);
            yield return (-step, 0);
            yield return (0, step);
            yield return (0, -step);
            yield return (step, step);
            yield return (-step, -step);
            yield return (step, -step);
            yield return (-step, step);
        }

        /// <summary>
        /// 获取形状的 PAH 启发式角度（凸包边方向及垂直方向）
        /// </summary>
        private List<float> GetPromisingAngles(NFP shape)
        {
            var angles = new List<float>();
            var hull = Background.getHull(shape);
            for (int i = 0; i < hull.length; i++)
            {
                var next = (i + 1) % hull.length;
                var dx = hull[next].x - hull[i].x;
                var dy = hull[next].y - hull[i].y;
                var angle = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);
                angles.Add(angle);
                angles.Add((angle + 90) % 360);
            }
            return angles.Distinct().ToList();
        }

        #endregion
    }
}