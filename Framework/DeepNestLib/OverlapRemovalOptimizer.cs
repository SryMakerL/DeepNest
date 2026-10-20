using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ClipperLib;

namespace DeepNestLib
{
    public static class OverlapRemovalOptimizer
    {
        private static int queryIdentity;
        private const int CandidateLimit = 160;
        private const int SegmentLimit = 72;
        private const int IterationLimit = 18;

        public static SheetPlacement Optimize(SheetPlacement input, NFP[] sheets, NFP[] parts,
            Func<SheetPlacement, double> evaluator, int timeBudgetMilliseconds = 100,
            double maxTranslation = 1.0, double minimumGap = 0)
        {
            return Optimize(input, sheets, parts, new SvgNestConfig(), evaluator, timeBudgetMilliseconds);
        }

        /// <summary>
        /// Coordinates in parts are unrotated; placement.rotation is absolute. Geometry already includes spacing.
        /// Lower evaluator scores are better. The synchronous evaluator must be bounded and side-effect free.
        /// </summary>
        public static SheetPlacement Optimize(SheetPlacement input, NFP[] sheets, NFP[] parts,
            SvgNestConfig config, Func<SheetPlacement, double> evaluator, int timeBudgetMilliseconds = 100)
        {
            if (input == null || input.placements == null || sheets == null || parts == null ||
                config == null || evaluator == null || timeBudgetMilliseconds <= 0) return input;
            var clock = Stopwatch.StartNew();
            bool improved = false;
            SheetPlacement best = input;
            try
            {
                var search = new Search(sheets, parts, config, clock, timeBudgetMilliseconds);
                best = Copy(input);
                if (best == null || !search.Valid(best)) return input;
                double baseline = evaluator(Copy(best));
                if (!Finite(baseline) || search.Expired) return input;
                for (int g = 0; g < best.placements.Length; g++)
                {
                    for (int s = 0; s < best.placements[g].Count; s++)
                    {
                        if (search.Expired) return improved ? best : input;
                        var candidate = Copy(best);
                        if (candidate == null) return improved ? best : input;
                        var sheet = candidate.placements[g][s];
                        if (!search.Compress(sheet) || search.Expired || !search.Valid(candidate)) continue;
                        double fitness = evaluator(Copy(candidate));
                        if (search.Expired) return improved ? best : input;
                        if (!Finite(fitness) || fitness >= baseline) continue;
                        candidate.fitness = fitness;
                        best = candidate;
                        baseline = fitness;
                        improved = true;
                    }
                }
                return improved ? best : input;
            }
            catch (TimeoutException)
            {
                return improved ? best : input;
            }
            catch (Exception)
            {
                return improved ? best : input;
            }
        }

        private static bool Finite(double x)
        {
            return !double.IsNaN(x) && !double.IsInfinity(x);
        }

        private static NFP Clone(NFP shape)
        {
            if (shape == null) return null;
            return new NFP
            {
                id = shape.id, source = shape.source, rotation = shape.rotation, Name = shape.Name,
                x = shape.x, y = shape.y, offsetx = shape.offsetx, offsety = shape.offsety,
                Points = shape.Points.Select(p => new SvgPoint(p.x, p.y)
                    { exact = p.exact, id = p.id, marked = p.marked }).ToArray(),
                children = shape.children == null ? null : shape.children.Select(Clone).ToList()
            };
        }

        private static PlacementItem CopyItem(PlacementItem item)
        {
            // Merged segments are opaque and coordinate-dependent; such inputs are not optimized.
            if (item == null || item.mergedSegments != null ||
                (item.mergedLength.HasValue && item.mergedLength.Value != 0)) return null;
            return new PlacementItem
            {
                id = item.id, source = item.source, rotation = item.rotation, x = item.x, y = item.y,
                mergedLength = item.mergedLength, hull = Clone(item.hull), hullsheet = Clone(item.hullsheet),
                nfp = item.nfp == null ? null : item.nfp.Select(p => new List<IntPoint>(p)).ToList()
            };
        }

