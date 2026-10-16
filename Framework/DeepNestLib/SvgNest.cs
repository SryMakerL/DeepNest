using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeepNestLib
{
    public class SvgNest
    {

        public SvgNest()
        {

        }
        public class InrangeItem
        {
            public SvgPoint point;
            public double distance;
        }

        #region experimental features
        public static SvgPoint RotatePoint(SvgPoint p, double cx, double cy, double angle)
        {
            return new SvgPoint(Math.Cos(angle) * (p.x - cx) - Math.Sin(angle) * (p.y - cy) + cx,
                         Math.Sin(angle) * (p.x - cx) + Math.Cos(angle) * (p.y - cy) + cy);
        }

        public static NFP GetMinimumBox(NFP vv)
        {
            var hull = Background.getHull(new NFP() { Points = vv.Points.Select(z => new SvgPoint(z.x, z.y)).ToArray() });
            double minArea = double.MaxValue;
            List<SvgPoint> rect = new List<SvgPoint>();
            for (int i = 0; i < hull.Length; i++)
            {
                var p0 = hull.Points[i];
                var p1 = hull.Points[(i + 1) % hull.Length];
                var dx = p1.x - p0.x;
                var dy = p1.y - p0.y;
                var atan = Math.Atan2(dy, dx);

                List<SvgPoint> dd = new List<SvgPoint>();
                for (int j = 0; j < vv.Length; j++)
                {
                    var r = RotatePoint(new SvgPoint(vv[j].x, vv[j].y), 0, 0, -atan);
                    dd.Add(r);
                }
                var maxx = dd.Max(z => z.x);
                var maxy = dd.Max(z => z.y);
                var minx = dd.Min(z => z.x);
                var miny = dd.Min(z => z.y);

                var area = (maxx - minx) * (maxy - miny);

                if (area < minArea)
                {
                    minArea = area;
                    rect.Clear();

                    rect.Add(new SvgPoint(minx, miny));
                    rect.Add(new SvgPoint(maxx, miny));
                    rect.Add(new SvgPoint(maxx, maxy));
                    rect.Add(new SvgPoint(minx, maxy));
                    for (int j = 0; j < rect.Count; j++)
                    {
                        rect[j] = RotatePoint(new SvgPoint(rect[j].x, rect[j].y), 0, 0, atan);
                    }
                }
            }

            NFP ret = new NFP();
            ret.Points = rect.ToArray();
            return ret;
        }
        static NFP boundingBox(NFP offset)
        {
            NFP ret = new NFP();
            var maxx = offset.Points.Max(z => z.x);
            var maxy = offset.Points.Max(z => z.y);
            var minx = offset.Points.Min(z => z.x);
            var miny = offset.Points.Min(z => z.y);
            ret.AddPoint(new SvgPoint(minx, miny));
            ret.AddPoint(new SvgPoint(maxx, miny));
            ret.AddPoint(new SvgPoint(maxx, maxy));
            ret.AddPoint(new SvgPoint(minx, maxy));
            return ret;
        }
        /// <summary>
        /// Clip the subject so it stays inside the clipBounds.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clipBounds"></param>
        /// <param name="clipperScale"></param>
        /// <returns></returns>
        internal static NFP ClipSubject(NFP subject, NFP clipBounds, double clipperScale)
        {
            var clipperSubject = Background.innerNfpToClipperCoordinates(new NFP[] { subject }, SvgNest.Config);
            var clipperClip = Background.innerNfpToClipperCoordinates(new NFP[] { clipBounds }, SvgNest.Config);

            var clipper = new Clipper();
            clipper.AddPaths(clipperClip.Select(z => z.ToList()).ToList(), PolyType.ptClip, true);
            clipper.AddPaths(clipperSubject.Select(z => z.ToList()).ToList(), PolyType.ptSubject, true);

            List<List<IntPoint>> finalNfp = new List<List<IntPoint>>();
            if (clipper.Execute(ClipType.ctIntersection, finalNfp, PolyFillType.pftNonZero, PolyFillType.pftNonZero) && finalNfp != null && finalNfp.Count > 0)
            {
                return Background.toNestCoordinates(finalNfp[0].ToArray(), clipperScale);
            }

            return subject;
        }
        #endregion

        public static SvgPoint getTarget(SvgPoint o, NFP simple, double tol)
        {
            List<InrangeItem> inrange = new List<InrangeItem>();
            // find closest points within 2 offset deltas
            for (var j = 0; j < simple.length; j++)
            {
                var s = simple[j];
                var d2 = (o.x - s.x) * (o.x - s.x) + (o.y - s.y) * (o.y - s.y);
                if (d2 < tol * tol)
                {
                    inrange.Add(new InrangeItem() { point = s, distance = d2 });
                }
            }

            SvgPoint target = null;
            if (inrange.Count > 0)
            {
                var filtered = inrange.Where((p) =>
                {
                    return p.point.exact;
                }).ToList();

                // use exact points when available, normal points when not
                inrange = filtered.Count > 0 ? filtered : inrange;


                inrange = inrange.OrderBy((b) =>
  {
      return b.distance;
  }).ToList();

                target = inrange[0].point;
            }
            else
            {
                double? mind = null;
                for (int j = 0; j < simple.length; j++)
                {
                    var s = simple[j];
                    var d2 = (o.x - s.x) * (o.x - s.x) + (o.y - s.y) * (o.y - s.y);
                    if (mind == null || d2 < mind)
                    {
                        target = s;
                        mind = d2;
                    }
                }
            }

            return target;
        }

        public static SvgNestConfig Config = new SvgNestConfig();


        public static NFP clone(NFP p)
        {
            var newp = new NFP();
            for (var i = 0; i < p.length; i++)
            {
                newp.AddPoint(new SvgPoint(

                     p[i].x,
                     p[i].y

                ));
            }

            return newp;
        }


        public static bool pointInPolygon(SvgPoint point, NFP polygon)
        {
            // scaling is deliberately coarse to filter out points that lie *on* the polygon

            var p = svgToClipper2(polygon, 1000);
            var pt = new ClipperLib.IntPoint(1000 * point.x, 1000 * point.y);

            return ClipperLib.Clipper.PointInPolygon(pt, p.ToList()) > 0;
        }

        // returns true if any complex vertices fall outside the simple polygon
        public static bool exterior(NFP simple, NFP complex, bool inside)
        {
            // find all protruding vertices
            for (var i = 0; i < complex.length; i++)
            {
                var v = complex[i];
                if (!inside && !pointInPolygon(v, simple) && find(v, simple) == null)
                {
                    return true;
                }
                if (inside && pointInPolygon(v, simple) && find(v, simple) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public static NFP simplifyFunction(NFP polygon, bool inside)
        {
            return simplifyFunction(polygon, inside, SvgNest.Config);
        }
        public static NFP simplifyFunction(NFP polygon, bool inside, SvgNestConfig config)
        {
            var tolerance = 4 * config.curveTolerance;

            // give special treatment to line segments above this length (squared)
            var fixedTolerance = 40 * config.curveTolerance * 40 * config.curveTolerance;
            int i, j, k;

            var hull = Background.getHull(polygon);
            if (config.simplify)
            {
                /*
				// use convex hull
				var hull = new ConvexHullGrahamScan();
				for(var i=0; i<polygon.length; i++){
					hull.addPoint(polygon[i].x, polygon[i].y);
				}
			
				return hull.getHull();*/
                if (hull != null)
                {
                    return hull;
                }
                else
                {
                    return polygon;
                }
            }

            var cleaned = cleanPolygon2(polygon);
            if (cleaned != null && cleaned.length > 1)
            {
                polygon = cleaned;
            }
            else
            {
                return polygon;
            }
            // polygon to polyline
            var copy = polygon.slice(0);
            copy.push(copy[0]);
            // mark all segments greater than ~0.25 in to be kept
            // the PD simplification algo doesn't care about the accuracy of long lines, only the absolute distance of each point
            // we care a great deal
            for (i = 0; i < copy.length - 1; i++)
            {
                var p1 = copy[i];
                var p2 = copy[i + 1];
                var sqd = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);
                if (sqd > fixedTolerance)
                {
                    p1.marked = true;
                    p2.marked = true;
                }
            }

            var simple = Simplify.simplify(copy, tolerance, true);
            // now a polygon again
            //simple.pop();
            simple.Points = simple.Points.Take(simple.Points.Count() - 1).ToArray();

            // could be dirty again (self intersections and/or coincident points)
            simple = cleanPolygon2(simple);

            // simplification process reduced poly to a line or point
            if (simple == null)
            {
                simple = polygon;
            }

            var offsets = polygonOffsetDeepNest(simple, inside ? -tolerance : tolerance);

            NFP offset = null;
            double offsetArea = 0;
            List<NFP> holes = new List<NFP>();
            for (i = 0; i < offsets.Length; i++)
            {
                var area = GeometryUtil.polygonArea(offsets[i]);
                if (offset == null || area < offsetArea)
                {
                    offset = offsets[i];
                    offsetArea = area;
                }
                if (area > 0)
                {
                    holes.Add(offsets[i]);
                }
            }

            // mark any points that are exact
            for (i = 0; i < simple.length; i++)
            {
                var seg = new NFP();
                seg.AddPoint(simple[i]);
                seg.AddPoint(simple[i + 1 == simple.length ? 0 : i + 1]);

                var index1 = find(seg[0], polygon);
                var index2 = find(seg[1], polygon);

                if (index1 + 1 == index2 || index2 + 1 == index1 || (index1 == 0 && index2 == polygon.length - 1) || (index2 == 0 && index1 == polygon.length - 1))
                {
                    seg[0].exact = true;
                    seg[1].exact = true;
                }
            }
            var numshells = 4;
            NFP[] shells = new NFP[numshells];

            for (j = 1; j < numshells; j++)
            {
                var delta = j * (tolerance / numshells);
                delta = inside ? -delta : delta;
                var shell = polygonOffsetDeepNest(simple, delta);
                if (shell.Count() > 0)
                {
                    shells[j] = shell.First();
                }
                else
                {
                    //shells[j] = shell;
                }
            }

            if (offset == null)
            {
                return polygon;
            }
            // selective reversal of offset
            for (i = 0; i < offset.length; i++)
            {
                var o = offset[i];
                var target = getTarget(o, simple, 2 * tolerance);

                // reverse point offset and try to find exterior points
                var test = clone(offset);
                test.Points[i] = new SvgPoint(target.x, target.y);

                if (!exterior(test, polygon, inside))
                {
                    o.x = target.x;
                    o.y = target.y;
                }
                else
                {
                    // a shell is an intermediate offset between simple and offset
                    for (j = 1; j < numshells; j++)
                    {
                        if (shells[j] != null)
                        {
                            var shell = shells[j];
                            var delta = j * (tolerance / numshells);
                            target = getTarget(o, shell, 2 * delta);
                            test = clone(offset);
                            test.Points[i] = new SvgPoint(target.x, target.y);
                            if (!exterior(test, polygon, inside))
                            {
                                o.x = target.x;
                                o.y = target.y;
                                break;
                            }
                        }
                    }
                }
            }

            // straighten long lines
            // a rounded rectangle would still have issues at this point, as the long sides won't line up straight

            var straightened = false;

            for (i = 0; i < offset.length; i++)
            {
                var p1 = offset[i];
                var p2 = offset[i + 1 == offset.length ? 0 : i + 1];

                var sqd = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);

                if (sqd < fixedTolerance)
                {
                    continue;
                }
                for (j = 0; j < simple.length; j++)
                {
                    var s1 = simple[j];
                    var s2 = simple[j + 1 == simple.length ? 0 : j + 1];

                    var sqds = (p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y);

                    if (sqds < fixedTolerance)
                    {
                        continue;
                    }

                    if ((GeometryUtil._almostEqual(s1.x, s2.x) || GeometryUtil._almostEqual(s1.y, s2.y)) && // we only really care about vertical and horizontal lines
                    GeometryUtil._withinDistance(p1, s1, 2 * tolerance) &&
                    GeometryUtil._withinDistance(p2, s2, 2 * tolerance) &&
                    (!GeometryUtil._withinDistance(p1, s1, config.curveTolerance / 1000) ||
                    !GeometryUtil._withinDistance(p2, s2, config.curveTolerance / 1000)))
                    {
                        p1.x = s1.x;
                        p1.y = s1.y;
                        p2.x = s2.x;
                        p2.y = s2.y;
                        straightened = true;
                    }
                }
            }

            //if (straightened)
            {

                var Ac = _Clipper.ScaleUpPaths(offset, 10000000);
                var Bc = _Clipper.ScaleUpPaths(polygon, 10000000);

                var combined = new List<List<IntPoint>>();
                var clipper = new ClipperLib.Clipper();

                clipper.AddPath(Ac.ToList(), ClipperLib.PolyType.ptSubject, true);
                clipper.AddPath(Bc.ToList(), ClipperLib.PolyType.ptSubject, true);

                // the line straightening may have made the offset smaller than the simplified
                if (clipper.Execute(ClipperLib.ClipType.ctUnion, combined, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
                {
                    double? largestArea = null;
                    for (i = 0; i < combined.Count; i++)
                    {
                        var n = Background.toNestCoordinates(combined[i].ToArray(), 10000000);
                        var sarea = -GeometryUtil.polygonArea(n);
                        if (largestArea == null || largestArea < sarea)
                        {
                            offset = n;
                            largestArea = sarea;
                        }
                    }
                }
            }

            cleaned = cleanPolygon2(offset);
            if (cleaned != null && cleaned.length > 1)
            {
                offset = cleaned;
            }

            #region experimental
            if (config.clipByHull)
            {
                offset = ClipSubject(offset, hull, config.clipperScale);
            }
            else if (config.clipByRects)
            {
                NFP rect1 = boundingBox(hull);
                offset = ClipSubject(offset, rect1, config.clipperScale);
                var mbox = GetMinimumBox(hull);
                offset = ClipSubject(offset, mbox, config.clipperScale);
            }
            #endregion 

            // mark any points that are exact (for line merge detection)
            for (i = 0; i < offset.length; i++)
            {
                var seg = new SvgPoint[] { offset[i], offset[i + 1 == offset.length ? 0 : i + 1] };
                var index1 = find(seg[0], polygon);
                var index2 = find(seg[1], polygon);
                if (index1 == null)
                {
                    index1 = 0;
                }
                if (index2 == null)
                {
                    index2 = 0;
                }
                if (index1 + 1 == index2 || index2 + 1 == index1
                    || (index1 == 0 && index2 == polygon.length - 1) ||
                    (index2 == 0 && index1 == polygon.length - 1))
                {
                    seg[0].exact = true;
                    seg[1].exact = true;
                }
            }

            if (!inside && holes != null && holes.Count > 0)
            {
                offset.children = holes;
            }

            return offset;

        }
        public static int? find(SvgPoint v, NFP p)
        {
            for (var i = 0; i < p.length; i++)
            {
                if (GeometryUtil._withinDistance(v, p[i], Config.curveTolerance / 1000))
                {
                    return i;
                }
            }
            return null;
        }
        // offset tree recursively
        public static void offsetTree(NFP t, double offset, SvgNestConfig config, bool? inside = null)
        {
            var simple = simplifyFunction(t, (inside == null) ? false : inside.Value);
            var offsetpaths = new NFP[] { simple };
            if (Math.Abs(offset) > 0)
            {
                offsetpaths = polygonOffsetDeepNest(simple, offset);
            }

            if (offsetpaths.Count() > 0)
            {

                List<SvgPoint> rett = new List<SvgPoint>();
                rett.AddRange(offsetpaths[0].Points);
                rett.AddRange(t.Points.Skip(t.length));
                t.Points = rett.ToArray();

                // replace array items in place

                //Array.prototype.splice.apply(t, [0, t.length].concat(offsetpaths[0]));
            }

            if (simple.children != null && simple.children.Count > 0)
            {
                if (t.children == null)
                {
                    t.children = new List<NFP>();
                }

                for (var i = 0; i < simple.children.Count; i++)
                {
                    t.children.Add(simple.children[i]);
                }
            }

            if (t.children != null && t.children.Count > 0)
            {
                for (var i = 0; i < t.children.Count; i++)
                {

                    offsetTree(t.children[i], -offset, config, (inside == null) ? true : (!inside));
                }
            }
        }


        // use the clipper library to return an offset to the given polygon. Positive offset expands the polygon, negative contracts
        // note that this returns an array of polygons
        public static NFP[] polygonOffsetDeepNest(NFP polygon, double offset)
        {

            if (offset == 0 || GeometryUtil._almostEqual(offset, 0))
            {
                return new[] { polygon };
            }

            var p = svgToClipper(polygon).ToList();

            var miterLimit = 4;
            var co = new ClipperLib.ClipperOffset(miterLimit, Config.curveTolerance * Config.clipperScale);
            co.AddPath(p.ToList(), ClipperLib.JoinType.jtMiter, ClipperLib.EndType.etClosedPolygon);

            var newpaths = new List<List<ClipperLib.IntPoint>>();
            co.Execute(ref newpaths, offset * Config.clipperScale);


            var result = new List<NFP>();
            for (var i = 0; i < newpaths.Count; i++)
            {
                result.Add(clipperToSvg(newpaths[i]));
            }


            return result.ToArray();
        }



        // converts a polygon from normal float coordinates to integer coordinates used by clipper, as well as x/y -> X/Y
        public static IntPoint[] svgToClipper2(NFP polygon, double? scale = null)
        {


            var d = _Clipper.ScaleUpPaths(polygon, scale == null ? Config.clipperScale : scale.Value);
            return d.ToArray();

        }

        // converts a polygon from normal float coordinates to integer coordinates used by clipper, as well as x/y -> X/Y
        public static ClipperLib.IntPoint[] svgToClipper(NFP polygon)
        {



            var d = _Clipper.ScaleUpPaths(polygon, Config.clipperScale);
            return d.ToArray();

            return polygon.Points.Select(z => new IntPoint((long)z.x, (long)z.y)).ToArray();
        }
        // returns a less complex polygon that satisfies the curve tolerance
        public static NFP cleanPolygon(NFP polygon)
        {
            var p = svgToClipper2(polygon);
            // remove self-intersections and find the biggest polygon that's left
            var simple = ClipperLib.Clipper.SimplifyPolygon(p.ToList(), ClipperLib.PolyFillType.pftNonZero);

            if (simple == null || simple.Count == 0)
            {
                return null;
            }

            var biggest = simple[0];
            var biggestarea = Math.Abs(ClipperLib.Clipper.Area(biggest));
            for (var i = 1; i < simple.Count; i++)
            {
                var area = Math.Abs(ClipperLib.Clipper.Area(simple[i]));
                if (area > biggestarea)
                {
                    biggest = simple[i];
                    biggestarea = area;
                }
            }

            // clean up singularities, coincident points and edges
            var clean = ClipperLib.Clipper.CleanPolygon(biggest, 0.01 *
                Config.curveTolerance * Config.clipperScale);

            if (clean == null || clean.Count == 0)
            {
                return null;
            }
            return clipperToSvg(clean);

        }

        public static NFP cleanPolygon2(NFP polygon)
        {
            var p = svgToClipper(polygon);
            // remove self-intersections and find the biggest polygon that's left
            var simple = ClipperLib.Clipper.SimplifyPolygon(p.ToList(), ClipperLib.PolyFillType.pftNonZero);

            if (simple == null || simple.Count == 0)
            {
                return null;
            }

            var biggest = simple[0];
            var biggestarea = Math.Abs(ClipperLib.Clipper.Area(biggest));
            for (var i = 1; i < simple.Count; i++)
            {
                var area = Math.Abs(ClipperLib.Clipper.Area(simple[i]));
                if (area > biggestarea)
                {
                    biggest = simple[i];
                    biggestarea = area;
                }
            }

            // clean up singularities, coincident points and edges
            var clean = ClipperLib.Clipper.CleanPolygon(biggest, 0.01 *
                Config.curveTolerance * Config.clipperScale);

            if (clean == null || clean.Count == 0)
            {
                return null;
            }
            var cleaned = clipperToSvg(clean);

            // remove duplicate endpoints
            var start = cleaned[0];
            var end = cleaned[cleaned.length - 1];
            if (start == end || (GeometryUtil._almostEqual(start.x, end.x)
                && GeometryUtil._almostEqual(start.y, end.y)))
            {
                cleaned.Points = cleaned.Points.Take(cleaned.Points.Count() - 1).ToArray();
            }

            return cleaned;

        }

        public static NFP clipperToSvg(IList<IntPoint> polygon)
        {
            List<SvgPoint> ret = new List<SvgPoint>();

            for (var i = 0; i < polygon.Count; i++)
            {
                ret.Add(new SvgPoint(polygon[i].X / Config.clipperScale, polygon[i].Y / Config.clipperScale));
            }

            return new NFP() { Points = ret.ToArray() };
        }


        public int toTree(PolygonTreeItem[] list, int idstart = 0)
        {
            List<PolygonTreeItem> parents = new List<PolygonTreeItem>();
            int i, j;

            // assign a unique id to each leaf
            //var id = idstart || 0;
            var id = idstart;

            for (i = 0; i < list.Length; i++)
            {
                var p = list[i];

                var ischild = false;
                for (j = 0; j < list.Length; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }
                    if (GeometryUtil.pointInPolygon(p.Polygon.Points[0], list[j].Polygon).Value)
                    {
                        if (list[j].Childs == null)
                        {
                            list[j].Childs = new List<PolygonTreeItem>();
                        }
                        list[j].Childs.Add(p);
                        p.Parent = list[j];
                        ischild = true;
                        break;
                    }
                }

                if (!ischild)
                {
                    parents.Add(p);
                }
            }

            for (i = 0; i < list.Length; i++)
            {
                if (parents.IndexOf(list[i]) < 0)
                {
                    list = list.Skip(i).Take(1).ToArray();
                    i--;
                }
            }

            for (i = 0; i < parents.Count; i++)
            {
                parents[i].Polygon.Id = id;
                id++;
            }

            for (i = 0; i < parents.Count; i++)
            {
                if (parents[i].Childs != null)
                {
                    id = toTree(parents[i].Childs.ToArray(), id);
                }
            }

            return id;
        }

        public static NFP cloneTree(NFP tree)
        {
            NFP newtree = new NFP();
            newtree.id = tree.id;
            newtree.source = tree.source;
            newtree.rotation = tree.rotation;
            newtree.x = tree.x;
            newtree.y = tree.y;
            newtree.Name = tree.Name;
            foreach (var t in tree.Points)
            {
                newtree.AddPoint(new SvgPoint(t.x, t.y) { exact = t.exact });
            }


            if (tree.children != null && tree.children.Count > 0)
            {
                newtree.children = new List<NFP>();
                foreach (var c in tree.children)
                {
                    newtree.children.Add(cloneTree(c));
                }

            }

            return newtree;
        }


        public Background background = new Background();

        // 多岛并行：每个岛一个独立的 GeneticAlgorithm 与 Background
        private List<GeneticAlgorithm> islands;
        private List<Background> islandBackgrounds;
        private int[] islandGenerations;
        private int[] sourceByPartId;
        private int generationCounter = 0;
        private static readonly object nestsLock = new object();

        PopulationItem individual = null;
        NFP[] placelist;
        GeneticAlgorithm ga;

        public List<SheetPlacement> nests = new List<SheetPlacement>();

        private int ResolvedIslandCount
        {
            get
            {
                if (Config.IslandCount > 0) return Config.IslandCount;
                // 默认取 min(CPU核数, 4)，避免评估量随核数线性膨胀
                return Math.Max(1, Math.Min(Environment.ProcessorCount, 4));
            }
        }

        // 将 (岛id, 个体下标) 编码为全局唯一 index，回调时再解码路由到对应岛
        private static int EncodeIndex(int islandId, int individualIndex)
        {
            return islandId * 1000000 + individualIndex;
        }

        public void ResponseProcessor(SheetPlacement payload)
        {
            //console.log('ipc response', payload);
            if (islands == null)
            {
                // user might have quit while we're away
                return;
            }
            int islandId = payload.index / 1000000;
            int individualIndex = payload.index % 1000000;
            if (islandId < 0 || islandId >= islands.Count) return;
            var island = islands[islandId];
            if (individualIndex < 0 || individualIndex >= island.population.Count) return;

            island.population[individualIndex].processing = null;
            island.population[individualIndex].fitness = payload.fitness;

            // render placement（跨线程，需加锁保护 nests）
            lock (nestsLock)
            {
                if (this.nests.Count == 0 || this.nests[0].fitness > payload.fitness)
                {
                    this.nests.Insert(0, payload);

                    if (this.nests.Count > Config.populationSize)
                    {
                        this.nests.RemoveAt(nests.Count - 1);
                    }
                }
            }
        }

        public void launchWorkers(NestItem[] parts)
        {
            // 构造板材列表（各岛共享同一份，BackgroundStart 内部按需克隆使用）
            List<NFP> sheets = new List<NFP>();
            List<int> sheetids = new List<int>();
            List<int> sheetsources = new List<int>();
            List<List<NFP>> sheetchildren = new List<List<NFP>>();
            var sid = 0;
            for (int i = 0; i < parts.Count(); i++)
            {
                if (parts[i].IsSheet)
                {
                    var poly = parts[i].Polygon;
                    for (int j = 0; j < parts[i].Quanity; j++)
                    {
                        var cln = cloneTree(poly);
                        cln.id = sid; // id is the unique id of all parts that will be nested, including cloned duplicates
                        cln.source = poly.source; // source is the id of each unique part from the main part list

                        sheets.Add(cln);
                        sheetids.Add(sid);
                        sheetsources.Add(i);
                        sheetchildren.Add(poly.children);
                        sid++;
                    }
                }
            }

            if (islands == null)
            {
                List<NFP> adam = new List<NFP>();
                List<int> originalSourcesById = new List<int>();
                var id = 0;
                for (int i = 0; i < parts.Count(); i++)
                {
                    if (!parts[i].IsSheet)
                    {
                        for (int j = 0; j < parts[i].Quanity; j++)
                        {
                            var poly = cloneTree(parts[i].Polygon); // deep copy
                            poly.id = id; // id is the unique id of all parts that will be nested, including cloned duplicates
                            poly.source = i; // source is the id of each unique part from the main part list

                            adam.Add(poly);
                            originalSourcesById.Add(i);
                            id++;
                        }
                    }
                }

                adam = adam.OrderByDescending(z => Math.Abs(GeometryUtil.polygonArea(z))).ToList();
                sourceByPartId = originalSourcesById.ToArray();

                // 初始化多岛：每个岛独立种子、独立 GA、独立 Background
                int islandCount = ResolvedIslandCount;
                islands = new List<GeneticAlgorithm>();
                islandBackgrounds = new List<Background>();
                islandGenerations = new int[islandCount];
                for (int k = 0; k < islandCount; k++)
                {
                    var islandParts = adam.Select(cloneTree).ToArray();
                    var islandGa = new GeneticAlgorithm(islandParts, Config, randomSeed: k * 7919 + 1);
                    islands.Add(islandGa);
                    var bg = new Background();
                    bg.ResponseAction = ResponseProcessor;
                    islandBackgrounds.Add(bg);
                }
                ga = islands[0]; // 兼容旧引用
            }
            individual = null;

            // 每个岛独立检测自己的代边界：该岛当代全部评估完则推进到下一代。
            // 用 islandGeneration 记录各岛代数，达迁移间隔时在岛间交换全局最优个体。
            for (int k = 0; k < islands.Count; k++)
            {
                var g = islands[k];
                bool islandFinished = g.population.All(p => p.fitness != null);
                if (islandFinished)
                {
                    islandGenerations[k]++;
                    generationCounter = islandGenerations.Max();
                    if (Config.MigrationInterval > 0 && islandGenerations[k] % Config.MigrationInterval == 0)
                    {
                        // 用当前各岛的全局最优替换该岛最差个体
                        var globalBest = islands.Select(x => x.GetBest())
                            .Where(b => b != null && b.fitness.HasValue)
                            .OrderBy(b => b.fitness.GetValueOrDefault()).FirstOrDefault();
                        if (globalBest != null)
                        {
                            g.ReplaceWorst(globalBest);
                        }
                    }
                    g.generation();
                }
            }

            // 渐进式批处理：每次调用每个岛只评估 batchSize 个待评估个体（保持每次迭代开销平稳），
            // 岛与岛之间并行；BackgroundStart 内部为同步执行（NFP 并行计算 + sync 同步放置 + 回调）。
            int batchSize = Math.Max(1, Config.populationSize / 10);
            var outerParallel = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) };
            Parallel.For(0, islands.Count, outerParallel, k =>
            {
                var g = islands[k];
                var bg = islandBackgrounds[k];
                int dispatched = 0;
                for (int i = 0; i < g.population.Count && dispatched < batchSize; i++)
                {
                    if (g.population[i].processing == null && g.population[i].fitness == null)
                    {
                        g.population[i].processing = true;
                        dispatched++;

                        List<int> ids = new List<int>();
                        List<int> sources = new List<int>();
                        List<List<NFP>> children = new List<List<NFP>>();

                        for (int j = 0; j < g.population[i].placements.Count; j++)
                        {
                            var pid = g.population[i].placements[j].id;
                            var child = g.population[i].placements[j].children;
                            ids.Add(pid);
                            sources.Add(sourceByPartId[pid]);
                            children.Add(child);
                        }

                        DataInfo data = new DataInfo()
                        {
                            index = EncodeIndex(k, i),
                            sheets = sheets.Select(cloneTree).ToList(),
                            sheetids = sheetids.ToArray(),
                            sheetsources = sheetsources.ToArray(),
                            sheetchildren = sheetchildren.Select(c => c == null ? null : c.Select(cloneTree).ToList()).ToList(),
                            individual = g.population[i],
                            config = Config,
                            ids = ids.ToArray(),
                            sources = sources.ToArray(),
                            children = children
                        };

                        bg.BackgroundStart(data);
                    }
                }
            });
        }

        public PolygonTreeItem[] tree;        


        public bool useHoles;
        public bool searchEdges;
    }

    public class DataInfo
    {

        public int index;
        public List<NFP> sheets;
        public int[] sheetids;
        public int[] sheetsources;
        public List<List<NFP>> sheetchildren;
        public PopulationItem individual;
        public SvgNestConfig config;
        public int[] ids;
        public int[] sources;
        public List<List<NFP>> children;
        //ipcRenderer.send('background-start', { index: i, sheets: sheets, sheetids: sheetids, sheetsources: sheetsources, sheetchildren: sheetchildren, 
        //individual: GA.population[i], config: config, ids: ids, sources: sources, children: children});
    }

    public class PolygonTreeItem
    {
        public NFP Polygon;
        public PolygonTreeItem Parent;
        public List<PolygonTreeItem> Childs = new List<PolygonTreeItem>();
    }

    public enum PlacementTypeEnum
    {
        box, gravity, squeeze
    }

    public class DbCacheKey
    {
        public int? A;
        public int? B;
        public float ARotation;
        public float BRotation;
        public NFP[] nfp;
        public int Type;
    }

    public class NfpPair
    {
        public NFP A;
        public NFP B;
        public NfpKey Key;
        public NFP nfp;

        public float ARotation;
        public float BRotation;

        public int Asource { get; internal set; }
        public int Bsource { get; internal set; }
    }

    public class NonameReturn
    {
        public NfpKey key;
        public NFP[] nfp;
        public NFP[] value
        {
            get
            {
                return nfp;
            }
        }
        public NonameReturn(NfpKey key, NFP[] nfp)
        {
            this.key = key;
            this.nfp = nfp;
        }
    }

    public interface IStringify
    {
        string stringify();
    }

    public class NfpKey : IStringify
    {

        public NFP A;
        public NFP B;
        public float ARotation { get; set; }
        public float BRotation { get; set; }
        public bool Inside { get; set; }

        public int AIndex { get; set; }
        public int BIndex { get; set; }
        public object Asource;
        public object Bsource;


        public string stringify()
        {
            return $"A:{AIndex} B:{BIndex} inside:{Inside} Arotation:{ARotation} Brotation:{BRotation}";
        }
    }

    public class PopulationItem
    {
        public object processing = null;

        public double? fitness;

        public float[] Rotation;
        public List<NFP> placements;

        public NFP[] paths;
        public double area;

        // 新增：模式组列表
        public List<PatternGroup> PatternGroups { get; set; } = new List<PatternGroup>();

        public PopulationItem Clone()
        {
            return new PopulationItem
            {
                placements = this.placements.Select(n => n.Clone()).ToList(),
                Rotation = this.Rotation?.ToArray(),
                fitness = this.fitness,
                processing = this.processing,

                
            };
        }
    }
    public class PatternGroup
    {
        public int GroupId { get; set; }
        public int PatternType { get; set; } // 0=none, 1=single lattice, 2=double lattice alternating
        public List<int> MemberIndices { get; set; }
        public float BaseAngle { get; set; }   // 主角度
        public float SecondAngle { get; set; } // 次角度（用于交替）
        public SvgPoint LatticeU { get; set; } // 晶格向量U（可选，不填则用默认包围盒间距）
        public SvgPoint LatticeV { get; set; } // 晶格向量V
    }

    public class SheetPlacementItem
    {
        public int sheetId;
        public int sheetSource;

        public List<PlacementItem> sheetplacements = new List<PlacementItem>();
        public List<PlacementItem> placements = new List<PlacementItem>();
    }

    public class PlacementItem
    {
        public double? mergedLength;
        public object mergedSegments;
        public List<List<ClipperLib.IntPoint>> nfp;
        public int id;
        public NFP hull;
        public NFP hullsheet;

        public float rotation;
        public double x;
        public double y;
        public int source;
    }

    public class SheetPlacement
    {
        public double? fitness;

        public float[] Rotation;
        public List<SheetPlacementItem>[] placements;

        public NFP[] paths;
        public double area;
        public double mergedLength;
        internal int index;
    }



    public class Sheet : NFP
    {
        public double Width;
        public double Height;
    }

    public class RectangleSheet : Sheet
    {

        public void Rebuild()
        {
            Points = new SvgPoint[] { };
            AddPoint(new SvgPoint(x, y));
            AddPoint(new SvgPoint(x + Width, y));
            AddPoint(new SvgPoint(x + Width, y + Height));
            AddPoint(new SvgPoint(x, y + Height));
        }
    }
    public class NestItem
    {
        public NFP Polygon;
        public int Quanity;
        public bool IsSheet;
    }
}

