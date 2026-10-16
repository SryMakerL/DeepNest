using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DeepNestLib;

internal static class PlacementSelectionChecks
{
    private static int checks;

    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }

    private static SheetPlacement Result(double? fitness, params int[][] sheetParts)
    {
        var sheets = new List<SheetPlacementItem>();
        for (int i = 0; i < sheetParts.Length; i++)
        {
            var sheet = new SheetPlacementItem { sheetId = i };
            foreach (int id in sheetParts[i]) sheet.sheetplacements.Add(new PlacementItem { id = id, source = 0 });
            sheets.Add(sheet);
        }
        return new SheetPlacement { fitness = fitness, placements = new[] { sheets } };
    }

    private static void Rules()
    {
        var baseline = Result(10, new[] { 0, 1 });
        Check(!SheetPlacement.IsBetterThan(null, baseline), "null candidate rejected");
        Check(SheetPlacement.IsBetterThan(baseline, null), "first result accepted");
        Check(SheetPlacement.IsBetterThan(Result(1000, new[] { 0, 1, 2 }), baseline), "more parts before fitness");
        Check(!SheetPlacement.IsBetterThan(Result(1, new[] { 0 }), baseline), "fewer parts rejected");
        Check(SheetPlacement.IsBetterThan(Result(100, new[] { 0, 1 }), Result(1, new[] { 0 }, new[] { 1 })), "fewer sheets before fitness");
        Check(!SheetPlacement.IsBetterThan(Result(1, new[] { 0 }, new[] { 1 }), baseline), "more sheets rejected");
        Check(SheetPlacement.IsBetterThan(Result(9, new[] { 0, 1 }), baseline), "lower fitness accepted");
        Check(!SheetPlacement.IsBetterThan(Result(10 - 1e-8, new[] { 0, 1 }), baseline), "relative tolerance keeps current");
        Check(SheetPlacement.IsBetterThan(Result(10 - 1e-4, new[] { 0, 1 }), baseline), "outside tolerance accepted");
        Check(!SheetPlacement.IsBetterThan(Result(10, new[] { 0, 1 }), baseline), "tie keeps current");
        Check(SheetPlacement.IsBetterThan(baseline, Result(double.NaN, new[] { 0, 1 })), "finite fitness before NaN");
        Check(!SheetPlacement.IsBetterThan(Result(double.PositiveInfinity, new[] { 0, 1 }), baseline), "infinite fitness rejected on tie");
        Check(!SheetPlacement.IsBetterThan(Result(null, new[] { 0, 1 }), Result(double.NaN, new[] { 0, 1 })), "invalid fitness tie keeps current");
        Check(SheetPlacement.IsBetterThan(Result(double.NaN, new[] { 0, 1, 2 }), baseline), "part count remains first priority");
        Check(!SheetPlacement.IsBetterThan(Result(10, new[] { 0, 0, 1 }), baseline), "duplicate ID does not inflate count");
        Check(!SheetPlacement.IsBetterThan(Result(10, new[] { 0, 1 }, new int[0]), baseline), "empty sheet not counted");
        Check(SheetPlacement.IsBetterThan(Result(0, new[] { 0, 1 }), Result(1e-12, new[] { 0, 1 })), "pure relative tolerance near zero");
        Check(!SheetPlacement.IsBetterThan(Result(-10 + 1e-4, new[] { 0, 1 }), Result(-10, new[] { 0, 1 })), "negative fitness ordered correctly");
    }

    private static void Rectangles()
    {
        Background.UseParallel = false;
        SvgNest.Config = new SvgNestConfig { placementType = PlacementTypeEnum.gravity, populationSize = 5,
            rotations = 4, spacing = 0, sheetSpacing = 0, PlaceParts1BeamWidth = 5, PlaceParts1LookaheadParts = 3 };
        var context = new NestingContext();
        context.AddSheet(100, 100, 0);
        context.AddSheet(100, 100, 0);
        for (int i = 0; i < 4; i++) context.AddRectanglePart(0, 40, 30);
        for (int i = 0; i < 2; i++) context.AddRectanglePart(1, 25, 20);
        context.StartNest();
        var watch = Stopwatch.StartNew();
        SheetPlacement previous = null;
        for (int iteration = 0; iteration < 3; iteration++)
        {
            context.NestIterate();
            Check(!SheetPlacement.IsBetterThan(previous, context.Current), "best result does not regress " + iteration);
            previous = context.Current;
        }
        watch.Stop();
        Check(context.Current.fitness.HasValue && !double.IsNaN(context.Current.fitness.Value)
            && !double.IsInfinity(context.Current.fitness.Value), "sample finite fitness");
        var placed = context.Current.placements.SelectMany(g => g).SelectMany(s => s.sheetplacements).ToArray();
        Check(placed.Select(p => p.id).Distinct().Count() == placed.Length, "sample unique part instances");
        Check(placed.Length == context.Polygons.Count, "sample all six parts placed");
        var boxes = new List<Tuple<int, double, double, double, double>>();
        foreach (var group in context.Current.placements)
        foreach (var sheet in group)
        {
            Check(context.Sheets.Any(s => s.id == sheet.sheetId), "sample valid sheet ID");
            foreach (var placement in sheet.sheetplacements)
            {
                Check(!double.IsNaN(placement.x) && !double.IsInfinity(placement.x)
                    && !double.IsNaN(placement.y) && !double.IsInfinity(placement.y)
                    && !float.IsNaN(placement.rotation) && !float.IsInfinity(placement.rotation), "sample finite transform");
                Check(Math.Abs(placement.rotation / 90 - Math.Round(placement.rotation / 90)) < 1e-6, "sample legal rectangle rotation");
                var polygon = context.Polygons.Single(p => p.id == placement.id);
                double angle = placement.rotation * Math.PI / 180;
                var xs = polygon.Points.Select(p => p.x * Math.Cos(angle) - p.y * Math.Sin(angle) + placement.x).ToArray();
                var ys = polygon.Points.Select(p => p.x * Math.Sin(angle) + p.y * Math.Cos(angle) + placement.y).ToArray();
                double left = xs.Min(), right = xs.Max(), bottom = ys.Min(), top = ys.Max();
                Check(left >= -1e-5 && bottom >= -1e-5 && right <= 100 + 1e-5 && top <= 100 + 1e-5, "sample within sheet");
                foreach (var box in boxes.Where(b => b.Item1 == sheet.sheetId))
                    Check(Math.Min(right, box.Item4) - Math.Max(left, box.Item2) <= 1e-5
                        || Math.Min(top, box.Item5) - Math.Max(bottom, box.Item3) <= 1e-5, "sample no positive-area overlap");
                boxes.Add(Tuple.Create(sheet.sheetId, left, bottom, right, top));
            }
        }
        Console.WriteLine("Sample: placed=" + placed.Length + " fitness=" + context.Current.fitness
            + " elapsedMs=" + watch.ElapsedMilliseconds);
    }

    public static void Main()
    {
        Rules();
        Rectangles();
        Console.WriteLine("CHECKS COMPLETE: " + checks);
    }
}