        private static SheetPlacement Copy(SheetPlacement input)
        {
            if (input.mergedLength != 0 || (input.paths != null && input.paths.Length != 0)) return null;
            var result = new SheetPlacement
            {
                fitness = input.fitness, area = input.area, mergedLength = input.mergedLength, index = input.index,
                Rotation = input.Rotation == null ? null : (float[])input.Rotation.Clone(),
                paths = input.paths == null ? null : input.paths.Select(Clone).ToArray(),
                placements = new List<SheetPlacementItem>[input.placements.Length]
            };
            for (int g = 0; g < input.placements.Length; g++)
            {
                result.placements[g] = new List<SheetPlacementItem>();
                foreach (var sheet in input.placements[g])
                {
                    if (sheet.placements.Count != 0) return null;
                    var next = new SheetPlacementItem { sheetId = sheet.sheetId, sheetSource = sheet.sheetSource };
                    foreach (var item in sheet.sheetplacements)
                    {
                        var copy = CopyItem(item);
                        if (copy == null) return null;
                        next.sheetplacements.Add(copy);
                    }
                    result.placements[g].Add(next);
                }
            }
            return result;
        }

        private struct Segment
        {
            public SvgPoint A, B;
            public Segment(SvgPoint a, SvgPoint b) { A = a; B = b; }
        }

        private sealed class Search
        {
            private readonly Dictionary<int, NFP> sheets;
            private readonly Dictionary<int, NFP> parts;
            private readonly Dictionary<Tuple<int, float>, NFP> rotated = new Dictionary<Tuple<int, float>, NFP>();
            private readonly Dictionary<Tuple<int, float, int>, NFP[]> inner = new Dictionary<Tuple<int, float, int>, NFP[]>();
            private readonly Dictionary<Tuple<int, float, int, float>, NFP> outer =
                new Dictionary<Tuple<int, float, int, float>, NFP>();
            private readonly Dictionary<NFP, PolygonBounds> boundsCache = new Dictionary<NFP, PolygonBounds>();
            private readonly Dictionary<NFP, List<List<IntPoint>>> boardPaths = new Dictionary<NFP, List<List<IntPoint>>>();
            private readonly SvgNestConfig config;
            private readonly Stopwatch clock;
            private readonly int budget;
            private readonly Random random = new Random(1729);
            private readonly double scale;
            public bool Expired { get { return clock.ElapsedMilliseconds >= budget; } }

            public Search(NFP[] boards, NFP[] originals, SvgNestConfig configuration, Stopwatch watch, int milliseconds)
            {
                sheets = boards.ToDictionary(p => p.id, Clone);
                parts = originals.ToDictionary(p => p.id, Clone);
                config = configuration;
                clock = watch;
                budget = milliseconds;
                scale = config.clipperScale;
                if (!Finite(scale) || scale <= 0 || config.rotations <= 0) throw new ArgumentException();
            }

            private void CheckTime()
            {
                if (Expired) throw new TimeoutException();
            }

            private NFP Shape(PlacementItem item)
            {
                var key = Tuple.Create(item.id, item.rotation);
                NFP shape;
                if (!rotated.TryGetValue(key, out shape))
                {
                    var original = parts[item.id];
                    if (!Finite(item.rotation)) throw new ArgumentException();
                    shape = Background.rotatePolygon(Clone(original), item.rotation);
                    shape.id = original.id;
                    shape.source = original.source;
                    shape.rotation = item.rotation;
                    rotated.Add(key, shape);
                }
                return shape;
            }

            private static void ClearSources(NFP shape)
            {
                shape.source = null;
                if (shape.children != null) foreach (var child in shape.children) ClearSources(child);
            }

