using System;
using System.Collections.Generic;
using System.Diagnostics;
using DeepNestLib;

namespace DeepNestOverlapSmoke
{
    internal static class Program
    {
        private static int Main()
        {
            var board = new NFP { id = 1, source = 11 };
            board.AddPoint(new SvgPoint(0, 0));
            board.AddPoint(new SvgPoint(100, 0));
            board.AddPoint(new SvgPoint(100, 200));
            board.AddPoint(new SvgPoint(0, 200));

            var partA = Rectangle(0, 21, 30, 20);
            var partB = Rectangle(1, 22, 30, 20);
            var original = new SheetPlacement
            {
                fitness = 65.5,
                placements = new[]
                {
                    new List<SheetPlacementItem>
                    {
                        new SheetPlacementItem
                        {
                            sheetId = 1,
                            sheetSource = 11,
                            sheetplacements = new List<PlacementItem>
                            {
                                new PlacementItem { id = 0, source = 21, rotation = 0, x = 0, y = 0 },
                                new PlacementItem { id = 1, source = 22, rotation = 0, x = 35, y = 0 }
                            }
                        }
                    }
                }
            };
            var config = new SvgNestConfig { rotations = 1, clipperScale = 10000000 };
            Func<SheetPlacement, double> score = candidate =>
            {
                var items = candidate.placements[0][0].sheetplacements;
                double minX = double.MaxValue, maxX = double.MinValue;
                foreach (var item in items)
                {
                    minX = Math.Min(minX, item.x);
                    maxX = Math.Max(maxX, item.x + 30);
                }
                return maxX - minX;
            };

            var result = OverlapRemovalOptimizer.Optimize(
                original, new[] { board }, new[] { partA, partB }, config, score, 10000);
            if (result == null || result.fitness == null || result.fitness.Value >= 65.5)
                return Fail("Optimizer did not improve the compressed placement.");
            if (original.placements[0][0].sheetplacements[1].x != 35 ||
                original.placements[0][0].sheetplacements[1].y != 0)
                return Fail("The input placement was modified.");
            Console.WriteLine("PASS: repaired placement improved width from 65 to " + result.fitness.Value);
            var benchmarkParts = new List<NFP>();
            var benchmarkItems = new List<PlacementItem>();
            for (int i = 0; i < 40; i++)
            {
                benchmarkParts.Add(Rectangle(i, 100 + i, 10, 10));
                benchmarkItems.Add(new PlacementItem { id = i, source = 100 + i, x = (i % 10) * 10, y = (i / 10) * 10 });
            }
            var benchmark = new SheetPlacement { placements = new[] { new List<SheetPlacementItem> {
                new SheetPlacementItem { sheetId = 1, sheetSource = 11, sheetplacements = benchmarkItems } } } };
            Func<SheetPlacement, double> benchmarkScore = candidate => Background.EvaluatePlacementFitness(candidate,
                new[] { board }, benchmarkParts.ToArray());
            var watch = Stopwatch.StartNew();
            var benchmarkResult = OverlapRemovalOptimizer.Optimize(benchmark, new[] { board }, benchmarkParts.ToArray(),
                config, benchmarkScore, 3000);
            watch.Stop();
            if (ReferenceEquals(benchmark, benchmarkResult) || !benchmarkResult.fitness.HasValue ||
                benchmarkResult.fitness.Value >= benchmarkScore(benchmark))
                return Fail("Dense-layout repair did not improve fitness.");
            var repaired = benchmarkResult.placements[0][0].sheetplacements;
            for (int i = 0; i < repaired.Count; i++)
            {
                if (benchmarkItems[i].x != (i % 10) * 10 || benchmarkItems[i].y != (i / 10) * 10)
                    return Fail("Dense-layout input was modified.");
                if (repaired[i].x < -1e-7 || repaired[i].y < -1e-7 ||
                    repaired[i].x + 10 > 100 + 1e-7 || repaired[i].y + 10 > 200 + 1e-7)
                    return Fail("A repaired part is outside the board.");
                for (int j = 0; j < i; j++)
                    if (Math.Min(repaired[i].x + 10, repaired[j].x + 10) - Math.Max(repaired[i].x, repaired[j].x) > 1e-7 &&
                        Math.Min(repaired[i].y + 10, repaired[j].y + 10) - Math.Max(repaired[i].y, repaired[j].y) > 1e-7)
                        return Fail("Repaired rectangles overlap.");
            }
            var unchanged = OverlapRemovalOptimizer.Optimize(benchmark, new[] { board }, benchmarkParts.ToArray(),
                config, benchmarkScore, 0);
            if (!ReferenceEquals(benchmark, unchanged)) return Fail("Zero budget did not preserve the input.");
            var tied = OverlapRemovalOptimizer.Optimize(original, new[] { board }, new[] { partA, partB },
                config, candidate => 1, 1000);
            if (!ReferenceEquals(original, tied)) return Fail("Equal fitness replaced the original.");
            Console.WriteLine("BENCH: 40 parts, elapsed=" + watch.ElapsedMilliseconds + "ms, improved=" +
                (!ReferenceEquals(benchmark, benchmarkResult)));
            Console.WriteLine("PASS: dense repair, bounds, non-overlap, input preservation, zero budget and equal-score fallback");
            if (!ExactFit(300, 100, 100, 100, 3) || !ExactFit(300, 120, 100, 100, 3) ||
                !ExactFit(100, 100, 100, 100, 1) || !ExactFit(299.99, 100, 100, 100, 2) ||
                !ExactFit(300, 100, 101, 100, 2) || !ExactFit(150, 100, 100, 50, 3, 90) ||
                !ExactFit(300, 100, 100, 98, 2, 0, 1)) return 1;
            return 0;
        }

