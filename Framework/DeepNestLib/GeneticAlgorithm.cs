using System;
using System.Collections.Generic;
using System.Linq;

namespace DeepNestLib
{
    public class GeneticAlgorithm
    {
        SvgNestConfig Config;
        public List<PopulationItem> population;

        public static bool StrictAngles = false;
        float[] defaultAngles = new float[] {
        0,
0,
90,
0,
0,
270,
180,
180,
180,
90

        };

        // 新增：自适应参数控制
        public bool UseAdaptiveParameters { get; set; } = true;
        public bool UseNiche { get; set; } = true;          // 小生境（排挤机制）
        public float NicheSimilarityThreshold { get; set; } = 0.85f; // 相似度阈值

        // 动态参数范围
        private float minCrossoverRate = 0.6f;
        private float maxCrossoverRate = 0.9f;
        private float minMutationRate = 0.01f;
        private float maxMutationRate = 0.1f;

        // 当前代的参数（在 generation 开始时计算）
        private float currentCrossoverRate;
        private float currentMutationRate;

        public GeneticAlgorithm(NFP[] adam, SvgNestConfig config)
        {
            List<float> ang2 = new List<float>();
            for (int i = 0; i < adam.Length; i++)
            {
                ang2.Add((i * 90) % 360);
            }
            defaultAngles = ang2.ToArray();
            Config = config;


            List<float> angles = new List<float>();
            List<float> angles1 = new List<float>();
            for (int i = 0; i < adam.Length; i++)
            {
                //原始状态进行初始种群计算
                angles.Add(0);//初始0度状态，
                angles1.Add(90);//初始90度状态
            }


            population = new List<PopulationItem>();
            population.Add(new PopulationItem() { placements = adam.ToList(), Rotation = angles.ToArray() });
            population.Add(new PopulationItem() { placements = adam.ToList(), Rotation = angles1.ToArray() });
            population.Add(new PopulationItem() { placements = adam.ToList(), Rotation = ang2.ToArray() });

            var mixedRotations = new float[adam.Length];
            for (int i = 0; i < adam.Length; i++)
            {
                mixedRotations[i] = (r.NextDouble() < 0.5) ? 0 : 90;
            }
            population.Add(new PopulationItem() { placements = adam.ToList(), Rotation = mixedRotations });


            //// 新增：PAH 启发式个体（平行角度）
            ////var convexHull = GeometryUtil.getPolygonBounds(adam[0]); // 同种形状
            //var promisingAngles = GetPromisingAngles(adam[0]);
            //foreach (var angle in promisingAngles)
            //{
            //    var rotations = new float[adam.Length];
            //    for (int i = 0; i < rotations.Length; i++)
            //        rotations[i] = angle;
            //    population.Add(new PopulationItem()
            //    {
            //        placements = adam.ToList(),
            //        Rotation = rotations
            //    });
            //}

            // 新增：Double-Lattice 交替旋转个体
            var latticeRotations = new float[adam.Length];
            for (int i = 0; i < adam.Length; i++)
                latticeRotations[i] = (i % 2 == 0) ? 0 : 180;
            population.Add(new PopulationItem()
            {
                placements = adam.ToList(),
                Rotation = latticeRotations
            });

            while (population.Count() < config.populationSize)
            {
                var mutant = this.mutate(population[0]);
                population.Add(mutant);
            }

            //// 1. 识别形状类型（假设 adam 数组中可能存在多种形状，但我们主要针对同质批量情况）
            //var shapeGroups = adam.GroupBy(s => s.source).Where(g => g.Count() >= 2).ToList();
            //int groupIdCounter = 0;
            //foreach (var group in shapeGroups)
            //{
            //    var shape = group.First();
            //    //var indices = adam.Select((s, idx) => new { s, idx }).Where(x => x.s.source == shape.source).Select(x => x.idx).ToArray();

            //    var indices = adam.Select((p, idx) => new { p, idx })
            //          .Where(x => x.p.source == group.Key)
            //          .Select(x => x.idx).ToList();
            //    // 模式1：单晶格，全部同角度（0°）
            //    var item1 = new PopulationItem { placements = adam.ToList(), Rotation = new float[adam.Length] };
            //    for (int i = 0; i < item1.Rotation.Length; i++) item1.Rotation[i] = 0;
            //    foreach (var idx in indices) item1.Rotation[idx] = 0;
            //    item1.PatternGroups.Add(new PatternGroup
            //    {
            //        GroupId = groupIdCounter++,
            //        PatternType = 1,
            //        MemberIndices = indices,
            //        BaseAngle = 0,
            //        SecondAngle = 0,
            //        LatticeU = new SvgPoint(group.First().WidthCalculated * 1.05, 0),
            //        LatticeV = new SvgPoint(0, group.First().HeightCalculated * 1.05)
            //    });
            //    population.Add(item1);

            //    // 模式2：双晶格交替（0°和180°）
            //    var item2 = new PopulationItem { placements = adam.ToList(), Rotation = new float[adam.Length] };
            //    for (int i = 0; i < item2.Rotation.Length; i++) item2.Rotation[i] = 0;
            //    for (int i = 0; i < indices.Count; i++)
            //        item2.Rotation[indices[i]] = (i % 2 == 0) ? 0f : 180f;
            //    item2.PatternGroups.Add(new PatternGroup
            //    {
            //        GroupId = groupIdCounter++,
            //        PatternType = 2,
            //        MemberIndices = indices,
            //        BaseAngle = 0,
            //        SecondAngle = 180,
            //        LatticeU = new SvgPoint(group.First().WidthCalculated * 1.1, 0),
            //        LatticeV = new SvgPoint(0, group.First().HeightCalculated * 1.1)
            //    });
            //    population.Add(item2);

            //    // 1.3 混合角度：PAH 中的两个互补角度交替
            //    var pahAngles = GetPromisingAngles(shape);
            //    if (pahAngles.Count >= 2)
            //    {
            //        var mixRotations = new float[adam.Length];
            //        for (int i = 0; i < mixRotations.Length; i++) mixRotations[i] = 0;
            //        for (int i = 0; i < indices.Count; i++)
            //        {
            //            mixRotations[indices[i]] = pahAngles[i % pahAngles.Count];
            //        }
            //        population.Add(new PopulationItem { placements = adam.ToList(), Rotation = mixRotations });
            //    }
            //}

            //// 如果种群数量仍不足，用随机变异补齐（保持原有逻辑）
            //while (population.Count < config.populationSize)
            //{
            //    population.Add(mutate(population[0]));
            //}
        }
        private List<float> GetPromisingAngles(NFP shape)
        {
            // 提取凸包边，计算使边平行于板材边界的角度
            var angles = new List<float>();
            var hull =Background.getHull(shape);
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

        public PopulationItem mutate(PopulationItem p, float? mutationRate = null)
        {
            float rate = mutationRate ?? (float)(Config.mutationRate * 0.01); // 原有 Config.mutationRate 是倍率因子
            if (UseAdaptiveParameters) rate = currentMutationRate;


            var clone = new PopulationItem();
            clone.placements = p.placements.ToList();
            clone.Rotation = p.Rotation.Clone() as float[];

            for (int i = 0; i < clone.placements.Count; i++)
            {
                var rand = r.NextDouble();
               
                if (rand < rate)
                {
                    // 随机选择变异类型：交换相邻、随机交换、反转片段、插入片段
                    int type = r.Next(4);
                    switch (type)
                    {
                        case 0: // 交换相邻
                            if (i + 1 < clone.placements.Count)
                            {
                                var temp = clone.placements[i];
                                clone.placements[i] = clone.placements[i + 1];
                                clone.placements[i + 1] = temp;
                            }
                            break;
                        case 1: // 随机交换两个位置
                            int j = r.Next(clone.placements.Count);
                            var temp2 = clone.placements[i];
                            clone.placements[i] = clone.placements[j];
                            clone.placements[j] = temp2;
                            break;
                        case 2: // 反转一个子序列
                            int start = r.Next(clone.placements.Count);
                            int end = r.Next(start, clone.placements.Count);
                            clone.placements.Reverse(start, end - start + 1);
                            break;
                        case 3: // 移动一个元素到另一个位置
                            int from = i;
                            int to = r.Next(clone.placements.Count);
                            var item = clone.placements[from];
                            clone.placements.RemoveAt(from);
                            clone.placements.Insert(to, item);
                            break;
                    }
                }

                // 角度变异（使用自适应变异率）
                rand = r.NextDouble();
                if (rand < rate)
                {
                    if (Config.rotations > 1)
                        clone.Rotation[i] = (float)Math.Floor(r.NextDouble() * Config.rotations) * (360f / Config.rotations);
                    else
                        clone.Rotation[i] = (float)(r.NextDouble() * 360);
                }

                // 5. 批量对齐变异：将连续一段同质形状设置为相同角度
                if (r.NextDouble() < rate * 0.2) // 20% 概率
                {
                    int start = r.Next(clone.placements.Count);
                    int length = r.Next(2, Math.Max(3, clone.placements.Count / 5));
                    int source = clone.placements[start].source ?? -1;
                    if (source != -1)
                    {
                        // 找到具有相同 source 的连续段
                        List<int> sameSourceIndices = new List<int>();
                        for (int k = start; k < Math.Min(start + length * 2, clone.placements.Count); k++)
                        {
                            if (clone.placements[k].source == source)
                                sameSourceIndices.Add(k);
                        }
                        if (sameSourceIndices.Count >= 2)
                        {
                            float commonAngle = (float)(r.NextDouble() * 360);
                            if (Config.rotations > 1)
                                commonAngle = (float)Math.Floor(r.NextDouble() * Config.rotations) * (360f / Config.rotations);
                            foreach (var idx in sameSourceIndices)
                                clone.Rotation[idx] = commonAngle;
                        }
                    }
                }

                // 6. 交替角度变异：将一段同质形状设为交替的两个角度
                if (r.NextDouble() < rate * 0.15)
                {
                    int start = r.Next(clone.placements.Count);
                    int source = clone.placements[start].source ?? -1;
                    if (source != -1)
                    {
                        var indices = clone.placements
                            .Select((pp, idx) => new { pp, idx })
                            .Where(x => x.pp.source == source)
                            .Select(x => x.idx)
                            .OrderBy(x => x)
                            .ToList();
                        if (indices.Count >= 4)
                        {
                            float angle1 = (float)(r.NextDouble() * 360);
                            float angle2 = (angle1 + 180) % 360;
                            if (Config.rotations > 1)
                            {
                                angle1 = (float)Math.Floor(r.NextDouble() * Config.rotations) * (360f / Config.rotations);
                                angle2 = (angle1 + 180) % 360;
                            }
                            for (int k = 0; k < indices.Count; k++)
                            {
                                clone.Rotation[indices[k]] = (k % 2 == 0) ? angle1 : angle2;
                            }
                        }
                    }
                }
            }
            return clone;
        }
        Random r = new Random();
        public float[] shuffleArray(float[] array)
        {
            for (var i = array.Length - 1; i > 0; i--)
            {
                var j = (int)Math.Floor(r.NextDouble() * (i + 1));
                var temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
            return array;
        }


        // returns a random individual from the population, weighted to the front of the list (lower fitness value is more likely to be selected)
        public PopulationItem randomWeightedIndividual(PopulationItem exclude = null)
        {
            //var pop = this.population.slice(0);
            var pop = this.population.ToArray();

            if (exclude != null && Array.IndexOf(pop, exclude) >= 0)
            {
                //pop.splice(Array.IndexOf(pop, exclude), 1);
                pop = pop.splice(Array.IndexOf(pop, exclude), 1);
            }

            var rand = r.NextDouble();

            float lower = 0;
            var weight = 1 / (float)pop.Length;
            float upper = weight;

            for (var i = 0; i < pop.Length; i++)
            {
                // if the random number falls between lower and upper bounds, select this individual
                if (rand > lower && rand < upper)
                {
                    return pop[i];
                }
                lower = upper;
                upper += 2 * weight * ((pop.Length - i) / (float)pop.Length);
            }

            return pop[0];
        }

        // single point crossover
        public PopulationItem[] mate(PopulationItem male, PopulationItem female)
        {
            var cutpoint = (int)Math.Round(Math.Min(Math.Max(r.NextDouble(), 0.1), 0.9) * (male.placements.Count - 1));

            var gene1 = new List<NFP>(male.placements.Take(cutpoint).ToArray());
            var rot1 = new List<float>(male.Rotation.Take(cutpoint).ToArray());

            var gene2 = new List<NFP>(female.placements.Take(cutpoint).ToArray());
            var rot2 = new List<float>(female.Rotation.Take(cutpoint).ToArray());

            int i = 0;

            for (i = 0; i < female.placements.Count; i++)
            {
                if (!gene1.Any(z => z.id == female.placements[i].id))
                {
                    gene1.Add(female.placements[i]);
                    rot1.Add(female.Rotation[i]);
                }
            }

            for (i = 0; i < male.placements.Count; i++)
            {
                if (!gene2.Any(z => z.id == male.placements[i].id))
                {
                    gene2.Add(male.placements[i]);
                    rot2.Add(male.Rotation[i]);
                }
            }

            return new[] {new  PopulationItem() {
                placements= gene1, Rotation= rot1.ToArray()},
                new PopulationItem(){ placements= gene2, Rotation= rot2.ToArray()}};
        }

        public void generation()
        {
            // 1. 按适应度排序（升序：fitness 越小越好）
            population = population.OrderBy(z => z.fitness).ToList();

            // 2. 更新自适应参数（基于当前种群多样性）
            UpdateAdaptiveParameters();

            // 3. 精英保留：保留前 eliteCount 个最优个体（至少1个）
            int eliteCount = Math.Max(1, (int)(population.Count * 0.05));
            var newPopulation = new List<PopulationItem>(population.Take(eliteCount));

            // 4. 生成子代直到填满原种群大小
            while (newPopulation.Count < population.Count)
            {
                // 选择父代（加权随机，排除已选）
                var male = randomWeightedIndividual();
                var female = randomWeightedIndividual(male);

                // 根据自适应交叉率决定是否交叉
                PopulationItem child1, child2;
                if (r.NextDouble() < currentCrossoverRate)
                {
                    var children = mate(male, female);
                    child1 = children[0];
                    child2 = children[1];
                }
                else
                {
                    // 不交叉则直接复制父代（深拷贝）
                    child1 = CloneIndividual(male);
                    child2 = CloneIndividual(female);
                }

                // 变异（使用自适应变异率）
                child1 = mutate(child1, currentMutationRate);
                child2 = mutate(child2, currentMutationRate);

                // 将子代加入新种群，确保不超过种群大小
                if (UseNiche)
                {
                    // 排挤模式：子代与当前新种群中最相似的个体竞争（不是与原种群竞争）
                    AddWithCrowding(newPopulation, child1);
                    if (newPopulation.Count < population.Count)
                        AddWithCrowding(newPopulation, child2);
                }
                else
                {
                    newPopulation.Add(child1);
                    if (newPopulation.Count < population.Count)
                        newPopulation.Add(child2);
                }
            }

            // 5. 替换旧种群
            population = newPopulation;
        }
        /// <summary>
        /// 向种群中添加个体，如果种群已满，则替换与 newIndividual 最相似的个体（若其适应度更差）
        /// </summary>
        private void AddWithCrowding(List<PopulationItem> pop, PopulationItem newInd)
        {
            if (pop.Count < population.Count)
            {
                pop.Add(newInd);
                return;
            }

            // 找出 pop 中与 newInd 最相似的个体
            int mostSimilarIdx = -1;
            float maxSim = -1;
            for (int i = 0; i < pop.Count; i++)
            {
                float sim = Similarity(newInd, pop[i]);
                if (sim > maxSim)
                {
                    maxSim = sim;
                    mostSimilarIdx = i;
                }
            }

            // 如果新个体适应度更好（更小），则替换
            if (mostSimilarIdx >= 0 && newInd.fitness < pop[mostSimilarIdx].fitness)
                pop[mostSimilarIdx] = newInd;
        }
        private PopulationItem CloneIndividual(PopulationItem source)
        {
            return new PopulationItem
            {
                placements = source.placements.Select(nfp => nfp.Clone()).ToList(), // 需要 NFP 实现 Clone
                Rotation = source.Rotation.ToArray(),
                fitness = source.fitness
            };
        }
        /// <summary>
        /// 计算两个个体的相似度（0~1，1表示完全相同）
        /// </summary>
        private float Similarity(PopulationItem a, PopulationItem b)
        {
            if (a.placements.Count != b.placements.Count) return 0;
            int match = 0;
            for (int i = 0; i < a.placements.Count; i++)
            {
                if (a.placements[i].id == b.placements[i].id)
                    match++;
            }
            return (float)match / a.placements.Count;
        }

        /// <summary>
        /// 计算当前种群的多样性（平均两两相似度，值越低多样性越高）
        /// </summary>
        private float PopulationDiversity()
        {
            if (population.Count < 2) return 0;
            float totalSim = 0;
            int pairs = 0;
            for (int i = 0; i < population.Count - 1; i++)
            {
                for (int j = i + 1; j < population.Count; j++)
                {
                    totalSim += Similarity(population[i], population[j]);
                    pairs++;
                }
            }
            return 1f - (totalSim / pairs); // 返回多样性：1 - 平均相似度
        }
        /// <summary>
        /// 在 generation() 开始时调用
        /// </summary>
        private void UpdateAdaptiveParameters()
        {
            if (!UseAdaptiveParameters) return;

            float diversity = PopulationDiversity();
            // 多样性低 -> 增加变异率，降低交叉率（探索新区域）
            // 多样性高 -> 降低变异率，增加交叉率（利用现有模式）
            currentMutationRate = minMutationRate + (1f - diversity) * (maxMutationRate - minMutationRate);
            currentCrossoverRate = minCrossoverRate + diversity * (maxCrossoverRate - minCrossoverRate);

            // 边界裁剪
            currentMutationRate = Clamp(currentMutationRate, minMutationRate, maxMutationRate);
            currentCrossoverRate = Clamp(currentCrossoverRate, minCrossoverRate, maxCrossoverRate);
        }

        public float Clamp(float value, float min, float max)
        {
            if (min > max)
                throw new ArgumentOutOfRangeException(nameof(min), "最小值不能大于最大值");

            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 排挤替换：新个体与种群中最相似的个体比较，若新个体适应度更好则替换
        /// </summary>
        private void CrowdingReplacement(PopulationItem newIndividual)
        {
            if (population.Count == 0) return;

            // 找出最相似的个体（最小相似度距离？实际是最相似，即 similarity 最大）
            float maxSim = -1;
            int mostSimilarIdx = -1;
            for (int i = 0; i < population.Count; i++)
            {
                float sim = Similarity(newIndividual, population[i]);
                if (sim > maxSim)
                {
                    maxSim = sim;
                    mostSimilarIdx = i;
                }
            }

            if (mostSimilarIdx >= 0 && newIndividual.fitness < population[mostSimilarIdx].fitness) // 假设 fitness 越小越好
            {
                population[mostSimilarIdx] = newIndividual;
            }
        }
    }
}