            private NFP[] Inner(int sheetId, PlacementItem item)
            {
                var key = Tuple.Create(sheetId, item.rotation, item.id);
                NFP[] value;
                if (inner.TryGetValue(key, out value)) return value;
                CheckTime();
                var board = Clone(sheets[sheetId]);
                var part = Clone(Shape(item));
                ClearSources(board);
                ClearSources(part);
                // A null source prevents insertion; B has a unique identity so even reads cannot alias a global key.
                part.source = Interlocked.Decrement(ref queryIdentity);
                if ((board.children == null || board.children.Count == 0) && IsRectangle(board))
                    value = GeometryUtil.noFitPolygonRectangle(board, part);
                else
                    value = Background.getInnerNfp(board, part, 0, config);
                value = value == null ? null : value.Select(Clone).ToArray();
                inner[key] = value;
                return value;
            }

            private NFP Outer(PlacementItem stationary, PlacementItem moving)
            {
                var key = Tuple.Create(stationary.id, stationary.rotation, moving.id, moving.rotation);
                NFP result;
                if (outer.TryGetValue(key, out result)) return result;
                CheckTime();
                var a = Clone(Shape(stationary));
                var b = Clone(Shape(moving));
                ClearSources(a);
                ClearSources(b);
                b.source = Interlocked.Decrement(ref queryIdentity);
                result = Background.getOuterNfp(a, b, 0);
                result = Clone(result);
                outer.Add(key, result);
                return result;
            }

            private static bool IsRectangle(NFP board)
            {
                int n = board.Points.Length;
                if (n == 5 && board[0].x == board[4].x && board[0].y == board[4].y) n--;
                if (n != 4) return false;
                var bounds = GeometryUtil.getPolygonBounds(board);
                int mask = 0;
                for (int i = 0; i < n; i++)
                {
                    var p = board[i]; var q = board[(i + 1) % n];
                    if ((p.x == q.x) == (p.y == q.y) ||
                        (p.x != bounds.x && p.x != bounds.x + bounds.width) ||
                        (p.y != bounds.y && p.y != bounds.y + bounds.height)) return false;
                    mask |= 1 << ((p.x == bounds.x ? 0 : 1) + (p.y == bounds.y ? 0 : 2));
                }
                return mask == 15;
            }

            private void Contours(NFP shape, double x, double y, List<List<IntPoint>> paths, int depth = 0)
            {
                CheckTime();
                if (shape.Points.Length < 3) throw new ArgumentException("Degenerate contour");
                var path = new List<IntPoint>();
                foreach (var p in shape.Points)
                {
                    double px = (p.x + x) * scale, py = (p.y + y) * scale;
                    if (!Finite(px) || !Finite(py) || Math.Abs(px) > 1e15 || Math.Abs(py) > 1e15)
                        throw new ArgumentException("Invalid coordinate");
                    path.Add(new IntPoint((long)Math.Round(px), (long)Math.Round(py)));
                }
                if (Clipper.Area(path) == 0) throw new ArgumentException("Degenerate contour");
                if (Clipper.Orientation(path) != (depth % 2 == 0)) path.Reverse();
                paths.Add(path);
                if (shape.children != null)
                    foreach (var child in shape.children) Contours(child, x, y, paths, depth + 1);
            }

            private List<List<IntPoint>> Paths(NFP shape, double x = 0, double y = 0)
            {
                var paths = new List<List<IntPoint>>();
                Contours(shape, x, y, paths);
                return paths;
            }

            private bool HasArea(List<List<IntPoint>> a, List<List<IntPoint>> b, ClipType operation)
            {
                CheckTime();
                var clipper = new Clipper();
                clipper.AddPaths(a, PolyType.ptSubject, true);
                clipper.AddPaths(b, PolyType.ptClip, true);
                var solution = new List<List<IntPoint>>();
                if (!clipper.Execute(operation, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero))
                    throw new InvalidOperationException("Polygon boolean failed");
                return solution.Any(p => Clipper.Area(p) != 0);
            }

            private PolygonBounds Bounds(NFP shape)
            {
                PolygonBounds bounds;
                if (!boundsCache.TryGetValue(shape, out bounds))
                {
                    bounds = GeometryUtil.getPolygonBounds(shape);
                    boundsCache.Add(shape, bounds);
                }
                return bounds;
            }