        private static bool ExactFit(double width, double height, double partWidth, double partHeight, int expected,
            float rotation = 0, double spacing = 0)
        {
            foreach (int prefix in new[] { -1, 0, 6 })
            {
                Background.window = new windowUnk();
                Background.cacheProcess.Clear();
                var sheet = Rectangle(0, 1000, width, height);
                var parts = new[] { Rectangle(0, 1, partWidth, partHeight), Rectangle(1, 1, partWidth, partHeight),
                    Rectangle(2, 1, partWidth, partHeight) };
                var config = new SvgNestConfig { rotations = 1, spacing = spacing, PlaceParts1LookaheadParts = Math.Max(0, prefix) };
                foreach (var part in parts)
                {
                    if (spacing > 0) SvgNest.offsetTree(part, spacing / 2, config);
                    part.rotation = rotation;
                }
                var result = prefix < 0 ? Background.placeParts(new[] { sheet }, parts, config, 0) :
                    Background.placeParts1(new[] { sheet }, parts, config, 0);
                int count = 0;
                foreach (var group in result.placements)
                    foreach (var board in group)
                    {
                        count += board.sheetplacements.Count;
                        var boxes = new List<PolygonBounds>();
                        foreach (var position in board.sheetplacements)
                        {
                            var shape = Background.rotatePolygon(parts[position.id], position.rotation);
                            var bounds = GeometryUtil.getPolygonBounds(shape);
                            bounds.x += position.x;
                            bounds.y += position.y;
                            if (bounds.x < -1e-7 || bounds.y < -1e-7 || bounds.x + bounds.width > width + 1e-7 ||
                                bounds.y + bounds.height > height + 1e-7)
                            { Fail("Exact-fit part outside sheet."); return false; }
                            foreach (var other in boxes)
                                if (Math.Min(bounds.x + bounds.width, other.x + other.width) - Math.Max(bounds.x, other.x) > 1e-7 &&
                                    Math.Min(bounds.y + bounds.height, other.y + other.height) - Math.Max(bounds.y, other.y) > 1e-7)
                                { Fail("Exact-fit parts overlap."); return false; }
                            boxes.Add(bounds);
                        }
                    }
                Console.WriteLine("FIT: board=" + width + "x" + height + ", part=" + partWidth + "x" + partHeight +
                    ", prefix=" + prefix + ", placed=" + count);
                if (count != expected) { Fail("Exact-fit placement count mismatch."); return false; }
            }
            return true;
        }

        private static NFP Rectangle(int id, int source, double width, double height)
        {
            var shape = new NFP { id = id, source = source };
            shape.AddPoint(new SvgPoint(0, 0));
            shape.AddPoint(new SvgPoint(width, 0));
            shape.AddPoint(new SvgPoint(width, height));
            shape.AddPoint(new SvgPoint(0, height));
            return shape;
        }

        private static int Fail(string message)
        {
            Console.Error.WriteLine("FAIL: " + message);
            return 1;
        }
    }
}
