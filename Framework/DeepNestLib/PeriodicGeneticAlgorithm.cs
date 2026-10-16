using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeepNestLib
{
    public class PeriodicIndividual
    {
        public double Angle;   // 度
        public double Vx0, Vy0, Vx1, Vy1;
        public int Count;      // 能排下的零件数量
        public double Fitness; // Count * partArea / sheetArea
    }

    public class PeriodicGeneticAlgorithm
    {
        private NFP part;
        private SvgNestConfig config;
        private NFP sheet;
        private double sheetArea;
        private double partArea;
        private Random rand = new Random();

        public PeriodicGeneticAlgorithm(NFP part, SvgNestConfig config, NFP sheet)
        {
            this.part = part;
            this.config = config;
            this.sheet = sheet;
            this.sheetArea = Math.Abs(GeometryUtil.polygonArea(sheet));
            this.partArea = Math.Abs(GeometryUtil.polygonArea(part));
        }

        public PeriodicIndividual Run(int generations = 100, int popSize = 50)
        {
            var population = InitializePopulation(popSize);
            for (int gen = 0; gen < generations; gen++)
            {
                // 评估适应度
                foreach (var ind in population)
                    EvaluateFitness(ind);
                // 排序
                population = population.OrderByDescending(x => x.Fitness).ToList();
                // 精英保留
                var newPop = new List<PeriodicIndividual> { population[0] };
                while (newPop.Count < popSize)
                {
                    var parent1 = TournamentSelect(population);
                    var parent2 = TournamentSelect(population);
                    var child = Crossover(parent1, parent2);
                    Mutate(child);
                    newPop.Add(child);
                }
                population = newPop;
            }
            return population.OrderByDescending(x => x.Fitness).First();
        }

        private List<PeriodicIndividual> InitializePopulation(int size)
        {
            var pop = new List<PeriodicIndividual>();
            for (int i = 0; i < size; i++)
            {
                var ind = new PeriodicIndividual();
                ind.Angle = rand.NextDouble() * 360;
                // 随机晶格向量，大小约为零件包围盒尺寸的1.5~3倍
                var bounds = GeometryUtil.getPolygonBounds(part);
                double w = bounds.width, h = bounds.height;
                ind.Vx0 = (rand.NextDouble() * 1.5 + 1.0) * w;
                ind.Vy0 = (rand.NextDouble() - 0.5) * w * 0.5;
                ind.Vx1 = (rand.NextDouble() - 0.5) * h * 0.5;
                ind.Vy1 = (rand.NextDouble() * 1.5 + 1.0) * h;
                pop.Add(ind);
            }
            return pop;
        }

        private void EvaluateFitness(PeriodicIndividual ind)
        {
            // 旋转零件
            var rotated = Background.rotatePolygon(part, (float)ind.Angle);
            var bounds = GeometryUtil.getPolygonBounds(rotated);
            double partW = bounds.width, partH = bounds.height;

            // 估算 i,j 范围
            double W = sheet.Points.Max(p => p.x) - sheet.Points.Min(p => p.x);
            double H = sheet.Points.Max(p => p.y) - sheet.Points.Min(p => p.y);
            int imax = (int)Math.Floor(W / Math.Abs(ind.Vx0)) + 2;
            int jmax = (int)Math.Floor(H / Math.Abs(ind.Vy1)) + 2;
            int count = 0;
            for (int i = 0; i <= imax; i++)
            {
                for (int j = 0; j <= jmax; j++)
                {
                    double x = i * ind.Vx0 + j * ind.Vx1;
                    double y = i * ind.Vy0 + j * ind.Vy1;
                    // 简单检查：零件外接矩形是否在板材内（更精确可用Clipper）
                    if (x + partW <= W && y + partH <= H && x >= 0 && y >= 0)
                        count++;
                    else
                        break; // 优化：由于j递增，超出后可跳出内循环
                }
            }
            ind.Count = count;
            ind.Fitness = (count * partArea) / sheetArea;
        }

        private PeriodicIndividual Crossover(PeriodicIndividual a, PeriodicIndividual b)
        {
            var child = new PeriodicIndividual();
            child.Angle = rand.NextDouble() < 0.5 ? a.Angle : b.Angle;
            child.Vx0 = rand.NextDouble() < 0.5 ? a.Vx0 : b.Vx0;
            child.Vy0 = rand.NextDouble() < 0.5 ? a.Vy0 : b.Vy0;
            child.Vx1 = rand.NextDouble() < 0.5 ? a.Vx1 : b.Vx1;
            child.Vy1 = rand.NextDouble() < 0.5 ? a.Vy1 : b.Vy1;
            return child;
        }

        private void Mutate(PeriodicIndividual ind, double rate = 0.1)
        {
            if (rand.NextDouble() < rate) ind.Angle += (rand.NextDouble() - 0.5) * 20;
            if (rand.NextDouble() < rate) ind.Vx0 += (rand.NextDouble() - 0.5) * 10;
            if (rand.NextDouble() < rate) ind.Vy0 += (rand.NextDouble() - 0.5) * 10;
            if (rand.NextDouble() < rate) ind.Vx1 += (rand.NextDouble() - 0.5) * 10;
            if (rand.NextDouble() < rate) ind.Vy1 += (rand.NextDouble() - 0.5) * 10;
            // 限制范围避免过小或过大
            ind.Vx0 = Math.Max(1, ind.Vx0);
            ind.Vy1 = Math.Max(1, ind.Vy1);
        }

        private PeriodicIndividual TournamentSelect(List<PeriodicIndividual> pop)
        {
            int k = 3;
            var best = pop[rand.Next(pop.Count)];
            for (int i = 1; i < k; i++)
            {
                var contender = pop[rand.Next(pop.Count)];
                if (contender.Fitness > best.Fitness) best = contender;
            }
            return best;
        }
    }
}