            private bool Inside(int sheetId, PlacementItem item)
            {
                var board = sheets[sheetId];
                var shape = Shape(item);
                var b = Bounds(board);
                var p = Bounds(shape);
                if (p.x + item.x < b.x - 1 / scale || p.y + item.y < b.y - 1 / scale ||
                    p.x + p.width + item.x > b.x + b.width + 1 / scale ||
                    p.y + p.height + item.y > b.y + b.height + 1 / scale) return false;
                if ((board.children == null || board.children.Count == 0) && IsRectangle(board)) return true;
                List<List<IntPoint>> paths;
                if (!boardPaths.TryGetValue(board, out paths))
                {
                    paths = Paths(board);
                    boardPaths.Add(board, paths);
                }
                return !HasArea(Paths(shape, item.x, item.y), paths, ClipType.ctDifference);
            }

            private bool Separated(PlacementItem a, PlacementItem b)
            {
                var ba = Bounds(Shape(a));
                var bb = Bounds(Shape(b));
                return ba.x + ba.width + a.x <= bb.x + b.x || bb.x + bb.width + b.x <= ba.x + a.x ||
                    ba.y + ba.height + a.y <= bb.y + b.y || bb.y + bb.height + b.y <= ba.y + a.y;
            }

            private bool Overlap(PlacementItem a, PlacementItem b)
            {
                if (Separated(a, b)) return false;
                return HasArea(Paths(Shape(a), a.x, a.y), Paths(Shape(b), b.x, b.y), ClipType.ctIntersection);
            }

            public bool Valid(SheetPlacement placement)
            {
                var ids = new HashSet<int>();
                var boardIds = new HashSet<int>();
                foreach (var group in placement.placements)
                    foreach (var sheet in group)
                    {
                        if (!boardIds.Add(sheet.sheetId) || sheets[sheet.sheetId].source != sheet.sheetSource) return false;
                        for (int i = 0; i < sheet.sheetplacements.Count; i++)
                        {
                            var item = sheet.sheetplacements[i];
                            if (!ids.Add(item.id) || parts[item.id].source != item.source ||
                                !Finite(item.x) || !Finite(item.y) || !Inside(sheet.sheetId, item)) return false;
                            for (int j = 0; j < i; j++) if (Overlap(item, sheet.sheetplacements[j])) return false;
                        }
                    }
                return !Expired;
            }

            private static SvgPoint Projection(SvgPoint p, Segment edge)
            {
                double dx = edge.B.x - edge.A.x, dy = edge.B.y - edge.A.y;
                double length = dx * dx + dy * dy;
                double t = length == 0 ? 0 : Math.Max(0, Math.Min(1,
                    ((p.x - edge.A.x) * dx + (p.y - edge.A.y) * dy) / length));
                return new SvgPoint(edge.A.x + t * dx, edge.A.y + t * dy);
            }

            private static void Edges(NFP shape, double x, double y, List<Segment> result)
            {
                if (shape == null) return;
                for (int i = 0; i < shape.Points.Length; i++)
                {
                    var a = shape[i]; var b = shape[(i + 1) % shape.Points.Length];
                    result.Add(new Segment(new SvgPoint(a.x + x, a.y + y), new SvgPoint(b.x + x, b.y + y)));
                }
                if (shape.children != null) foreach (var child in shape.children) Edges(child, x, y, result);
            }

            private double PD(PlacementItem stationary, PlacementItem moving)
            {
                if (!Overlap(stationary, moving)) return 0;
                var nfp = Outer(stationary, moving);
                if (nfp == null) throw new InvalidOperationException("Missing outer NFP");
                var reference = Shape(moving)[0];
                var p = new SvgPoint(reference.x + moving.x, reference.y + moving.y);
                var segments = new List<Segment>();
                Edges(nfp, stationary.x, stationary.y, segments);
                double distance = double.PositiveInfinity;
                foreach (var edge in segments)
                {
                    var q = Projection(p, edge);
                    distance = Math.Min(distance, Math.Sqrt((q.x - p.x) * (q.x - p.x) + (q.y - p.y) * (q.y - p.y)));
                }
                if (!Finite(distance)) throw new InvalidOperationException("Invalid NFP");
                return Math.Max(1 / scale, distance);
            }

            private double Cost(List<PlacementItem> items, double[,] weights)
            {
                double total = 0;
                for (int i = 0; i < items.Count; i++)
                    for (int j = i + 1; j < items.Count; j++) total += weights[i, j] * PD(items[i], items[j]);
                return total;
            }

            private double LocalCost(List<PlacementItem> items, int index, double[,] weights, double limit)
            {
                double total = 0;
                for (int j = 0; j < items.Count; j++)
                {
                    if (j == index) continue;
                    CheckTime();
                    total += weights[Math.Min(index, j), Math.Max(index, j)] *
                        (j < index ? PD(items[j], items[index]) : PD(items[index], items[j]));
                    if (total >= limit) break;
                }
                return total;
            }

            private static SvgPoint Intersection(Segment a, Segment b)
            {
                double ax = a.B.x - a.A.x, ay = a.B.y - a.A.y;
                double bx = b.B.x - b.A.x, by = b.B.y - b.A.y;
                double denominator = ax * by - ay * bx;
                if (Math.Abs(denominator) < 1e-12) return null;
                double dx = b.A.x - a.A.x, dy = b.A.y - a.A.y;
                double t = (dx * by - dy * bx) / denominator;
                double u = (dx * ay - dy * ax) / denominator;
                return t >= 0 && t <= 1 && u >= 0 && u <= 1
                    ? new SvgPoint(a.A.x + t * ax, a.A.y + t * ay) : null;
            }

            private List<SvgPoint> Candidates(int sheetId, List<PlacementItem> items, int index,
                double minX, double maxX)
            {
                var moving = items[index];
                var shape = Shape(moving);
                var bounds = GeometryUtil.getPolygonBounds(shape);
                double left = minX - bounds.x, right = maxX - bounds.x - bounds.width;
                if (right < left) return new List<SvgPoint>();
                var ifp = Inner(sheetId, moving);
                if (ifp == null || ifp.Length == 0) return new List<SvgPoint>();
                var reference = shape[0];
                // IFP coordinates are shape translations; candidate coordinates below use the polygon origin.
                var current = new SvgPoint(moving.x + reference.x, moving.y + reference.y);
                var edges = new List<Segment>();
                foreach (var region in ifp) Edges(region, 0, 0, edges);
                for (int j = 0; j < items.Count; j++)
                    if (j != index) Edges(Outer(items[j], moving), items[j].x, items[j].y, edges);
                var board = GeometryUtil.getPolygonBounds(sheets[sheetId]);
                edges.Add(new Segment(new SvgPoint(left + reference.x, board.y + reference.y - bounds.y),
                    new SvgPoint(left + reference.x, board.y + board.height + reference.y - bounds.y - bounds.height)));
                edges.Add(new Segment(new SvgPoint(right + reference.x, board.y + reference.y - bounds.y),
                    new SvgPoint(right + reference.x, board.y + board.height + reference.y - bounds.y - bounds.height)));
                var candidates = new List<SvgPoint>();
                candidates.Add(new SvgPoint(Math.Max(left, Math.Min(right, moving.x)) + reference.x, current.y));
                // Sample across the entire edge set rather than dropping late neighbours on crowded sheets.
                var selected = new List<Segment>();
                for (int k = 0; k < Math.Min(SegmentLimit, edges.Count); k++)
                    selected.Add(edges[k * edges.Count / Math.Min(SegmentLimit, edges.Count)]);
                foreach (var edge in selected)
                {
                    candidates.Add(edge.A);
                    candidates.Add(Projection(current, edge));
                }
                for (int i = 0; i < selected.Count && candidates.Count < CandidateLimit; i++)
                    for (int j = i + 1; j < selected.Count && candidates.Count < CandidateLimit; j++)
                    {
                        var p = Intersection(selected[i], selected[j]);
                        if (p != null) candidates.Add(p);
                    }
                return candidates.Take(CandidateLimit).Select(p => new SvgPoint(p.x - reference.x, p.y - reference.y))
                    .Where(p => p.x >= left - 1 / scale && p.x <= right + 1 / scale).ToList();
            }

            private double Envelope(List<PlacementItem> items, out double minX)
            {
                minX = double.PositiveInfinity;
                double maxX = double.NegativeInfinity;
                foreach (var item in items)
                {
                    var bounds = GeometryUtil.getPolygonBounds(Shape(item));
                    minX = Math.Min(minX, bounds.x + item.x);
                    maxX = Math.Max(maxX, bounds.x + bounds.width + item.x);
                }
                return maxX - minX;
            }

            public bool Compress(SheetPlacementItem sheet)
            {
                var items = sheet.sheetplacements;
                if (items.Count == 0) return false;
                double minX;
                double originalWidth = Envelope(items, out minX);
                double maxX = minX + originalWidth * 0.96;
                var seedAngles = items.Select(p => parts[p.id].rotation).ToArray();
                for (int i = 0; i < items.Count; i++)
                {
                    var bounds = GeometryUtil.getPolygonBounds(Shape(items[i]));
                    double left = minX - bounds.x, right = maxX - bounds.x - bounds.width;
                    if (right < left) return false;
                    items[i].x = Math.Max(left, Math.Min(right, items[i].x));
                    if (!Inside(sheet.sheetId, items[i])) return false;
                }
                var weights = new double[items.Count, items.Count];
                for (int i = 0; i < items.Count; i++)
                    for (int j = i + 1; j < items.Count; j++) weights[i, j] = 1;
                for (int iteration = 0; iteration < IterationLimit; iteration++)
                {
                    CheckTime();
                    double cost = Cost(items, weights);
                    if (cost == 0)
                    {
                        double actualMin;
                        double width = Envelope(items, out actualMin);
                        if (width >= originalWidth - 1 / scale || actualMin < minX - 1 / scale ||
                            actualMin + width > maxX + 1 / scale) return false;
                        foreach (var item in items) { item.hull = null; item.hullsheet = null; item.nfp = null; }
                        return true;
                    }
                    bool changed = false;
                    var order = Enumerable.Range(0, items.Count).ToArray();
                    for (int i = order.Length - 1; i > 0; i--)
                    {
                        int j = random.Next(i + 1); int swap = order[i]; order[i] = order[j]; order[j] = swap;
                    }
                    foreach (int index in order)
                    {
                        double previous = LocalCost(items, index, weights, double.PositiveInfinity);
                        if (previous == 0) continue;
                        var saved = items[index];
                        PlacementItem winner = saved;
                        double best = previous;
                        int rotations = Math.Min(config.rotations, 16);
                        for (int r = -1; r < rotations; r++)
                        {
                            CheckTime();
                            var trial = CopyItem(saved);
                            trial.rotation = r < 0 ? saved.rotation : seedAngles[index] + r * 360f / config.rotations;
                            items[index] = trial;
                            foreach (var point in Candidates(sheet.sheetId, items, index, minX, maxX))
                            {
                                CheckTime();
                                trial.x = point.x; trial.y = point.y;
                                // Actual difference is the authoritative IFP membership test, including holes and boundaries.
                                if (!Inside(sheet.sheetId, trial)) continue;
                                double next = LocalCost(items, index, weights, best);
                                if (next < best)
                                {
                                    best = next;
                                    winner = CopyItem(trial);
                                }
                            }
                        }
                        items[index] = winner;
                        if (best < previous) { cost = Math.Max(0, cost + best - previous); changed = true; }
                        if (cost == 0) break;
                    }
                    if (!changed)
                    {
                        for (int i = 0; i < items.Count; i++)
                            for (int j = i + 1; j < items.Count; j++)
                                if (PD(items[i], items[j]) > 0) weights[i, j] += 1;
                    }
                }
                return false;
            }
        }
    }
}
