using ClipperLib;
using Minkowski;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeepNestLib
{
    public class Background
    {
        /// <summary>
        /// 启动缓存
        /// </summary>
        public static bool EnableCaches = true;

        public static NFP shiftPolygon(NFP p, PlacementItem shift)
        {
            NFP shifted = new NFP();
            for (var i = 0; i < p.length; i++)
            {
                shifted.AddPoint(new SvgPoint(p[i].x + shift.x, p[i].y + shift.y) { exact = p[i].exact });
            }
            if (p.children != null /*&& p.children.Count*/)
            {
                shifted.children = new List<NFP>();
                for (int i = 0; i < p.children.Count; i++)
                {
                    shifted.children.Add(shiftPolygon(p.children[i], shift));
                }
            }

            return shifted;
        }


        // returns the square of the length of any merged lines
        // filter out any lines less than minlength long
        public static MergedResult mergedLength(NFP[] parts, NFP p, double minlength, double tolerance)
        {
            //            var min2 = minlength * minlength;
            //            var totalLength = 0;
            //            var segments = [];

            //            for (var i = 0; i < p.length; i++)
            //            {
            //                var A1 = p[i];

            //                if (i + 1 == p.length)
            //                {
            //                    A2 = p[0];
            //                }
            //                else
            //                {
            //                    var A2 = p[i + 1];
            //                }

            //                if (!A1.exact || !A2.exact)
            //                {
            //                    continue;
            //                }

            //                var Ax2 = (A2.x - A1.x) * (A2.x - A1.x);
            //                var Ay2 = (A2.y - A1.y) * (A2.y - A1.y);

            //                if (Ax2 + Ay2 < min2)
            //                {
            //                    continue;
            //                }

            //                var angle = Math.atan2((A2.y - A1.y), (A2.x - A1.x));

            //                var c = Math.cos(-angle);
            //                var s = Math.sin(-angle);

            //                var c2 = Math.cos(angle);
            //                var s2 = Math.sin(angle);

            //                var relA2 = { x: A2.x - A1.x, y: A2.y - A1.y};
            //            var rotA2x = relA2.x * c - relA2.y * s;

            //            for (var j = 0; j < parts.length; j++)
            //            {
            //                var B = parts[j];
            //                if (B.length > 1)
            //                {
            //                    for (var k = 0; k < B.length; k++)
            //                    {
            //                        var B1 = B[k];

            //                        if (k + 1 == B.length)
            //                        {
            //                            var B2 = B[0];
            //                        }
            //                        else
            //                        {
            //                            var B2 = B[k + 1];
            //                        }

            //                        if (!B1.exact || !B2.exact)
            //                        {
            //                            continue;
            //                        }
            //                        var Bx2 = (B2.x - B1.x) * (B2.x - B1.x);
            //                        var By2 = (B2.y - B1.y) * (B2.y - B1.y);

            //                        if (Bx2 + By2 < min2)
            //                        {
            //                            continue;
            //                        }

            //                        // B relative to A1 (our point of rotation)
            //                        var relB1 = { x: B1.x - A1.x, y: B1.y - A1.y};
            //                    var relB2 = { x: B2.x - A1.x, y: B2.y - A1.y};


            //                // rotate such that A1 and A2 are horizontal
            //                var rotB1 = { x: relB1.x* c -relB1.y * s, y: relB1.x* s +relB1.y * c};
            //            var rotB2 = { x: relB2.x* c -relB2.y * s, y: relB2.x* s +relB2.y * c};

            //					if(!GeometryUtil.almostEqual(rotB1.y, 0, tolerance) || !GeometryUtil.almostEqual(rotB2.y, 0, tolerance)){
            //						continue;
            //					}

            //					var min1 = Math.min(0, rotA2x);
            //        var max1 = Math.max(0, rotA2x);

            //        var min2 = Math.min(rotB1.x, rotB2.x);
            //        var max2 = Math.max(rotB1.x, rotB2.x);

            //					// not overlapping
            //					if(min2 >= max1 || max2 <= min1){
            //						continue;
            //					}

            //					var len = 0;
            //        var relC1x = 0;
            //        var relC2x = 0;

            //					// A is B
            //					if(GeometryUtil.almostEqual(min1, min2) && GeometryUtil.almostEqual(max1, max2)){
            //						len = max1-min1;
            //						relC1x = min1;
            //						relC2x = max1;
            //					}
            //					// A inside B
            //					else if(min1 > min2 && max1<max2){
            //						len = max1-min1;
            //						relC1x = min1;
            //						relC2x = max1;
            //					}
            //					// B inside A
            //					else if(min2 > min1 && max2<max1){
            //						len = max2-min2;
            //						relC1x = min2;
            //						relC2x = max2;
            //					}
            //					else{
            //						len = Math.max(0, Math.min(max1, max2) - Math.max(min1, min2));
            //						relC1x = Math.min(max1, max2);
            //						relC2x = Math.max(min1, min2);		
            //					}

            //					if(len* len > min2){
            //						totalLength += len;

            //						var relC1 = { x: relC1x * c2, y: relC1x * s2 };
            //var relC2 = { x: relC2x * c2, y: relC2x * s2 };

            //var C1 = { x: relC1.x + A1.x, y: relC1.y + A1.y };
            //var C2 = { x: relC2.x + A1.x, y: relC2.y + A1.y };

            //segments.push([C1, C2]);
            //					}
            //				}
            //			}

            //			if(B.children && B.children.length > 0){
            //				var child = mergedLength(B.children, p, minlength, tolerance);
            //totalLength += child.totalLength;
            //				segments = segments.concat(child.segments);
            //			}
            //		}
            //	}

            //	return {totalLength: totalLength, segments: segments};
            throw new NotImplementedException();
        }

        public class MergedResult
        {
            public double totalLength;
            public object segments;
        }


        public static NFP[] cloneNfp(NFP[] nfp, bool inner = false)
        {
            if (!inner)
            {
                return new[] { clone(nfp.First()) };
            }

            // inner nfp is actually an array of nfps
            List<NFP> newnfp = new List<NFP>();
            for (var i = 0; i < nfp.Count(); i++)
            {
                newnfp.Add(clone(nfp[i]));
            }

            return newnfp.ToArray();
        }

        public static NFP clone(NFP nfp)
        {
            NFP newnfp = new NFP();
            newnfp.source = nfp.source;
            for (var i = 0; i < nfp.length; i++)
            {
                newnfp.AddPoint(new SvgPoint(nfp[i].x, nfp[i].y));
            }

            if (nfp.children != null && nfp.children.Count > 0)
            {
                newnfp.children = new List<NFP>();
                for (int i = 0; i < nfp.children.Count; i++)
                {
                    var child = nfp.children[i];
                    NFP newchild = new NFP();
                    for (var j = 0; j < child.length; j++)
                    {
                        newchild.AddPoint(new SvgPoint(child[j].x, child[j].y));
                    }
                    newnfp.children.Add(newchild);
                }
            }

            return newnfp;
        }


        public static int callCounter = 0;

        public static Dictionary<string, NFP[]> cacheProcess = new Dictionary<string, NFP[]>();
        public static NFP[] Process2(NFP A, NFP B, int type)
        {
            var key = A.source + ";" + B.source + ";" + A.rotation + ";" + B.rotation;
            bool cacheAllow = type != 1;
            if (cacheProcess.ContainsKey(key) && cacheAllow)
            {
                return cacheProcess[key];
            }

            Stopwatch swg = Stopwatch.StartNew();
            Dictionary<string, List<PointF>> dic1 = new Dictionary<string, List<PointF>>();
            Dictionary<string, List<double>> dic2 = new Dictionary<string, List<double>>();
            dic2.Add("A", new List<double>());
            foreach (var item in A.Points)
            {
                var target = dic2["A"];
                target.Add(item.x);
                target.Add(item.y);
            }
            dic2.Add("B", new List<double>());
            foreach (var item in B.Points)
            {
                var target = dic2["B"];
                target.Add(item.x);
                target.Add(item.y);
            }


            List<double> hdat = new List<double>();

            foreach (var item in A.children)
            {
                foreach (var pitem in item.Points)
                {
                    hdat.Add(pitem.x);
                    hdat.Add(pitem.y);
                }
            }

            var aa = dic2["A"];
            var bb = dic2["B"];
            var arr1 = A.children.Select(z => z.Points.Count() * 2).ToArray();

            MinkowskiWrapper.setData(aa.Count, aa.ToArray(), A.children.Count, arr1, hdat.ToArray(), bb.Count, bb.ToArray());
            MinkowskiWrapper.calculateNFP();

            callCounter++;

            int[] sizes = new int[2];
            MinkowskiWrapper.getSizes1(sizes);
            int[] sizes1 = new int[sizes[0]];
            int[] sizes2 = new int[sizes[1]];
            MinkowskiWrapper.getSizes2(sizes1, sizes2);
            double[] dat1 = new double[sizes1.Sum()];
            double[] hdat1 = new double[sizes2.Sum()];

            MinkowskiWrapper.getResults(dat1, hdat1);

            if (sizes1.Count() > 1)
            {
                throw new ArgumentException("sizes1 cnt >1");
            }


            //convert back to answer here
            bool isa = true;
            List<PointF> Apts = new List<PointF>();



            List<List<double>> holesval = new List<List<double>>();
            bool holes = false;

            for (int i = 0; i < dat1.Length; i += 2)
            {
                var x1 = (float)dat1[i];
                var y1 = (float)dat1[i + 1];
                Apts.Add(new PointF(x1, y1));
            }

            int index = 0;
            for (int i = 0; i < sizes2.Length; i++)
            {
                holesval.Add(new List<double>());
                for (int j = 0; j < sizes2[i]; j++)
                {
                    holesval.Last().Add(hdat1[index]);
                    index++;
                }
            }

            List<List<PointF>> holesout = new List<List<PointF>>();
            foreach (var item in holesval)
            {
                holesout.Add(new List<PointF>());
                for (int i = 0; i < item.Count; i += 2)
                {
                    var x = (float)item[i];
                    var y = (float)item[i + 1];
                    holesout.Last().Add(new PointF(x, y));
                }
            }

            NFP ret = new NFP();
            ret.Points = new SvgPoint[] { };
            foreach (var item in Apts)
            {
                ret.AddPoint(new SvgPoint(item.X, item.Y));
            }


            foreach (var item in holesout)
            {
                if (ret.children == null)
                    ret.children = new List<NFP>();

                ret.children.Add(new NFP());
                ret.children.Last().Points = new SvgPoint[] { };
                foreach (var hitem in item)
                {
                    ret.children.Last().AddPoint(new SvgPoint(hitem.X, hitem.Y));
                }
            }

            swg.Stop();
            var msg = swg.ElapsedMilliseconds;
            var res = new NFP[] { ret };

            if (cacheAllow)
            {
                cacheProcess.Add(key, res);
            }
            return res;
        }

        public static NFP getFrame(NFP A)
        {
            var bounds = GeometryUtil.getPolygonBounds(A);

            // expand bounds by 10%
            bounds.width *= 1.1;
            bounds.height *= 1.1;
            bounds.x -= 0.5 * (bounds.width - (bounds.width / 1.1));
            bounds.y -= 0.5 * (bounds.height - (bounds.height / 1.1));

            var frame = new NFP();
            frame.push(new SvgPoint(bounds.x, bounds.y));
            frame.push(new SvgPoint(bounds.x + bounds.width, bounds.y));
            frame.push(new SvgPoint(bounds.x + bounds.width, bounds.y + bounds.height));
            frame.push(new SvgPoint(bounds.x, bounds.y + bounds.height));


            frame.children = new List<NFP>() { (NFP)A };
            frame.source = A.source;
            frame.rotation = 0;

            return frame;
        }

        public static NFP[] getInnerNfp(NFP A, NFP B, int type, SvgNestConfig config)
        {
            if (A.source != null && B.source != null)
            {

                var key = new DbCacheKey()
                {
                    A = A.source.Value,
                    B = B.source.Value,
                    ARotation = 0,
                    BRotation = B.rotation,
                    //Inside =true??
                };
                //var doc = window.db.find({ A: A.source, B: B.source, Arotation: 0, Brotation: B.rotation }, true);
                var res = window.db.find(key, true);
                if (res != null)
                {
                    return res;
                }
            }


            var frame = getFrame(A);

            var nfp = getOuterNfp(frame, B, type, true);

            if (nfp == null || nfp.children == null || nfp.children.Count == 0)
            {
                return null;
            }
            List<NFP> holes = new List<NFP>();
            if (A.children != null && A.children.Count > 0)
            {
                for (var i = 0; i < A.children.Count; i++)
                {
                    var hnfp = getOuterNfp(A.children[i], B, 1);
                    if (hnfp != null)
                    {
                        holes.Add(hnfp);
                    }
                }
            }

            if (holes.Count == 0)
            {
                return nfp.children.ToArray();
            }
            var clipperNfp = innerNfpToClipperCoordinates(nfp.children.ToArray(), config);
            var clipperHoles = innerNfpToClipperCoordinates(holes.ToArray(), config);

            List<List<IntPoint>> finalNfp = new List<List<IntPoint>>();
            var clipper = new ClipperLib.Clipper();

            clipper.AddPaths(clipperHoles.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptClip, true);
            clipper.AddPaths(clipperNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);

            if (!clipper.Execute(ClipperLib.ClipType.ctDifference, finalNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
            {
                return nfp.children.ToArray();
            }

            if (finalNfp.Count == 0)
            {
                return null;
            }

            List<NFP> f = new List<NFP>();
            for (var i = 0; i < finalNfp.Count; i++)
            {
                f.Add(toNestCoordinates(finalNfp[i].ToArray(), config.clipperScale));
            }

            if (A.source != null && B.source != null)
            {
                // insert into db
                //console.log('inserting inner: ', A.source, B.source, B.rotation, f);
                var doc = new DbCacheKey()
                {
                    A = A.source.Value,
                    B = B.source.Value,
                    ARotation = 0,
                    BRotation = B.rotation,
                    nfp = f.ToArray()


                };
                window.db.insert(doc, true);
            }

            return f.ToArray();

        }
        public static NFP rotatePolygon(NFP polygon, float degrees)
        {
            NFP rotated = new NFP();

            var angle = degrees * Math.PI / 180;
            List<SvgPoint> pp = new List<SvgPoint>();
            for (var i = 0; i < polygon.length; i++)
            {
                var x = polygon[i].x;
                var y = polygon[i].y;
                var x1 = (x * Math.Cos(angle) - y * Math.Sin(angle));
                var y1 = (x * Math.Sin(angle) + y * Math.Cos(angle));

                pp.Add(new SvgPoint(x1, y1));
            }
            rotated.Points = pp.ToArray();

            if (polygon.children != null && polygon.children.Count > 0)
            {
                rotated.children = new List<NFP>(); ;
                for (var j = 0; j < polygon.children.Count; j++)
                {
                    rotated.children.Add(rotatePolygon(polygon.children[j], degrees));
                }
            }

            return rotated;
        }
        /// <summary>
        /// 葛工版本
        /// </summary>
        public static SheetPlacement placeParts(NFP[] sheets, NFP[] parts, SvgNestConfig config, int nestindex)
        {
            if (sheets == null || sheets.Count() == 0) return null;


            int i, j, k, m, n;
            double totalsheetarea = 0;

            NFP part = null;
            double totalMerged = 0;

            // rotate paths by given rotation
            var rotated = new List<NFP>();
            for (i = 0; i < parts.Length; i++)
            {
                var r = rotatePolygon(parts[i], parts[i].rotation);
                r.Rotation = parts[i].rotation;
                r.source = parts[i].source;
                r.Id = parts[i].Id;
                rotated.Add(r);
            }

            parts = rotated.ToArray();

            List<SheetPlacementItem> allplacements = new List<SheetPlacementItem>();

            double fitness1 = 0;

            string key;
            NFP nfp;
            NFP nfp1;
            double sheetarea = -1;
            int totalPlaced = 0;
            int totalParts = parts.Count();
            //最后一板的样片面积
            double lastArea = 0;

            while (parts.Length > 0)
            {

                List<NFP> placed = new List<NFP>();

                List<PlacementItem> placements = new List<PlacementItem>();

                // open a new sheet
                var sheet = sheets.First();
                sheets = sheets.Skip(1).ToArray();
                sheetarea = Math.Abs(GeometryUtil.polygonArea(sheet));
                totalsheetarea += sheetarea;

                fitness1 += sheetarea;// 每打开一张新板，加1张（适合度越低越好）add 1 for each new sheet opened (lower fitness is better)

                string clipkey = "";
                Dictionary<string, ClipCacheItem> clipCache = new Dictionary<string, ClipCacheItem>();
                var clipper = new ClipperLib.Clipper();
                var combinedNfp = new List<List<ClipperLib.IntPoint>>();
                var error = false;
                IntPoint[][] clipperSheetNfp = null;
                double? minwidth = null;
                double minHeight = sheet.HeightCalculated;
                PlacementItem position = null;
                double? minarea = null;

                for (i = 0; i < parts.Length; i++)
                {
                    float prog = 0.66f + 0.34f * (totalPlaced / (float)totalParts);
                    DisplayProgress(prog);

                    part = parts[i];
                    // inner NFP
                    NFP[] sheetNfp = null;
                    // try all possible rotations until it fits
                    // (only do this for the first part of each sheet, to ensure that all parts that can be placed are, even if we have to to open a lot of sheets)
                    for (j = 0; j < (360f / config.rotations); j++)
                    {
                        sheetNfp = getInnerNfp(sheet, part, 0, config);

                        if (sheetNfp != null && sheetNfp.Count() > 0)
                        {
                            if (sheetNfp[0].length == 0)
                            {
                                throw new ArgumentException();
                            }
                            else
                            {
                                break;
                            }
                        }

                        var r = rotatePolygon(part, 360f / config.rotations);
                        r.rotation = part.rotation + (360f / config.rotations);
                        r.source = part.source;
                        r.id = part.id;

                        // rotation is not in-place
                        part = r;
                        parts[i] = r;

                        if (part.rotation > 360f)
                        {
                            part.rotation = part.rotation % 360f;
                        }
                    }
                    // part unplaceable, skip
                    if (sheetNfp == null || sheetNfp.Count() == 0)
                    {
                        continue;
                    }

                    position = null;

                    if (placed.Count == 0)
                    {
                        // first placement, put it on the top left corner
                        for (j = 0; j < sheetNfp.Count(); j++)
                        {
                            for (k = 0; k < sheetNfp[j].length; k++)
                            {
                                if (position == null ||
                                    ((sheetNfp[j][k].x - part[0].x) < position.x) ||
                                    (
                                    GeometryUtil._almostEqual(sheetNfp[j][k].x - part[0].x, position.x)
                                    && ((sheetNfp[j][k].y - part[0].y) < position.y))
                                    )
                                {
                                    position = new PlacementItem()
                                    {
                                        x = sheetNfp[j][k].x - part[0].x,
                                        y = sheetNfp[j][k].y - part[0].y,
                                        id = part.id,
                                        rotation = part.rotation,
                                        source = part.source.Value

                                    };


                                }
                            }
                        }

                        if (position == null)
                        {
                            throw new Exception("position null");
                            //console.log(sheetNfp);
                        }
                        placements.Add(position);

                        placed.Add(part);
                        totalPlaced++;

                        if (parts.Length == 1)
                        {
                            //var transp = rotatePolygon(part, position.rotation);
                            ///part已经根据原图旋转过的
                            minwidth = part.Points.Max(p => p.x) - part.Points.Min(p => p.x);
                        }

                        continue;
                    }

                    clipperSheetNfp = innerNfpToClipperCoordinates(sheetNfp, config);

                    clipper = new ClipperLib.Clipper();
                    combinedNfp = new List<List<ClipperLib.IntPoint>>();
                    error = false;

                    // check if stored in clip cache
                    //var startindex = 0;
                    clipkey = "s:" + part.source + "r:" + part.rotation;
                    var startindex = 0;
                    if (EnableCaches && clipCache.ContainsKey(clipkey))
                    {
                        var prevNfp = clipCache[clipkey].nfpp;
                        clipper.AddPaths(prevNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
                        startindex = clipCache[clipkey].index;
                    }

                    for (j = startindex; j < placed.Count; j++)
                    {
                        nfp = getOuterNfp(placed[j], part, 0);
                        //nfp1 = getOuterNfp(placed[j], part, 0);
                        // minkowski difference failed. very rare but could happen
                        if (nfp == null)
                        {
                            error = true;
                            break;
                        }
                        // shift to placed location
                        for (m = 0; m < nfp.length; m++)
                        {
                            nfp[m].x += placements[j].x;
                            nfp[m].y += placements[j].y;
                        }
                        if (nfp.children != null && nfp.children.Count > 0)
                        {
                            for (n = 0; n < nfp.children.Count; n++)
                            {
                                for (var o = 0; o < nfp.children[n].length; o++)
                                {
                                    nfp.children[n][o].x += placements[j].x;
                                    nfp.children[n][o].y += placements[j].y;
                                }
                            }
                        }

                        var clipperNfp = nfpToClipperCoordinates(nfp, config.clipperScale);

                        clipper.AddPaths(clipperNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);


                    }
                    //TODO: a lot here to insert

                    if (error || !clipper.Execute(ClipperLib.ClipType.ctUnion, combinedNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))

                    {
                        //console.log('clipper error', error);
                        continue;
                    }


                    if (EnableCaches)
                    {
                        clipCache[clipkey] = new ClipCacheItem()
                        {
                            index = placed.Count - 1,
                            nfpp = combinedNfp.Select(z => z.ToArray()).ToArray()
                        };
                    }

                    //console.log('save cache', placed.length - 1);

                    // difference with sheet polygon
                    List<List<IntPoint>> _finalNfp = new List<List<IntPoint>>();
                    clipper = new ClipperLib.Clipper();

                    clipper.AddPaths(combinedNfp, ClipperLib.PolyType.ptClip, true);

                    clipper.AddPaths(clipperSheetNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);


                    if (!clipper.Execute(ClipperLib.ClipType.ctDifference, _finalNfp, ClipperLib.PolyFillType.pftEvenOdd, ClipperLib.PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    if (_finalNfp == null || _finalNfp.Count == 0)
                    {
                        continue;
                    }


                    List<NFP> f = new List<NFP>();
                    for (j = 0; j < _finalNfp.Count; j++)
                    {
                        // back to normal scale
                        f.Add(Background.toNestCoordinates(_finalNfp[j].ToArray(), config.clipperScale));
                    }
                    var finalNfp = f;
                    //finalNfp = f;

                    // choose placement that results in the smallest bounding box/hull etc
                    // todo: generalize gravity direction
                    /*var minwidth = null;
                    var minarea = null;
                    var minx = null;
                    var miny = null;
                    var nf, area, shiftvector;*/
                    minwidth = null;
                    minarea = null;
                    double? minx = null;
                    double? miny = null;
                    NFP nf;
                    double area = 0;
                    PlacementItem shiftvector = null;


                    NFP allpoints = new NFP();
                    for (m = 0; m < placed.Count; m++)
                    {
                        for (n = 0; n < placed[m].length; n++)
                        {
                            allpoints.AddPoint(
                                new SvgPoint(
                                 placed[m][n].x + placements[m].x, placed[m][n].y + placements[m].y));
                        }
                    }

                    PolygonBounds allbounds = null;
                    PolygonBounds partbounds = null;
                    if (config.placementType == PlacementTypeEnum.gravity || config.placementType == PlacementTypeEnum.box)
                    {
                        allbounds = GeometryUtil.getPolygonBounds(allpoints);

                        NFP partpoints = new NFP();
                        for (m = 0; m < part.length; m++)
                        {
                            partpoints.AddPoint(new SvgPoint(part[m].x, part[m].y));
                        }
                        partbounds = GeometryUtil.getPolygonBounds(partpoints);
                    }
                    else
                    {
                        allpoints = getHull(allpoints);
                    }

                    int? divides = null;
                    double? rate = null;//越大越好，表示预留的面积大
                    for (j = 0; j < finalNfp.Count; j++)
                    {
                        nf = finalNfp[j];
                        //console.log('evalnf',nf.length);
                        for (k = 0; k < nf.length; k++)
                        {
                            shiftvector = new PlacementItem()
                            {
                                id = part.id,
                                x = nf[k].x - part[0].x,
                                y = nf[k].y - part[0].y,
                                source = part.source.Value,
                                rotation = part.rotation
                            };
                            PolygonBounds rectbounds = null;
                            NFP poly = new NFP();
                            poly.AddPoint(new SvgPoint(allbounds.x, allbounds.y));
                            poly.AddPoint(new SvgPoint(allbounds.x + allbounds.width, allbounds.y));
                            poly.AddPoint(new SvgPoint(allbounds.x + allbounds.width, allbounds.y + allbounds.height));
                            poly.AddPoint(new SvgPoint(allbounds.x, allbounds.y + allbounds.height));
                            /*
                         [
                            // allbounds points
                        { x: allbounds.x, y: allbounds.y},
                        { x: allbounds.x + allbounds.width, y: allbounds.y},
                        { x: allbounds.x + allbounds.width, y: allbounds.y + allbounds.height},
                        { x: allbounds.x, y: allbounds.y + allbounds.height},*/

                            poly.AddPoint(new SvgPoint(partbounds.x + shiftvector.x, partbounds.y + shiftvector.y));
                            poly.AddPoint(new SvgPoint(partbounds.x + partbounds.width + shiftvector.x, partbounds.y + shiftvector.y));
                            poly.AddPoint(new SvgPoint(partbounds.x + partbounds.width + shiftvector.x, partbounds.y + partbounds.height + shiftvector.y));
                            poly.AddPoint(new SvgPoint(partbounds.x + shiftvector.x, partbounds.y + partbounds.height + shiftvector.y));
                            /*
                             [                            

                            // part points
                            { x: partbounds.x + shiftvector.x, y: partbounds.y + shiftvector.y},
                            { x: partbounds.x + partbounds.width + shiftvector.x, y: partbounds.y + shiftvector.y},
                            { x: partbounds.x + partbounds.width + shiftvector.x, y: partbounds.y + partbounds.height + shiftvector.y},
                            { x: partbounds.x + shiftvector.x, y: partbounds.y + partbounds.height + shiftvector.y}
                        ]*/
                            rectbounds = GeometryUtil.getPolygonBounds(poly);

                            // weigh width more, to help compress in direction of gravity
                            if (config.placementType == PlacementTypeEnum.gravity)
                            {
                                area = rectbounds.width * 2 + rectbounds.height;
                            }
                            else
                            {
                                area = rectbounds.width * rectbounds.height;
                            }

                            #region
                            List<IntPoint> currentMaxRec = new List<IntPoint>();
                            currentMaxRec.Add(new IntPoint(0, 0));
                            currentMaxRec.Add(new IntPoint(0, sheet.HeightCalculated));
                            currentMaxRec.Add(new IntPoint(rectbounds.width, sheet.HeightCalculated));
                            currentMaxRec.Add(new IntPoint(rectbounds.width, 0));

                            List<IntPoint> placesBounds1 = new List<IntPoint>();
                            placesBounds1.Add(new IntPoint(allbounds.x, allbounds.y));
                            placesBounds1.Add(new IntPoint(allbounds.x + allbounds.width, allbounds.y));
                            placesBounds1.Add(new IntPoint(allbounds.x + allbounds.width, allbounds.y + allbounds.height));
                            placesBounds1.Add(new IntPoint(allbounds.x, allbounds.y + allbounds.height));

                            List<IntPoint> placesBounds2 = new List<IntPoint>();
                            placesBounds2.Add(new IntPoint(partbounds.x + shiftvector.x, partbounds.y + shiftvector.y));
                            placesBounds2.Add(new IntPoint(partbounds.x + partbounds.width + shiftvector.x, partbounds.y + shiftvector.y));
                            placesBounds2.Add(new IntPoint(partbounds.x + partbounds.width + shiftvector.x, partbounds.y + partbounds.height + shiftvector.y));
                            placesBounds2.Add(new IntPoint(partbounds.x + shiftvector.x, partbounds.y + partbounds.height + shiftvector.y));

                            var clip = new ClipperLib.Clipper();
                            clip.AddPath(placesBounds1, ClipperLib.PolyType.ptClip, true);
                            clip.AddPath(placesBounds2, ClipperLib.PolyType.ptClip, true);
                            clip.AddPath(currentMaxRec, ClipperLib.PolyType.ptSubject, true);
                            //clipper.AddPath(placesBounds1, ClipperLib.PolyType.ptClip, true);
                            //clipper.AddPath(placesBounds2, ClipperLib.PolyType.ptClip, true);
                            //clipper.AddPath(currentMaxRec, ClipperLib.PolyType.ptSubject, true);

                            List<List<IntPoint>> results = new List<List<IntPoint>>();
                            clip.Execute(ClipperLib.ClipType.ctDifference, results, ClipperLib.PolyFillType.pftEvenOdd, ClipperLib.PolyFillType.pftNonZero);
                            if (divides == null || divides.Value > results.Count)
                            {
                                divides = results.Count;
                                minwidth = rectbounds.width;
                                minHeight = rectbounds.height;
                                position = shiftvector;
                            }
                            else if (divides.Value == results.Count)
                            {
                                if (divides.Value > 1)
                                {
                                    List<double> areas = new List<double>();
                                    foreach (var item in results)
                                    {
                                        double area1 = 0;
                                        int ii, jj;
                                        for (ii = 0, jj = item.Count - 1; ii < item.Count; jj = ii++)
                                        {
                                            area += (item[jj].X + item[ii].X) * (item[jj].Y
                                                - item[ii].Y);
                                        }

                                        areas.Add(0.5 * area1);
                                    }
                                    areas.Sort((a, b) => -a.CompareTo(b));
                                    double ra = areas[0] / areas[1];
                                    if (rate == null || rate < ra)
                                    {
                                        rate = ra;
                                        minwidth = rectbounds.width;
                                        minHeight = rectbounds.height;
                                        position = shiftvector;

                                    }
                                    else if (rate == ra && minwidth > rectbounds.width)
                                    {
                                        minwidth = rectbounds.width;
                                        minHeight = rectbounds.height;
                                        position = shiftvector;
                                    }
                                }
                                else if (minwidth > rectbounds.width)
                                {
                                    minwidth = rectbounds.width;
                                    minHeight = rectbounds.height;
                                    position = shiftvector;
                                }

                            }
                            #endregion

                        }


                    }

                    //第二种计算方式，放置时保证分割的块最小
                    if (position != null)
                    {
                        placed.Add(part);
                        totalPlaced++;
                        placements.Add(position);
                        if (position.mergedLength != null)
                        {
                            totalMerged += position.mergedLength.Value;
                        }
                    }
                    // send placement progress signal
                    var placednum = placed.Count;
                    for (j = 0; j < allplacements.Count; j++)
                    {
                        placednum += allplacements[j].sheetplacements.Count;
                    }
                    //console.log(placednum, totalnum);
                    //ipcRenderer.send('background-progress', { index: nestindex, progress: 0.5 + 0.5 * (placednum / totalnum)});

                    //console.timeEnd('placement');
                }
                //if(minwidth){

                //if (!minwidth.HasValue)
                //{
                //    fitness = double.NaN;
                //}
                //else
                //{
                //    //minarea-排出所占材料面积，minwinwidth-排出最小宽度
                //    fitness += (minwidth.Value / sheetarea) + minarea.Value;
                //}
                if (minwidth.HasValue)
                {
                    double polyarea = 0;
                    foreach (var item in placed)
                    {
                        polyarea += Math.Abs(GeometryUtil.polygonArea(item));
                    }
                    fitness1 += minwidth.Value * sheet.HeightCalculated / polyarea * 100;//当前排版的利用率的倒数，因为算法的适应度是越小越好
                    //fitness1 += minHeight / sheet.HeightCalculated;
                    if (placed.Count == parts.Length)
                    {
                        //表示是最后一板的使用面积
                        lastArea = minwidth.Value * sheet.HeightCalculated;
                    }
                }

                for (i = 0; i < placed.Count; i++)
                {
                    var index = Array.IndexOf(parts, placed[i]);
                    if (index >= 0)
                    {
                        parts = parts.splice(index, 1);
                    }
                }

                if (placements != null && placements.Count > 0)
                {
                    allplacements.Add(new SheetPlacementItem()
                    {
                        sheetId = sheet.id,
                        sheetSource = sheet.source.Value,
                        sheetplacements = placements
                    });
                    //allplacements.Add({ sheet: sheet.source, sheetid: sheet.id, sheetplacements: placements});
                }
                else
                {
                    break; // something went wrong
                }

                if (sheets.Count() == 0)
                {
                    break;
                }

            }
            // there were parts that couldn't be placed //计算没有排版的料子
            // scale this value high - we really want to get all the parts in, even at the cost of opening new sheets
            for (i = 0; i < parts.Count(); i++)
            {
                fitness1 += 100000000 * (Math.Abs(GeometryUtil.polygonArea(parts[i])) / totalsheetarea);
                //fitness += Math.Abs(GeometryUtil.polygonArea(parts[i])) / totalsheetarea;
            }
            // send finish progerss signal
            //ipcRenderer.send('background-progress', { index: nestindex, progress: -1});

            //假设最后一板的轮廓没排上
            if (allplacements.Count > 1)
            {
                //尽量使最后一板的面积更低
                fitness1 += lastArea;
            }

            return new SheetPlacement()
            {
                placements = new[] { allplacements.ToList() },
                fitness = fitness1,
                //  paths = paths,
                area = sheetarea,
                mergedLength = totalMerged


            };
            //return { placements: allplacements, fitness: fitness, area: sheetarea, mergedLength: totalMerged };
        }
        static void ExpandPlacementBounds(NFP placedPart, double originX, double originY, ref double minX, ref double minY, ref double maxX, ref double maxY)
        {
            if (placedPart == null) return;
            for (int i = 0; i < placedPart.length; i++)
            {
                double x = placedPart[i].x + originX;
                double y = placedPart[i].y + originY;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        static void AddResidualCoordinate(double[] values, ref int count, double value, double min, double max)
        {
            if (value < min) value = min;
            else if (value > max) value = max;
            for (int i = 0; i < count; i++)
            {
                if (Math.Abs(values[i] - value) <= 1e-7) return;
            }
            values[count++] = value;
        }

        static void SortResidualCoordinates(double[] values, int count)
        {
            for (int i = 1; i < count; i++)
            {
                double value = values[i];
                int j = i - 1;
                while (j >= 0 && values[j] > value)
                {
                    values[j + 1] = values[j];
                    j--;
                }
                values[j + 1] = value;
            }
        }

        static int FindResidualRoot(int[] parent, int index)
        {
            while (parent[index] != index)
            {
                parent[index] = parent[parent[index]];
                index = parent[index];
            }
            return index;
        }

        static void UnionResidual(int[] parent, int left, int right)
        {
            int leftRoot = FindResidualRoot(parent, left);
            int rightRoot = FindResidualRoot(parent, right);
            if (leftRoot != rightRoot) parent[rightRoot] = leftRoot;
        }

        sealed class PlaceParts1BeamState
        {
            public readonly List<NFP> Placed = new List<NFP>();
            public readonly List<PlacementItem> Placements = new List<PlacementItem>();
            public double MinX = double.MaxValue;
            public double MinY = double.MaxValue;
            public double MaxX = double.MinValue;
            public double MaxY = double.MinValue;
            public int Fragments = int.MaxValue;
            public double ResidualRatio;

            public double Width { get { return Placed.Count == 0 ? 0 : MaxX - MinX; } }
            public double Height { get { return Placed.Count == 0 ? 0 : MaxY - MinY; } }

            public PlaceParts1BeamState Extend(NFP part, PlacementItem placement, int fragments, double ratio)
            {
                var next = new PlaceParts1BeamState
                {
                    MinX = MinX,
                    MinY = MinY,
                    MaxX = MaxX,
                    MaxY = MaxY,
                    Fragments = fragments,
                    ResidualRatio = ratio
                };
                next.Placed.AddRange(Placed);
                next.Placements.AddRange(Placements);
                next.Placed.Add(part);
                next.Placements.Add(new PlacementItem
                {
                    id = placement.id,
                    x = placement.x,
                    y = placement.y,
                    source = placement.source,
                    rotation = placement.rotation
                });
                ExpandPlacementBounds(part, placement.x, placement.y, ref next.MinX, ref next.MinY, ref next.MaxX, ref next.MaxY);
                return next;
            }
        }

        sealed class PlaceParts1BeamCandidate
        {
            public PlacementItem Placement;
            public double Width;
            public double Height;
            public int Fragments;
            public double ResidualRatio;
        }

        static int ComparePlaceParts1Candidates(PlaceParts1BeamCandidate left, PlaceParts1BeamCandidate right)
        {
            int compare = left.Width.CompareTo(right.Width);
            if (compare != 0) return compare;
            compare = left.Height.CompareTo(right.Height);
            if (compare != 0) return compare;
            compare = left.Fragments.CompareTo(right.Fragments);
            if (compare != 0) return compare;
            compare = right.ResidualRatio.CompareTo(left.ResidualRatio);
            if (compare != 0) return compare;
            compare = left.Placement.x.CompareTo(right.Placement.x);
            return compare != 0 ? compare : left.Placement.y.CompareTo(right.Placement.y);
        }

        static int ComparePlaceParts1BeamStates(PlaceParts1BeamState left, PlaceParts1BeamState right)
        {
            int compare = right.Placed.Count.CompareTo(left.Placed.Count);
            if (compare != 0) return compare;
            compare = left.Width.CompareTo(right.Width);
            if (compare != 0) return compare;
            compare = left.Height.CompareTo(right.Height);
            if (compare != 0) return compare;
            compare = left.Fragments.CompareTo(right.Fragments);
            return compare != 0 ? compare : right.ResidualRatio.CompareTo(left.ResidualRatio);
        }

        static List<PlaceParts1BeamState> SelectPlaceParts1Beam(List<PlaceParts1BeamState> states, int width)
        {
            states.Sort(ComparePlaceParts1BeamStates);
            var result = new List<PlaceParts1BeamState>(Math.Min(width, states.Count));
            var signatures = new HashSet<string>();
            foreach (var state in states)
            {
                string signature = string.Join(";", state.Placements.Select(p => p.id + ":" +
                    Math.Round(p.x * 10000).ToString() + "," + Math.Round(p.y * 10000).ToString()));
                if (!signatures.Add(signature)) continue;
                result.Add(state);
                if (result.Count >= width) break;
            }
            return result;
        }

        [ThreadStatic] static double[] residualXs;
        [ThreadStatic] static double[] residualYs;
        [ThreadStatic] static int[] residualCellRoot;
        [ThreadStatic] static int[] residualParent;
        [ThreadStatic] static double[] residualArea;
        [ThreadStatic] static double[] residualRootArea;
        [ThreadStatic] static bool[] residualSeen;

        // 矩形减去两个轴对齐包围盒，统计剩余连通块和最大/次大面积比。替代每个候选点一次 Clipper。
        static void ScorePlaceParts1Residual(
            double rectWidth,
            double rectHeight,
            double ax0, double ay0, double ax1, double ay1,
            double bx0, double by0, double bx1, double by1,
            out int components,
            out double ratio)
        {
            components = 0;
            ratio = 0;
            if (rectWidth <= 1e-9 || rectHeight <= 1e-9) return;

            if (residualXs == null)
            {
                residualXs = new double[6];
                residualYs = new double[6];
                residualCellRoot = new int[25];
                residualParent = new int[25];
                residualArea = new double[25];
                residualRootArea = new double[25];
                residualSeen = new bool[25];
            }
            var xs = residualXs;
            var ys = residualYs;
            int nx = 0;
            int ny = 0;
            AddResidualCoordinate(xs, ref nx, 0, 0, rectWidth);
            AddResidualCoordinate(xs, ref nx, rectWidth, 0, rectWidth);
            AddResidualCoordinate(xs, ref nx, ax0, 0, rectWidth);
            AddResidualCoordinate(xs, ref nx, ax1, 0, rectWidth);
            AddResidualCoordinate(xs, ref nx, bx0, 0, rectWidth);
            AddResidualCoordinate(xs, ref nx, bx1, 0, rectWidth);
            AddResidualCoordinate(ys, ref ny, 0, 0, rectHeight);
            AddResidualCoordinate(ys, ref ny, rectHeight, 0, rectHeight);
            AddResidualCoordinate(ys, ref ny, ay0, 0, rectHeight);
            AddResidualCoordinate(ys, ref ny, ay1, 0, rectHeight);
            AddResidualCoordinate(ys, ref ny, by0, 0, rectHeight);
            AddResidualCoordinate(ys, ref ny, by1, 0, rectHeight);
            SortResidualCoordinates(xs, nx);
            SortResidualCoordinates(ys, ny);
            if (nx < 2 || ny < 2) return;

            int cols = nx - 1;
            int rows = ny - 1;
            int cellCount = cols * rows;
            var cellRoot = residualCellRoot;
            var parent = residualParent;
            var area = residualArea;
            for (int i = 0; i < cellCount; i++) cellRoot[i] = -1;

            int free = 0;
            for (int iy = 0; iy < rows; iy++)
            {
                double y0 = ys[iy];
                double y1 = ys[iy + 1];
                double height = y1 - y0;
                if (height <= 1e-9) continue;
                for (int ix = 0; ix < cols; ix++)
                {
                    double x0 = xs[ix];
                    double x1 = xs[ix + 1];
                    double width = x1 - x0;
                    if (width <= 1e-9) continue;
                    double mx = (x0 + x1) * 0.5;
                    double my = (y0 + y1) * 0.5;
                    bool covered =
                        (mx >= ax0 && mx <= ax1 && my >= ay0 && my <= ay1) ||
                        (mx >= bx0 && mx <= bx1 && my >= by0 && my <= by1);
                    if (covered) continue;
                    int cell = iy * cols + ix;
                    cellRoot[cell] = free;
                    parent[free] = free;
                    area[free] = width * height;
                    free++;
                }
            }
            if (free == 0) return;

            for (int iy = 0; iy < rows; iy++)
            {
                for (int ix = 0; ix < cols; ix++)
                {
                    int id = cellRoot[iy * cols + ix];
                    if (id < 0) continue;
                    if (ix + 1 < cols)
                    {
                        int right = cellRoot[iy * cols + ix + 1];
                        if (right >= 0) UnionResidual(parent, id, right);
                    }
                    if (iy + 1 < rows)
                    {
                        int up = cellRoot[(iy + 1) * cols + ix];
                        if (up >= 0) UnionResidual(parent, id, up);
                    }
                }
            }

            var rootArea = residualRootArea;
            var seen = residualSeen;
            for (int i = 0; i < free; i++)
            {
                rootArea[i] = 0;
                seen[i] = false;
            }
            double largest = 0;
            double second = 0;
            for (int i = 0; i < free; i++)
            {
                int root = FindResidualRoot(parent, i);
                if (!seen[root])
                {
                    seen[root] = true;
                    components++;
                }
                rootArea[root] += area[i];
            }
            for (int i = 0; i < free; i++)
            {
                if (rootArea[i] <= 0) continue;
                if (rootArea[i] > largest)
                {
                    second = largest;
                    largest = rootArea[i];
                }
                else if (rootArea[i] > second)
                {
                    second = rootArea[i];
                }
            }
            if (components > 1 && second > 1e-9) ratio = largest / second;
            else if (components > 1 && largest > 0) ratio = largest * 1e6;
        }

        static List<PlaceParts1BeamCandidate> GetPlaceParts1BeamCandidates(
            NFP[] sheetNfp,
            NFP sheet,
            NFP part,
            PlaceParts1BeamState state,
            SvgNestConfig config,
            int maxCandidates)
        {
            var candidates = new List<PlaceParts1BeamCandidate>();
            if (sheetNfp == null || sheetNfp.Length == 0 || part == null || part.length == 0) return candidates;

            double partMinX = double.MaxValue;
            double partMinY = double.MaxValue;
            double partMaxX = double.MinValue;
            double partMaxY = double.MinValue;
            for (int i = 0; i < part.length; i++)
            {
                partMinX = Math.Min(partMinX, part[i].x);
                partMinY = Math.Min(partMinY, part[i].y);
                partMaxX = Math.Max(partMaxX, part[i].x);
                partMaxY = Math.Max(partMaxY, part[i].y);
            }

            if (state.Placed.Count == 0)
            {
                PlacementItem first = null;
                for (int i = 0; i < sheetNfp.Length; i++)
                {
                    for (int j = 0; j < sheetNfp[i].length; j++)
                    {
                        double x = sheetNfp[i][j].x - part[0].x;
                        double y = sheetNfp[i][j].y - part[0].y;
                        if (first == null || x < first.x || (GeometryUtil._almostEqual(x, first.x) && y < first.y))
                        {
                            first = new PlacementItem { id = part.id, source = part.source.Value, rotation = part.rotation, x = x, y = y };
                        }
                    }
                }
                if (first != null)
                {
                    candidates.Add(new PlaceParts1BeamCandidate
                    {
                        Placement = first,
                        Width = partMaxX - partMinX,
                        Height = partMaxY - partMinY,
                        Fragments = 0,
                        ResidualRatio = 0
                    });
                }
                return candidates;
            }

            IntPoint[][] clipperSheetNfp = innerNfpToClipperCoordinates(sheetNfp, config);
            var clipper = new ClipperLib.Clipper();
            var combinedNfp = new List<List<IntPoint>>();
            for (int i = 0; i < state.Placed.Count; i++)
            {
                NFP outerNfp = getOuterNfp(state.Placed[i], part, 0);
                if (outerNfp == null) return candidates;
                for (int j = 0; j < outerNfp.length; j++)
                {
                    outerNfp[j].x += state.Placements[i].x;
                    outerNfp[j].y += state.Placements[i].y;
                }
                if (outerNfp.children != null)
                {
                    foreach (var child in outerNfp.children)
                    {
                        for (int j = 0; j < child.length; j++)
                        {
                            child[j].x += state.Placements[i].x;
                            child[j].y += state.Placements[i].y;
                        }
                    }
                }
                var paths = nfpToClipperCoordinates(outerNfp, config.clipperScale);
                clipper.AddPaths(paths.Select(path => path.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
            }

            if (!clipper.Execute(ClipperLib.ClipType.ctUnion, combinedNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
                return candidates;

            var finalClipperNfp = new List<List<IntPoint>>();
            clipper = new ClipperLib.Clipper();
            clipper.AddPaths(combinedNfp, ClipperLib.PolyType.ptClip, true);
            clipper.AddPaths(clipperSheetNfp.Select(path => path.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
            if (!clipper.Execute(ClipperLib.ClipType.ctDifference, finalClipperNfp, ClipperLib.PolyFillType.pftEvenOdd, ClipperLib.PolyFillType.pftNonZero))
                return candidates;

            var sheetBounds = GeometryUtil.getPolygonBounds(sheet);
            double sheetHeight = sheetBounds.height;
            var seen = new HashSet<string>();
            for (int i = 0; i < finalClipperNfp.Count; i++)
            {
                NFP finalNfp = toNestCoordinates(finalClipperNfp[i].ToArray(), config.clipperScale);
                for (int j = 0; j < finalNfp.length; j++)
                {
                    double shiftX = finalNfp[j].x - part[0].x;
                    double shiftY = finalNfp[j].y - part[0].y;
                    string key = Math.Round(shiftX * 10000).ToString() + "," + Math.Round(shiftY * 10000).ToString();
                    if (!seen.Add(key)) continue;

                    double candMinX = partMinX + shiftX;
                    double candMinY = partMinY + shiftY;
                    double candMaxX = partMaxX + shiftX;
                    double candMaxY = partMaxY + shiftY;
                    double minX = Math.Min(state.MinX, candMinX);
                    double minY = Math.Min(state.MinY, candMinY);
                    double maxX = Math.Max(state.MaxX, candMaxX);
                    double maxY = Math.Max(state.MaxY, candMaxY);
                    double width = maxX - minX;
                    double height = maxY - minY;
                    int fragments;
                    double ratio;
                    ScorePlaceParts1Residual(
                        width,
                        sheetHeight,
                        state.MinX - minX, state.MinY - sheetBounds.y, state.MaxX - minX, state.MaxY - sheetBounds.y,
                        candMinX - minX, candMinY - sheetBounds.y, candMaxX - minX, candMaxY - sheetBounds.y,
                        out fragments,
                        out ratio);
                    candidates.Add(new PlaceParts1BeamCandidate
                    {
                        Placement = new PlacementItem { id = part.id, source = part.source.Value, rotation = part.rotation, x = shiftX, y = shiftY },
                        Width = width,
                        Height = height,
                        Fragments = fragments,
                        ResidualRatio = ratio
                    });
                }
            }

            candidates.Sort(ComparePlaceParts1Candidates);
            if (candidates.Count > maxCandidates)
                candidates.RemoveRange(maxCandidates, candidates.Count - maxCandidates);
            return candidates;
        }

        static List<PlaceParts1BeamState> ExpandPlaceParts1Beam(
            List<PlaceParts1BeamState> beam,
            NFP[] sheetNfp,
            NFP sheet,
            NFP part,
            SvgNestConfig config,
            int beamWidth)
        {
            var expanded = new List<PlaceParts1BeamState>();
            for (int i = 0; i < beam.Count; i++)
            {
                var candidates = GetPlaceParts1BeamCandidates(sheetNfp, sheet, part, beam[i], config, beamWidth);
                if (candidates.Count == 0)
                {
                    expanded.Add(beam[i]);
                    continue;
                }
                foreach (var candidate in candidates)
                {
                    expanded.Add(beam[i].Extend(part, candidate.Placement, candidate.Fragments, candidate.ResidualRatio));
                }
            }
            if (expanded.Count == 0) expanded.AddRange(beam);
            return SelectPlaceParts1Beam(expanded, beamWidth);
        }
        /// <summary>
        /// 贪心+束搜索放置：placeParts1
        /// </summary>
        public static SheetPlacement placeParts1(NFP[] sheets, NFP[] parts, SvgNestConfig config, int nestindex)
        {
            if (sheets == null || sheets.Count() == 0) return null;


            int i, j, k, m, n;
            double totalsheetarea = 0;

            NFP part = null;
            double totalMerged = 0;

            // rotate paths by given rotation
            var rotated = new List<NFP>();
            for (i = 0; i < parts.Length; i++)
            {
                var r = rotatePolygon(parts[i], parts[i].rotation);
                r.Rotation = parts[i].rotation;
                r.source = parts[i].source;
                r.Id = parts[i].Id;
                rotated.Add(r);
            }

            parts = rotated.ToArray();

            List<SheetPlacementItem> allplacements = new List<SheetPlacementItem>();

            double fitness1 = 0;

            string key;
            NFP nfp;
            NFP nfp1;
            double sheetarea = -1;
            int totalPlaced = 0;
            int totalParts = parts.Count();
            //最后一板的样片面积
            double lastArea = 0;

            while (parts.Length > 0)
            {

                List<NFP> placed = new List<NFP>();

                List<PlacementItem> placements = new List<PlacementItem>();

                // open a new sheet
                var sheet = sheets.First();
                sheets = sheets.Skip(1).ToArray();
                sheetarea = Math.Abs(GeometryUtil.polygonArea(sheet));
                var sheetBounds = GeometryUtil.getPolygonBounds(sheet);
                totalsheetarea += sheetarea;

                fitness1 += sheetarea;// 每打开一张新板，加1张（适合度越低越好）add 1 for each new sheet opened (lower fitness is better)

                string clipkey = "";
                Dictionary<string, ClipCacheItem> clipCache = new Dictionary<string, ClipCacheItem>();
                var clipper = new ClipperLib.Clipper();
                var combinedNfp = new List<List<ClipperLib.IntPoint>>();
                var error = false;
                IntPoint[][] clipperSheetNfp = null;
                double? minwidth = null;
                double minHeight = sheet.HeightCalculated;
                PlacementItem position = null;
                double? minarea = null;
                double placedMinX = double.MaxValue;
                double placedMinY = double.MaxValue;
                double placedMaxX = double.MinValue;
                double placedMaxY = double.MinValue;
                double sheetHeight = minHeight;
                double sheetMinY = sheetBounds.y;

                int beamWidth = Math.Max(1, config.PlaceParts1BeamWidth);
                int beamPrefix = Math.Min(parts.Length, Math.Max(0, config.PlaceParts1LookaheadParts));
                if (beamPrefix > 0)
                {
                    var beam = new List<PlaceParts1BeamState> { new PlaceParts1BeamState() };
                    for (int seedIndex = 0; seedIndex < beamPrefix; seedIndex++)
                    {
                        part = parts[seedIndex];
                        NFP[] seedSheetNfp = null;
                        for (j = 0; j < (360f / config.rotations); j++)
                        {
                            seedSheetNfp = getInnerNfp(sheet, part, 0, config);
                            if (seedSheetNfp != null && seedSheetNfp.Length > 0)
                            {
                                if (seedSheetNfp[0].length == 0) throw new ArgumentException();
                                break;
                            }

                            var rotatedPart = rotatePolygon(part, 360f / config.rotations);
                            rotatedPart.rotation = part.rotation + (360f / config.rotations);
                            rotatedPart.source = part.source;
                            rotatedPart.id = part.id;
                            part = rotatedPart;
                            parts[seedIndex] = rotatedPart;
                            if (part.rotation > 360f) part.rotation %= 360f;
                        }

                        if (seedSheetNfp != null && seedSheetNfp.Length > 0)
                            beam = ExpandPlaceParts1Beam(beam, seedSheetNfp, sheet, part, config, beamWidth);
                    }

                    var selectedState = SelectPlaceParts1Beam(beam, 1)[0];
                    placed.AddRange(selectedState.Placed);
                    placements.AddRange(selectedState.Placements);
                    totalPlaced += selectedState.Placed.Count;
                    placedMinX = selectedState.MinX;
                    placedMinY = selectedState.MinY;
                    placedMaxX = selectedState.MaxX;
                    placedMaxY = selectedState.MaxY;
                    if (selectedState.Placed.Count > 0)
                    {
                        minwidth = selectedState.Width;
                        minHeight = selectedState.Height;
                    }
                }

                for (i = beamPrefix; i < parts.Length; i++)
                {
                    float prog = 0.66f + 0.34f * (totalPlaced / (float)totalParts);
                    DisplayProgress(prog);

                    part = parts[i];
                    // inner NFP
                    NFP[] sheetNfp = null;
                    // try all possible rotations until it fits
                    // (only do this for the first part of each sheet, to ensure that all parts that can be placed are, even if we have to to open a lot of sheets)
                    for (j = 0; j < (360f / config.rotations); j++)
                    {
                        sheetNfp = getInnerNfp(sheet, part, 0, config);

                        if (sheetNfp != null && sheetNfp.Count() > 0)
                        {
                            if (sheetNfp[0].length == 0)
                            {
                                throw new ArgumentException();
                            }
                            else
                            {
                                break;
                            }
                        }

                        var r = rotatePolygon(part, 360f / config.rotations);
                        r.rotation = part.rotation + (360f / config.rotations);
                        r.source = part.source;
                        r.id = part.id;

                        // rotation is not in-place
                        part = r;
                        parts[i] = r;

                        if (part.rotation > 360f)
                        {
                            part.rotation = part.rotation % 360f;
                        }
                    }
                    // part unplaceable, skip
                    if (sheetNfp == null || sheetNfp.Count() == 0)
                    {
                        continue;
                    }

                    position = null;

                    if (placed.Count == 0)
                    {
                        // first placement, put it on the top left corner
                        for (j = 0; j < sheetNfp.Count(); j++)
                        {
                            for (k = 0; k < sheetNfp[j].length; k++)
                            {
                                if (position == null ||
                                    ((sheetNfp[j][k].x - part[0].x) < position.x) ||
                                    (
                                    GeometryUtil._almostEqual(sheetNfp[j][k].x - part[0].x, position.x)
                                    && ((sheetNfp[j][k].y - part[0].y) < position.y))
                                    )
                                {
                                    position = new PlacementItem()
                                    {
                                        x = sheetNfp[j][k].x - part[0].x,
                                        y = sheetNfp[j][k].y - part[0].y,
                                        id = part.id,
                                        rotation = part.rotation,
                                        source = part.source.Value

                                    };


                                }
                            }
                        }

                        if (position == null)
                        {
                            throw new Exception("position null");
                            //console.log(sheetNfp);
                        }
                        placements.Add(position);

                        placed.Add(part);
                        ExpandPlacementBounds(part, position.x, position.y, ref placedMinX, ref placedMinY, ref placedMaxX, ref placedMaxY);
                        totalPlaced++;

                        if (parts.Length == 1)
                        {
                            //var transp = rotatePolygon(part, position.rotation);
                            ///part已经根据原图旋转过的
                            minwidth = part.Points.Max(p => p.x) - part.Points.Min(p => p.x);
                        }

                        continue;
                    }

                    clipperSheetNfp = innerNfpToClipperCoordinates(sheetNfp, config);

                    clipper = new ClipperLib.Clipper();
                    combinedNfp = new List<List<ClipperLib.IntPoint>>();
                    error = false;

                    // check if stored in clip cache
                    //var startindex = 0;
                    clipkey = "s:" + part.source + "r:" + part.rotation;
                    var startindex = 0;
                    if (EnableCaches && clipCache.ContainsKey(clipkey))
                    {
                        var prevNfp = clipCache[clipkey].nfpp;
                        clipper.AddPaths(prevNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
                        startindex = clipCache[clipkey].index;
                    }

                    NFP[] outerNfps = null;
                    int outerCount = placed.Count - startindex;
                    if (outerCount >= 4 && Environment.ProcessorCount > 1)
                    {
                        outerNfps = new NFP[placed.Count];
                        int nfpStart = startindex;
                        var nfpPart = part;
                        var nfpPlaced = placed;
                        Parallel.For(nfpStart, placed.Count, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, pj =>
                        {
                            outerNfps[pj] = getOuterNfp(nfpPlaced[pj], nfpPart, 0);
                        });
                    }

                    for (j = startindex; j < placed.Count; j++)
                    {
                        nfp = outerNfps != null ? outerNfps[j] : getOuterNfp(placed[j], part, 0);
                        // minkowski difference failed. very rare but could happen
                        if (nfp == null)
                        {
                            error = true;
                            break;
                        }
                        // shift to placed location
                        for (m = 0; m < nfp.length; m++)
                        {
                            nfp[m].x += placements[j].x;
                            nfp[m].y += placements[j].y;
                        }
                        if (nfp.children != null && nfp.children.Count > 0)
                        {
                            for (n = 0; n < nfp.children.Count; n++)
                            {
                                for (var o = 0; o < nfp.children[n].length; o++)
                                {
                                    nfp.children[n][o].x += placements[j].x;
                                    nfp.children[n][o].y += placements[j].y;
                                }
                            }
                        }

                        var clipperNfp = nfpToClipperCoordinates(nfp, config.clipperScale);

                        clipper.AddPaths(clipperNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
                       

                    }
                    //TODO: a lot here to insert

                    if (error || !clipper.Execute(ClipperLib.ClipType.ctUnion, combinedNfp, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
                       
                    {
                        //console.log('clipper error', error);
                        continue;
                    }


                    if (EnableCaches)
                    {
                        clipCache[clipkey] = new ClipCacheItem()
                        {
                            index = placed.Count - 1,
                            nfpp = combinedNfp.Select(z => z.ToArray()).ToArray()
                        };
                    }

                    //console.log('save cache', placed.length - 1);

                    // difference with sheet polygon
                    List<List<IntPoint>> _finalNfp = new List<List<IntPoint>>();
                    clipper = new ClipperLib.Clipper();

                    clipper.AddPaths(combinedNfp, ClipperLib.PolyType.ptClip, true);

                    clipper.AddPaths(clipperSheetNfp.Select(z => z.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);


                    if (!clipper.Execute(ClipperLib.ClipType.ctDifference, _finalNfp, ClipperLib.PolyFillType.pftEvenOdd, ClipperLib.PolyFillType.pftNonZero))
                    {
                        continue;
                    }

                    if (_finalNfp == null || _finalNfp.Count == 0)
                    {
                        continue;
                    }


                    List<NFP> f = new List<NFP>();
                    for (j = 0; j < _finalNfp.Count; j++)
                    {
                        // back to normal scale
                        f.Add(Background.toNestCoordinates(_finalNfp[j].ToArray(), config.clipperScale));
                    }
                    var finalNfp = f;
                    //finalNfp = f;

                    // choose placement that results in the smallest bounding box/hull etc
                    // todo: generalize gravity direction
                    /*var minwidth = null;
                    var minarea = null;
                    var minx = null;
                    var miny = null;
                    var nf, area, shiftvector;*/
                    minwidth = null;
                    minHeight = sheetHeight;
                    minarea = null;
                    double partMinX = double.MaxValue;
                    double partMinY = double.MaxValue;
                    double partMaxX = double.MinValue;
                    double partMaxY = double.MinValue;
                    for (m = 0; m < part.length; m++)
                    {
                        if (part[m].x < partMinX) partMinX = part[m].x;
                        if (part[m].y < partMinY) partMinY = part[m].y;
                        if (part[m].x > partMaxX) partMaxX = part[m].x;
                        if (part[m].y > partMaxY) partMaxY = part[m].y;
                    }

                    int bestFragments = int.MaxValue;
                    double bestRatio = 0;
                    for (j = 0; j < finalNfp.Count; j++)
                    {
                        var nf = finalNfp[j];
                        for (k = 0; k < nf.length; k++)
                        {
                            if (k > 0 && GeometryUtil._almostEqual(nf[k].x, nf[k - 1].x) && GeometryUtil._almostEqual(nf[k].y, nf[k - 1].y))
                                continue;

                            double shiftX = nf[k].x - part[0].x;
                            double shiftY = nf[k].y - part[0].y;
                            double candMinX = partMinX + shiftX;
                            double candMinY = partMinY + shiftY;
                            double candMaxX = partMaxX + shiftX;
                            double candMaxY = partMaxY + shiftY;
                            double unionMinX = placedMinX < candMinX ? placedMinX : candMinX;
                            double unionMinY = placedMinY < candMinY ? placedMinY : candMinY;
                            double unionMaxX = placedMaxX > candMaxX ? placedMaxX : candMaxX;
                            double unionMaxY = placedMaxY > candMaxY ? placedMaxY : candMaxY;
                            double candidateWidth = unionMaxX - unionMinX;
                            double candidateHeight = unionMaxY - unionMinY;
                            if (minwidth.HasValue && candidateWidth > minwidth.Value + 1e-9)
                                continue;

                            int fragments;
                            double residualRatio;
                            ScorePlaceParts1Residual(
                                candidateWidth,
                                sheetHeight,
                                placedMinX - unionMinX, placedMinY - sheetMinY, placedMaxX - unionMinX, placedMaxY - sheetMinY,
                                candMinX - unionMinX, candMinY - sheetMinY, candMaxX - unionMinX, candMaxY - sheetMinY,
                                out fragments,
                                out residualRatio);

                            bool better = !minwidth.HasValue || candidateWidth < minwidth.Value - 1e-9;
                            if (!better && Math.Abs(candidateWidth - minwidth.Value) <= 1e-9)
                            {
                                double currentHeight = minHeight;
                                if (candidateHeight < currentHeight - 1e-9)
                                {
                                    better = true;
                                }
                                else if (Math.Abs(candidateHeight - currentHeight) <= 1e-9)
                                {
                                    better = fragments < bestFragments
                                        || (fragments == bestFragments && residualRatio > bestRatio + 1e-9);
                                }
                            }
                            if (!better) continue;

                            bestFragments = fragments;
                            bestRatio = residualRatio;
                            minwidth = candidateWidth;
                            minHeight = candidateHeight;
                            position = new PlacementItem()
                            {
                                id = part.id,
                                x = shiftX,
                                y = shiftY,
                                source = part.source.Value,
                                rotation = part.rotation
                            };
                        }
                    }

                    // 束搜索已提交前缀分支；后续贪心放置优先缩小占用宽高，分块仅用于平局判定。
                    if (position != null)
                    {
                        placed.Add(part);
                        ExpandPlacementBounds(part, position.x, position.y, ref placedMinX, ref placedMinY, ref placedMaxX, ref placedMaxY);
                        totalPlaced++;
                        placements.Add(position);
                        if (position.mergedLength != null)
                        {
                            totalMerged += position.mergedLength.Value;
                        }
                    }
                    // send placement progress signal
                    var placednum = placed.Count;
                    for (j = 0; j < allplacements.Count; j++)
                    {
                        placednum += allplacements[j].sheetplacements.Count;
                    }
                    //console.log(placednum, totalnum);
                    //ipcRenderer.send('background-progress', { index: nestindex, progress: 0.5 + 0.5 * (placednum / totalnum)});

                    //console.timeEnd('placement');
                }
                //if(minwidth){

                //if (!minwidth.HasValue)
                //{
                //    fitness = double.NaN;
                //}
                //else
                //{
                //    //minarea-排出所占材料面积，minwinwidth-排出最小宽度
                //    fitness += (minwidth.Value / sheetarea) + minarea.Value;
                //}
                if (minwidth.HasValue)
                {
                    double polyarea = 0;
                    foreach (var item in placed)
                    {
                        polyarea += Math.Abs(GeometryUtil.polygonArea(item));
                    }
                    fitness1 += minwidth.Value * sheet.HeightCalculated / polyarea * 100;//当前排版的利用率的倒数，因为算法的适应度是越小越好
                    //fitness1 += minHeight / sheet.HeightCalculated;
                    if (placed.Count == parts.Length)
                    {
                        //表示是最后一板的使用面积
                        lastArea = minwidth.Value * sheet.HeightCalculated;
                    }
                }

                for (i = 0; i < placed.Count; i++)
                {
                    var index = Array.IndexOf(parts, placed[i]);
                    if (index >= 0)
                    {
                        parts = parts.splice(index, 1);
                    }
                }

                if (placements != null && placements.Count > 0)
                {
                    allplacements.Add(new SheetPlacementItem()
                    {
                        sheetId = sheet.id,
                        sheetSource = sheet.source.Value,
                        sheetplacements = placements
                    });
                    //allplacements.Add({ sheet: sheet.source, sheetid: sheet.id, sheetplacements: placements});
                }
                else
                {
                    break; // something went wrong
                }

                if (sheets.Count() == 0)
                {
                    break;
                }

            }
            // there were parts that couldn't be placed //计算没有排版的料子
            // scale this value high - we really want to get all the parts in, even at the cost of opening new sheets
            for (i = 0; i < parts.Count(); i++)
            {
                fitness1 += 100000000 * (Math.Abs(GeometryUtil.polygonArea(parts[i])) / totalsheetarea);
                //fitness += Math.Abs(GeometryUtil.polygonArea(parts[i])) / totalsheetarea;
            }
            // send finish progerss signal
            //ipcRenderer.send('background-progress', { index: nestindex, progress: -1});

            //假设最后一板的轮廓没排上
            if (allplacements.Count > 1)
            {
                //尽量使最后一板的面积更低
                fitness1 += lastArea;
            }

            return new SheetPlacement()
            {
                placements = new[] { allplacements.ToList() },
                fitness = fitness1,
                //  paths = paths,
                area = sheetarea,
                mergedLength = totalMerged


            };
            //return { placements: allplacements, fitness: fitness, area: sheetarea, mergedLength: totalMerged };
        }

        /// <summary>
        /// 混合启发式放置：placeParts2
        /// </summary>
        public static SheetPlacement placeParts2(NFP[] sheets, NFP[] parts, SvgNestConfig config, int nestindex)
        {
            parts = parts ?? new NFP[0];
            sheets = sheets ?? new NFP[0];
            var clock = Stopwatch.StartNew();
            int starts = Math.Max(1, Math.Min(32, config.PlaceParts2Starts));
            int beamWidth = Math.Max(1, Math.Min(32, config.PlaceParts2BeamWidth));
            int lookahead = Math.Max(1, Math.Min(8, config.PlaceParts2Lookahead));
            int candidateCount = Math.Max(1, Math.Min(32, config.PlaceParts2Candidates));
            var random = new Random(config.PlaceParts2Seed);
            var weights = parts.Select(p => Math.Max(1e-9, Math.Abs(GeometryUtil.polygonArea(p)))).ToArray();
            var rotated = new Dictionary<int, NFP[]>();
            SheetPlacement best = null;
            for (int start = 0; start < starts; start++)
            {
                if (start > 0 && clock.ElapsedMilliseconds >= Math.Max(0, config.PlaceParts2TimeLimitMs)) break;
                if (start > 0)
                    for (int i = 0; i < weights.Length; i++) weights[i] *= 0.8 + random.NextDouble() * 0.4;
                double weightSum = weights.Sum();
                if (weightSum > 0)
                    for (int i = 0; i < weights.Length; i++) weights[i] /= weightSum;
                var remaining = start == 0 ? Enumerable.Range(0, parts.Length).ToList()
                    : Enumerable.Range(0, parts.Length).OrderByDescending(i => weights[i]).ThenBy(i => i).ToList();
                var allplacements = new List<SheetPlacementItem>();
                double totalsheetarea = 0;
                double sheetarea = 0;
                double fitness = 0;
                double lastArea = 0;
                foreach (var inputSheet in sheets)
                {
                    if (remaining.Count == 0) break;
                    var sheet = ClonePlaceParts2Polygon(inputSheet);
                    sheet.id = inputSheet.id;
                    sheet.source = inputSheet.source;
                    sheet.rotation = inputSheet.rotation;
                    sheetarea = Math.Abs(GeometryUtil.polygonArea(sheet));
                    int sheetPartCount = remaining.Count;
                    var innerNfps = new Dictionary<NFP, NFP[]>();
                    var current = new PlaceParts2State
                    {
                        Geometry = new PlaceParts1BeamState(),
                        Pending = remaining.ToList()
                    };
                    while (current.Pending.Count > 0)
                    {
                        bool greedy = clock.ElapsedMilliseconds >= Math.Max(0, config.PlaceParts2TimeLimitMs);
                        var beam = new List<PlaceParts2State> { current };
                        for (int depth = 0; depth < (greedy ? 1 : lookahead); depth++)
                        {
                            var expanded = new List<PlaceParts2State>();
                            foreach (var state in beam)
                            {
                                if (state.Pending.Count == 0) { expanded.Add(state); continue; }
                                var skipped = new PlaceParts2State
                                {
                                    Geometry = state.Geometry,
                                    Pending = state.Pending.Skip(1).ToList(),
                                    Taken = state.Taken.ToList(),
                                    Area = state.Area
                                };
                                expanded.Add(skipped);
                                foreach (int partIndex in state.Pending.Take(greedy ? 1 : lookahead))
                                {
                                    NFP[] variants;
                                    if (!rotated.TryGetValue(partIndex, out variants))
                                    {
                                        int rotations = Math.Max(1, config.rotations);
                                        variants = new NFP[rotations];
                                        for (int angle = 0; angle < rotations; angle++)
                                        {
                                            float rotation = (parts[partIndex].rotation + angle * (360f / rotations)) % 360f;
                                            var variant = rotatePolygon(parts[partIndex], rotation);
                                            variant.id = parts[partIndex].id;
                                            variant.source = parts[partIndex].source;
                                            variant.rotation = rotation;
                                            variants[angle] = variant;
                                        }
                                        rotated.Add(partIndex, variants);
                                    }
                                    foreach (var variant in variants)
                                    {
                                        greedy = greedy || clock.ElapsedMilliseconds >= Math.Max(0, config.PlaceParts2TimeLimitMs);
                                        NFP[] inner;
                                        if (!innerNfps.TryGetValue(variant, out inner))
                                        {
                                            var bounds = GeometryUtil.getPolygonBounds(variant);
                                            inner = variant.length == 0 || bounds.width > sheet.WidthCalculated + 1e-7
                                                || bounds.height > sheet.HeightCalculated + 1e-7
                                                ? null : getInnerNfp(sheet, variant, 0, config);
                                            innerNfps.Add(variant, inner);
                                        }
                                        var candidates = GetPlaceParts2Candidates(inner, sheet, variant, state.Geometry, config,
                                            greedy ? 1 : candidateCount);
                                        foreach (var candidate in candidates)
                                        {
                                            var next = new PlaceParts2State
                                            {
                                                Geometry = state.Geometry.Extend(variant, candidate.Placement, 0, 0),
                                                Pending = state.Pending.Where(i => i != partIndex).ToList(),
                                                Taken = state.Taken.Concat(new[] { partIndex }).ToList(),
                                                Area = state.Area + Math.Abs(GeometryUtil.polygonArea(variant))
                                            };
                                            expanded.Add(next);
                                        }
                                    }
                                    if (!greedy && clock.ElapsedMilliseconds >= Math.Max(0, config.PlaceParts2TimeLimitMs)) break;
                                }
                            }
                            beam = SelectPlaceParts2Beam(expanded, greedy ? 1 : beamWidth);
                            if (beam.All(s => s.Pending.Count == 0)
                                || clock.ElapsedMilliseconds >= Math.Max(0, config.PlaceParts2TimeLimitMs)) break;
                        }
                        current = beam[0];
                    }
                    remaining.RemoveAll(i => current.Taken.Contains(i));
                    var placed = current.Geometry;
                    if (placed.Placed.Count > 0 && current.Area > 0)
                    {
                        fitness += placed.Width * sheet.HeightCalculated / current.Area * 100;
                        if (placed.Placed.Count == sheetPartCount) lastArea = placed.Width * sheet.HeightCalculated;
                    }
                    if (placed.Placements.Count == 0) continue;
                    totalsheetarea += sheetarea;
                    fitness += sheetarea;
                    allplacements.Add(new SheetPlacementItem
                    {
                        sheetId = sheet.id,
                        sheetSource = sheet.source.Value,
                        sheetplacements = placed.Placements
                    });
                }
                foreach (int i in remaining)
                {
                    fitness += 100000000 * Math.Max(1e-9, Math.Abs(GeometryUtil.polygonArea(parts[i]))) / Math.Max(1e-9, totalsheetarea);
                    weights[i] *= 1.5;
                }
                double boostedSum = weights.Sum();
                if (boostedSum > 0)
                    for (int i = 0; i < weights.Length; i++) weights[i] /= boostedSum;
                if (allplacements.Count > 1) fitness += lastArea;
                if (double.IsNaN(fitness) || double.IsInfinity(fitness)) fitness = double.MaxValue;
                var result = new SheetPlacement
                {
                    placements = new[] { allplacements },
                    fitness = fitness,
                    area = sheetarea,
                    mergedLength = 0
                };
                if (best == null || SheetPlacement.IsBetterThan(result, best)) best = result;
            }
            return best;
        }

        sealed class PlaceParts2State
        {
            public PlaceParts1BeamState Geometry;
            public List<int> Pending;
            public List<int> Taken = new List<int>();
            public double Area;
        }

        static List<PlaceParts2State> SelectPlaceParts2Beam(List<PlaceParts2State> states, int width)
        {
            var ordered = states.OrderByDescending(s => s.Geometry.Placed.Count).ThenByDescending(s => s.Area)
                .ThenBy(s => s.Geometry.Width * s.Geometry.Height).ThenBy(s => s.Geometry.Width)
                .ThenBy(s => s.Geometry.Height);
            var result = new List<PlaceParts2State>();
            var seen = new HashSet<string>();
            foreach (var state in ordered)
            {
                string key = string.Join(";", state.Geometry.Placements.Select(p =>
                    p.id + ":" + p.rotation.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":"
                    + p.x.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ","
                    + p.y.ToString("R", System.Globalization.CultureInfo.InvariantCulture)))
                    + "|" + string.Join(",", state.Pending) + "|" + string.Join(",", state.Taken);
                if (!seen.Add(key)) continue;
                result.Add(state);
                if (result.Count == width) break;
            }
            return result;
        }

        static NFP ClonePlaceParts2Polygon(NFP polygon)
        {
            var copy = clone(polygon);
            if (polygon.children != null)
                copy.children = polygon.children.Select(ClonePlaceParts2Polygon).ToList();
            return copy;
        }
        sealed class PlaceParts2Candidate
        {
            public PlacementItem Placement;
            public double Width;
            public double Height;
            public double Score;
        }

        static List<PlaceParts2Candidate> GetPlaceParts2Candidates(
            NFP[] sheetNfp,
            NFP sheet,
            NFP part,
            PlaceParts1BeamState state,
            SvgNestConfig config,
            int maxCandidates)
        {
            var candidates = new List<PlaceParts2Candidate>();
            if (sheetNfp == null || sheetNfp.Length == 0 || part.length == 0) return candidates;
            var feasible = sheetNfp;
            if (state.Placed.Count > 0)
            {
                var clipper = new ClipperLib.Clipper();
                for (int i = 0; i < state.Placed.Count; i++)
                {
                    var outerNfp = getOuterNfp(state.Placed[i], part, 0);
                    if (outerNfp == null) return candidates;
                    outerNfp = shiftPolygon(ClonePlaceParts2Polygon(outerNfp), state.Placements[i]);
                    var paths = nfpToClipperCoordinates(outerNfp, config.clipperScale);
                    clipper.AddPaths(paths.Select(p => p.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
                }
                var combined = new List<List<IntPoint>>();
                if (!clipper.Execute(ClipperLib.ClipType.ctUnion, combined, ClipperLib.PolyFillType.pftNonZero, ClipperLib.PolyFillType.pftNonZero))
                    return candidates;
                clipper = new ClipperLib.Clipper();
                clipper.AddPaths(combined, ClipperLib.PolyType.ptClip, true);
                var inner = innerNfpToClipperCoordinates(sheetNfp, config);
                clipper.AddPaths(inner.Select(p => p.ToList()).ToList(), ClipperLib.PolyType.ptSubject, true);
                var difference = new List<List<IntPoint>>();
                if (!clipper.Execute(ClipperLib.ClipType.ctDifference, difference, ClipperLib.PolyFillType.pftEvenOdd, ClipperLib.PolyFillType.pftNonZero))
                    return candidates;
                feasible = difference.Select(p => toNestCoordinates(p.ToArray(), config.clipperScale)).ToArray();
            }

            var bounds = GeometryUtil.getPolygonBounds(part);
            var sheetBounds = GeometryUtil.getPolygonBounds(sheet);
            double sheetWidth = Math.Max(1e-9, sheet.WidthCalculated);
            var seen = new HashSet<string>();
            int stride = Math.Max(1, (int)Math.Ceiling(feasible.Sum(p => (double)p.length) / 256));
            int vertex = 0;
            foreach (var path in feasible)
            {
                for (int i = 0; i < path.length; i++)
                {
                    if (vertex++ % stride != 0) continue;
                    double x = path[i].x - part[0].x;
                    double y = path[i].y - part[0].y;
                    string key = Math.Round(x * 10000).ToString() + "," + Math.Round(y * 10000).ToString();
                    if (!seen.Add(key)) continue;
                    double minX = bounds.x + x;
                    double minY = bounds.y + y;
                    double maxX = minX + bounds.width;
                    double maxY = minY + bounds.height;
                    if (minX < sheetBounds.x - 1e-7 || minY < sheetBounds.y - 1e-7
                        || maxX > sheetBounds.x + sheetBounds.width + 1e-7
                        || maxY > sheetBounds.y + sheetBounds.height + 1e-7) continue;
                    if (state.Placed.Count > 0)
                    {
                        minX = Math.Min(minX, state.MinX);
                        minY = Math.Min(minY, state.MinY);
                        maxX = Math.Max(maxX, state.MaxX);
                        maxY = Math.Max(maxY, state.MaxY);
                    }
                    var placement = new PlacementItem { id = part.id, source = part.source.Value, rotation = part.rotation, x = x, y = y };
                    double width = maxX - minX;
                    double score = width / sheetWidth;
                    candidates.Add(new PlaceParts2Candidate { Placement = placement, Width = width, Height = maxY - minY, Score = score });
                }
            }
            candidates.Sort((left, right) =>
            {
                int compare = left.Score.CompareTo(right.Score);
                if (compare != 0) return compare;
                compare = left.Width.CompareTo(right.Width);
                if (compare != 0) return compare;
                compare = left.Height.CompareTo(right.Height);
                if (compare != 0) return compare;
                compare = left.Placement.x.CompareTo(right.Placement.x);
                return compare != 0 ? compare : left.Placement.y.CompareTo(right.Placement.y);
            });
            if (candidates.Count > maxCandidates) candidates.RemoveRange(maxCandidates, candidates.Count - maxCandidates);
            return candidates;
        }



        // jsClipper uses X/Y instead of x/y...
        public DataInfo data;
        NFP[] parts;

        int index;
        // run the placement synchronously


        public static windowUnk window = new windowUnk();

        public Action<SheetPlacement> ResponseAction;

        public static long LastPlacePartTime = 0;
        public void sync()

        {
            //console.log('starting synchronous calculations', Object.keys(window.nfpCache).length);
            //console.log('in sync');
            var c = 0;
            foreach (var key in window.nfpCache)
            {
                c++;
            }
            //console.log('nfp cached:', c);
            Stopwatch sw = Stopwatch.StartNew();

            //var placement = placeParts(data.sheets.ToArray(), parts, data.config, index);
            //var placement1 = placeParts1(data.sheets.ToArray(), parts, data.config, index);
            //var placement2 = data.config.EnablePlaceParts2
            //    ? placeParts2(data.sheets.ToArray(), parts, data.config, index) : null;
            //foreach (var candidate in new[] { placement1, placement2 })
            //{
            //    if (SheetPlacement.IsBetterThan(candidate, placement))
            //    {
            //        placement = candidate;
            //    }
            //}

            var placement = placeParts2(data.sheets.ToArray(), parts, data.config, index);

            sw.Stop();
            LastPlacePartTime = sw.ElapsedMilliseconds;

            placement.index = data.index;
            ResponseAction(placement);
            //ipcRenderer.send('background-response', placement);
        }
        public void BackgroundStart(DataInfo data)
        {
            this.data = data;
            var index = data.index;
            var individual = data.individual;

            var parts = individual.placements;
            var rotations = individual.Rotation;
            var ids = data.ids;
            var sources = data.sources;
            var children = data.children;

            for (var i = 0; i < parts.Count; i++)
            {
                parts[i].rotation = rotations[i];
                parts[i].id = ids[i];
                parts[i].source = sources[i];
                if (!data.config.simplify)
                {
                    parts[i].children = children[i];
                }
            }

            for (int i = 0; i < data.sheets.Count; i++)
            {
                data.sheets[i].id = data.sheetids[i];
                data.sheets[i].source = data.sheetsources[i];
                data.sheets[i].children = data.sheetchildren[i];
            }

            // preprocess
            List<NfpPair> pairs = new List<NfpPair>();

            if (Background.UseParallel)
            {
                object lobj = new object();
                Parallel.For(0, parts.Count, i =>
                {
                    {
                        var B = parts[i];
                        for (var j = 0; j < i; j++)
                        {
                            var A = parts[j];
                            var key = new NfpPair()
                            {
                                A = A,
                                B = B,
                                ARotation = A.rotation,
                                BRotation = B.rotation,
                                Asource = A.source.Value,
                                Bsource = B.source.Value

                            };
                            var doc = new DbCacheKey()
                            {
                                A = A.source.Value,
                                B = B.source.Value,

                                ARotation = A.rotation,
                                BRotation = B.rotation

                            };
                            lock (lobj)
                            {
                                if (!inpairs(key, pairs.ToArray()) && !window.db.has(doc))
                                {
                                    pairs.Add(key);
                                }
                            }
                        }
                    }
                });


            }
            else
            {
                for (var i = 0; i < parts.Count; i++)
                {
                    var B = parts[i];
                    for (var j = 0; j < i; j++)
                    {
                        var A = parts[j];
                        var key = new NfpPair()
                        {
                            A = A,
                            B = B,
                            ARotation = A.rotation,
                            BRotation = B.rotation,
                            Asource = A.source.Value,
                            Bsource = B.source.Value

                        };
                        var doc = new DbCacheKey()
                        {
                            A = A.source.Value,
                            B = B.source.Value,

                            ARotation = A.rotation,
                            BRotation = B.rotation

                        };
                        if (!inpairs(key, pairs.ToArray()) && !window.db.has(doc))
                        {
                            pairs.Add(key);
                        }
                    }
                }
            }

            //console.log('pairs: ', pairs.length);
            //console.time('Total');

            this.parts = parts.ToArray();
            if (pairs.Count > 0)
            {

                var ret1 = pmapDeepNest(pairs);
                thenDeepNest(ret1, parts);
            }
            else
            {
                sync();
            }
        }
        public NFP getPart(int source, List<NFP> parts)
        {
            for (var k = 0; k < parts.Count; k++)
            {
                if (parts[k].source == source)
                {
                    return parts[k];
                }
            }
            return null;
        }

        public void thenIterate(NfpPair processed, List<NFP> parts)
        {

            // returned data only contains outer nfp, we have to account for any holes separately in the synchronous portion
            // this is because the c++ addon which can process interior nfps cannot run in the worker thread					
            var A = getPart(processed.Asource, parts);
            var B = getPart(processed.Bsource, parts);

            List<NFP> Achildren = new List<NFP>();


            if (A.children != null)
            {
                for (int j = 0; j < A.children.Count; j++)
                {
                    Achildren.Add(rotatePolygon(A.children[j], processed.ARotation));
                }
            }

            if (Achildren.Count > 0)
            {
                var Brotated = rotatePolygon(B, processed.BRotation);
                var bbounds = GeometryUtil.getPolygonBounds(Brotated);
                List<NFP> cnfp = new List<NFP>();

                for (int j = 0; j < Achildren.Count; j++)
                {
                    var cbounds = GeometryUtil.getPolygonBounds(Achildren[j]);
                    if (cbounds.width > bbounds.width && cbounds.height > bbounds.height)
                    {
                        var n = getInnerNfp(Achildren[j], Brotated, 1, data.config);
                        if (n != null && n.Count() > 0)
                        {
                            cnfp.AddRange(n);
                        }
                    }
                }

                if (processed.nfp.children != null)
                {
                    processed.nfp.children.AddRange(cnfp);
                }
                else
                {
                    processed.nfp.children = cnfp;
                }
            }
            DbCacheKey doc = new DbCacheKey()
            {
                A = processed.Asource,
                B = processed.Bsource,
                ARotation = processed.ARotation,
                BRotation = processed.BRotation,
                nfp = new[] { processed.nfp }//尝试把这个也变成内不路径
            };

            /*var doc = {
                    A: processed[i].Asource,
                    B: processed[i].Bsource,
                    Arotation: processed[i].Arotation,
                    Brotation: processed[i].Brotation,
                    nfp: processed[i].nfp

                };*/

            window.db.insert(doc);
        }

        public static Action<float> displayProgress;
        public static void DisplayProgress(float p)
        {
            if (displayProgress != null)
            {
                displayProgress(p);
            }
        }
        public void thenDeepNest(NfpPair[] processed, List<NFP> parts)
        {
            int cnt = 0;
            if (UseParallel)
            {
                Parallel.For(0, processed.Count(), (i) =>
                {
                    float progress = 0.33f + 0.33f * (cnt / (float)processed.Count());
                    cnt++;
                    DisplayProgress(progress);
                    thenIterate(processed[i], parts);
                });

            }
            else
            {
                for (var i = 0; i < processed.Count(); i++)
                {
                    float progress = 0.33f + 0.33f * (cnt / (float)processed.Count());
                    cnt++;
                    DisplayProgress(progress);
                    thenIterate(processed[i], parts);
                }
            }

            //console.timeEnd('Total');
            //console.log('before sync');
            sync();
        }

        public bool inpairs(NfpPair key, NfpPair[] p)
        {
            for (var i = 0; i < p.Length; i++)
            {
                if (p[i].Asource == key.Asource && p[i].Bsource == key.Bsource && p[i].ARotation == key.ARotation && p[i].BRotation == key.BRotation)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool UseParallel = false;
        public NfpPair[] pmapDeepNest(List<NfpPair> pairs)
        {
            NfpPair[] ret = new NfpPair[pairs.Count()];
            int cnt = 0;
            if (UseParallel)
            {
                Parallel.For(0, pairs.Count, (i) =>
                {
                    ret[i] = process(pairs[i]);
                    float progress = 0.33f * (cnt / (float)pairs.Count);
                    cnt++;
                    DisplayProgress(progress);
                });
            }
            else
            {
                for (int i = 0; i < pairs.Count; i++)
                {
                    var item = pairs[i];
                    ret[i] = process(item);
                    float progress = 0.33f * (cnt / (float)pairs.Count);
                    cnt++;
                    DisplayProgress(progress);
                }
            }
            return ret.ToArray();
        }
        public NfpPair process(NfpPair pair)
        {
            var A = rotatePolygon(pair.A, pair.ARotation);
            var B = rotatePolygon(pair.B, pair.BRotation);

            var Ac = _Clipper.ScaleUpPaths(A, 10000000);
            var Bc = _Clipper.ScaleUpPaths(B, 10000000);
            for (var i = 0; i < Bc.Length; i++)
            {
                Bc[i].X *= -1;
                Bc[i].Y *= -1;
            }

            var solution = ClipperLib.Clipper.MinkowskiSum(new List<IntPoint>(Ac), new List<IntPoint>(Bc), true);
            NFP clipperNfp = null;
            #region test
            //if (solution.Count > 1)
            //{
            //    using (System.IO.StreamWriter writer = new System.IO.StreamWriter("..\\2.plt"))
            //    {
            //        foreach (var ps in solution)
            //        {
            //            if (ps.Count < 2)
            //                continue;
            //            writer.WriteLine($"PU{ps[0].X / 250000},{ps[0].Y / 250000};");
            //            for (int i = 1; i < ps.Count; i++)
            //            {
            //                writer.WriteLine($"PD{ps[i].X / 250000},{ps[i].Y / 250000};");
            //            }
            //        }
            //        writer.WriteLine($"PU{Ac[0].X / 250000},{Ac[0].Y / 250000};");
            //        for (int i = 1; i < Ac.Length; i++)
            //        {
            //            writer.WriteLine($"PD{Ac[i].X / 250000},{Ac[i].Y / 250000};");
            //        }
            //        writer.WriteLine($"PU{Bc[0].X / 250000},{Bc[0].Y / 250000};");
            //        for (int i = 1; i < Bc.Length; i++)
            //        {
            //            writer.WriteLine($"PD{Bc[i].X / 250000},{Bc[i].Y / 250000};");
            //        }
            //    }
            //}

            #endregion
            double? largestArea = null;
            int indexofouter = -1;
            for (int i = 0; i < solution.Count(); i++)
            {
                var n = toNestCoordinates(solution[i].ToArray(), 10000000);
                var sarea = -GeometryUtil.polygonArea(n);
                if (largestArea == null || largestArea < sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                    indexofouter = i;
                }
            }
            
            if (indexofouter != -1)
            {
                for (int i = 0; i < solution.Count(); i++)
                {
                    if (i == indexofouter)
                        continue;
                    if (clipperNfp.children == null)
                        clipperNfp.children = new List<NFP>();

                    var n = toNestCoordinates(solution[i].ToArray(), 10000000);
                    clipperNfp.children.Add(n);
                }
            }

            if (clipperNfp.children != null)
            {
                List<NFP> childs = new List<NFP>();
                foreach (var nfp in clipperNfp.children)
                {
                    bool inner = false;
                    for (var i = 0; i < nfp.length; i++)
                    {
                        nfp[i].x += B[0].x;
                        nfp[i].y += B[0].y;
                        //判断是否是内部nfp路径
                        if (IsVisible(nfp[i], A))
                        {
                            inner = true;
                            break;
                        }
                    }
                    if (!inner)
                    {
                        //判断面积是否为0
                        double area = GeometryUtil.polygonArea(nfp);
                        if(area > 0.01)
                        {
                            childs.Add(nfp);
                        }
                        
                    }
                }
                if (childs.Count > 0)
                {
                    clipperNfp.children = childs;
                }
                else
                {
                    clipperNfp.children = null;
                }
            }

            for (var i = 0; i < clipperNfp.length; i++)
            {
                clipperNfp[i].x += B[0].x;
                clipperNfp[i].y += B[0].y;
            }
            #region test
            //using (System.IO.StreamWriter writer = new System.IO.StreamWriter("..\\2.plt"))
            //{
            //    foreach (var ps in solution)
            //    {
            //        if (ps.Count < 2)
            //            continue;
            //        writer.WriteLine($"PU{ps[0].X / 250000 + B[0].x * 40},{ps[0].Y / 250000 + B[0].y * 40};");
            //        for (int i = 1; i < ps.Count; i++)
            //        {
            //            writer.WriteLine($"PD{ps[i].X / 250000 + B[0].x * 40},{ps[i].Y / 250000 + B[0].y * 40};");
            //        }
            //    }
            //    writer.WriteLine($"PU{Ac[0].X / 250000},{Ac[0].Y / 250000};");
            //    for (int i = 1; i < Ac.Length; i++)
            //    {
            //        writer.WriteLine($"PD{Ac[i].X / 250000},{Ac[i].Y / 250000};");
            //    }
            //    writer.WriteLine($"PU{Bc[0].X / 250000},{Bc[0].Y / 250000};");
            //    for (int i = 1; i < Bc.Length; i++)
            //    {
            //        writer.WriteLine($"PD{Bc[i].X / 250000},{Bc[i].Y / 250000};");
            //    }
            //}

            #endregion

            //return new SvgNestPort.NFP[] { new SvgNestPort.NFP() { Points = clipperNfp.Points } };

            pair.A = null;
            pair.B = null;
            pair.nfp = clipperNfp;
            return pair;
        }
        private bool IsVisible(SvgPoint p, NFP points)
        {
            int i, j = points.length - 1;
            bool isVisible = false;
            for (i = 0; i < points.length; i++)
            {
                if (points[i].y < p.y && points[j].y >= p.y
                    || points[j].y < p.y && points[i].y >= p.y)
                {
                    if (points[i].x + (p.y - points[i].y) / (points[j].y - points[i].y)
                        * (points[j].x - points[i].x) < p.x)
                    {
                        isVisible = !isVisible;
                    }
                }
                j = i;
            }
            return isVisible;
        }

        public static NFP toNestCoordinates(IntPoint[] polygon, double scale)
        {
            var clone = new List<SvgPoint>();

            for (var i = 0; i < polygon.Count(); i++)
            {
                clone.Add(new SvgPoint(
                     polygon[i].X / scale,
                             polygon[i].Y / scale
                        ));
            }
            return new NFP() { Points = clone.ToArray() };
        }

        public static NFP getHull(NFP polygon)
        {
            // convert to hulljs format
            /*var hull = new ConvexHullGrahamScan();
            for(var i=0; i<polygon.length; i++){
                hull.addPoint(polygon[i].x, polygon[i].y);
            }

            return hull.getHull();*/
            double[][] points = new double[polygon.length][];
            for (var i = 0; i < polygon.length; i++)
            {
                points[i] = (new double[] { polygon[i].x, polygon[i].y });
            }

            var hullpoints = D3.polygonHull(points);

            if (hullpoints == null)
            {
                return polygon;
            }

            NFP hull = new NFP();
            for (int i = 0; i < hullpoints.Count(); i++)
            {
                hull.AddPoint(new SvgPoint(hullpoints[i][0], hullpoints[i][1]));
            }
            return hull;
        }


        // returns clipper nfp. Remember that clipper nfp are a list of polygons, not a tree!
        public static IntPoint[][] nfpToClipperCoordinates(NFP nfp, double clipperScale = 10000000)
        {

            List<IntPoint[]> clipperNfp = new List<IntPoint[]>();

            // children first
            if (nfp.children != null && nfp.children.Count > 0)
            {
                for (var j = 0; j < nfp.children.Count; j++)
                {
                    if (GeometryUtil.polygonArea(nfp.children[j]) < 0)
                    {
                        nfp.children[j].reverse();
                    }
                    //var childNfp = SvgNest.toClipperCoordinates(nfp.children[j]);
                    var childNfp = _Clipper.ScaleUpPaths(nfp.children[j], clipperScale);
                    clipperNfp.Add(childNfp);
                }
            }

            if (GeometryUtil.polygonArea(nfp) > 0)
            {
                nfp.reverse();
            }


            //var outerNfp = SvgNest.toClipperCoordinates(nfp);

            // clipper js defines holes based on orientation

            var outerNfp = _Clipper.ScaleUpPaths(nfp, clipperScale);

            //var cleaned = ClipperLib.Clipper.CleanPolygon(outerNfp, 0.00001*config.clipperScale);

            clipperNfp.Add(outerNfp);
            //var area = Math.abs(ClipperLib.Clipper.Area(cleaned));

            return clipperNfp.ToArray();
        }
        // inner nfps can be an array of nfps, outer nfps are always singular
        public static IntPoint[][] innerNfpToClipperCoordinates(NFP[] nfp, SvgNestConfig config)
        {
            List<IntPoint[]> clipperNfp = new List<IntPoint[]>();
            for (var i = 0; i < nfp.Count(); i++)
            {
                var clip = nfpToClipperCoordinates(nfp[i], config.clipperScale);
                clipperNfp.AddRange(clip);
                //clipperNfp = clipperNfp.Concat(new[] { clip }).ToList();
            }

            return clipperNfp.ToArray();
        }

        static object lockobj = new object();

        public static NFP[] NewMinkowskiSum(NFP pattern, NFP path, int type, bool useChilds = false, bool takeOnlyBiggestArea = true)
        {
            var key = pattern.source + ";" + path.source + ";" + pattern.rotation + ";" + path.rotation;
            bool cacheAllow = type != 1;
            if (cacheProcess.ContainsKey(key) && cacheAllow)
            {
                return cacheProcess[key];
            }

            var ac = _Clipper.ScaleUpPaths(pattern, 10000000);
            List<List<IntPoint>> solution = null;
            if (useChilds)
            {
                var bc = Background.nfpToClipperCoordinates(path, 10000000);
                for (var i = 0; i < bc.Length; i++)
                {
                    for (int j = 0; j < bc[i].Length; j++)
                    {
                        bc[i][j].X *= -1;
                        bc[i][j].Y *= -1;
                    }
                }

                solution = ClipperLib.Clipper.MinkowskiSum(new List<IntPoint>(ac), new List<List<IntPoint>>(bc.Select(z => z.ToList())), true);
            }
            else
            {
                var bc = _Clipper.ScaleUpPaths(path, 10000000);
                for (var i = 0; i < bc.Length; i++)
                {
                    bc[i].X *= -1;
                    bc[i].Y *= -1;
                }
                solution = Clipper.MinkowskiSum(new List<IntPoint>(ac), new List<IntPoint>(bc), true);
            }
            NFP clipperNfp = null;

            double? largestArea = null;
            int largestIndex = -1;

            for (int i = 0; i < solution.Count(); i++)
            {
                var n = toNestCoordinates(solution[i].ToArray(), 10000000);
                var sarea = Math.Abs(GeometryUtil.polygonArea(n));
                if (largestArea == null || largestArea < sarea)
                {
                    clipperNfp = n;
                    largestArea = sarea;
                    largestIndex = i;
                }
            }
            if (!takeOnlyBiggestArea)
            {
                for (int j = 0; j < solution.Count; j++)
                {
                    if (j == largestIndex) continue;
                    var n = toNestCoordinates(solution[j].ToArray(), 10000000);
                    if (clipperNfp.children == null)
                        clipperNfp.children = new List<NFP>();
                    clipperNfp.children.Add(n);
                }
            }
            for (var i = 0; i < clipperNfp.Length; i++)
            {

                clipperNfp[i].x *= -1;
                clipperNfp[i].y *= -1;
                clipperNfp[i].x += pattern[0].x;
                clipperNfp[i].y += pattern[0].y;

            }
            if (clipperNfp.children != null)
                foreach (var nFP in clipperNfp.children)
                {
                    for (int j = 0; j < nFP.Length; j++)
                    {

                        nFP.Points[j].x *= -1;
                        nFP.Points[j].y *= -1;
                        nFP.Points[j].x += pattern[0].x;
                        nFP.Points[j].y += pattern[0].y;
                    }
                }
            var res = new[] { clipperNfp };
            if (cacheAllow)
            {
                cacheProcess.Add(key, res);
            }
            return res;
        }
        public static void ExecuteSTA(Action act)
        {
            if (!Debugger.IsAttached) return;
            Thread thread = new Thread(() => { act(); });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }

        public static bool UseExternalDll = false;
        public static NFP getOuterNfp(NFP A, NFP B, int type, bool inside = false)//todo:?inside def?
        {
            NFP[] nfp = null;


            var key = new DbCacheKey()
            {
                A = A.source,
                B = B.source,
                ARotation = A.rotation,
                BRotation = B.rotation,
                //Type = type
            };

            var doc = window.db.find(key);
            if (doc != null)
            {
                return doc.First();
            }

            /*

            // try the file cache if the calculation will take a long time
            var doc = window.db.find({ A: A.source, B: B.source, Arotation: A.rotation, Brotation: B.rotation });

            if (doc)
            {
                return doc;
            }*/


            // not found in cache           
            if (inside || (A.children != null && A.children.Count > 0))
            {
                lock (lockobj)
                {
                    if (UseExternalDll)
                    {
                        nfp = Process2(A, B, type);

                    }
                    else
                    {
                        nfp = NewMinkowskiSum(B, A, type, true, takeOnlyBiggestArea: false);
                    }
                }
            }
            else
            {
                var Ac = _Clipper.ScaleUpPaths(A, 10000000);
                var aac = Clipper.SimplifyPolygon(Ac.ToList(), PolyFillType.pftNonZero);

                var Bc = _Clipper.ScaleUpPaths(B, 10000000);
                for (var i = 0; i < Bc.Length; i++)
                {
                    Bc[i].X *= -1;
                    Bc[i].Y *= -1;
                }
                var solution = ClipperLib.Clipper.MinkowskiSum(new List<IntPoint>(Ac), new List<IntPoint>(Bc), true);
                NFP clipperNfp = null;

                double? largestArea = null;
                for (int i = 0; i < solution.Count(); i++)
                {
                    var n = Background.toNestCoordinates(solution[i].ToArray(), 10000000);
                    var sarea = GeometryUtil.polygonArea(n);
                    if (largestArea == null || largestArea > sarea)
                    {
                        clipperNfp = n;
                        largestArea = sarea;
                    }
                }

                for (var i = 0; i < clipperNfp.length; i++)
                {
                    clipperNfp[i].x += B[0].x;
                    clipperNfp[i].y += B[0].y;
                }
                nfp = new NFP[] { new NFP() { Points = clipperNfp.Points } };


            }

            if (nfp == null || nfp.Length == 0)
            {
                //console.log('holy shit', nfp, A, B, JSON.stringify(A), JSON.stringify(B));
                return null;
            }

            NFP nfps = nfp.First();
            /*
            nfp = nfp.pop();
            */
            if (nfps == null || nfps.Length == 0)
            {
                return null;
            }
            /*
            if (!nfp || nfp.length == 0)
            {
                return null;
            }
            */
            if (!inside && A.source != null && B.source != null)
            {
                var doc2 = new DbCacheKey()
                {
                    A = A.source.Value,
                    B = B.source.Value,
                    ARotation = A.rotation,
                    BRotation = B.rotation,
                    nfp = nfp
                };
                window.db.insert(doc2);


            }
            /*
            if (!inside && typeof A.source !== 'undefined' && typeof B.source !== 'undefined')
            {
                // insert into db
                doc = {
                    A: A.source,
            B: B.source,
            Arotation: A.rotation,
            Brotation: B.rotation,
            nfp: nfp

        };
                window.db.insert(doc);
            }
            */
            return nfps;

        }

        #region
        /// <summary>
        /// 计算两个形状在当前相对位置下的贴合度 (0~1，越高越贴合)
        /// </summary>
        public static double CalculatePairFitness(NFP shapeA, NFP shapeB,
                                                  PlacementItem posA, PlacementItem posB)
        {
            // 获取 B 相对 A 的偏移
            double offsetX = posB.x - posA.x;
            double offsetY = posB.y - posA.y;

            // 计算 NFP（A 静止，B 移动）
            var nfp = Background.getOuterNfp(shapeA, shapeB, 0);
            if (nfp == null) return 0.0;

            // 寻找 NFP 边界上距离当前偏移最近的点
            double minDist = double.MaxValue;
            foreach (var pt in nfp.Points)
            {
                double dx = pt.x - offsetX;
                double dy = pt.y - offsetY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) minDist = dist;
            }

            // 贴合度：距离越小越好，使用 1/(1+dist) 映射到 (0,1]
            return 1.0 / (1.0 + minDist);
        }
        private static double CalculateLayoutFitness(PlacementItem candidate, NFP candidateShape, List<PlacementItem> allPlacements,
                                      List<NFP> allShapes)
        {
            var bounds = GeometryUtil.getPolygonBounds(candidateShape);
            // 将包围盒平移到候选位置
            bounds.x += candidate.x;
            bounds.y += candidate.y;

            var nearbyIndices = GetNearbyParts(bounds, allPlacements, allShapes, 1);
            
            if (nearbyIndices.Count == 0) return 0.0;

            double totalFitness = 0;
            int count = 0;
            foreach (int idx in nearbyIndices)
            {
                // 计算与邻居的贴合度
                double fit = CalculatePairFitness(allShapes[idx], candidateShape,
                                                  allPlacements[idx], candidate);
                totalFitness += fit;
                count++;
            }
            return totalFitness / count; // 平均贴合度
        }
        /// <summary>
        /// 从已放置零件列表中筛选出与候选零件包围盒相交或非常接近的零件索引。
        /// </summary>
        /// <param name="candidateBounds">候选零件的包围盒（已在绝对坐标系中）</param>
        /// <param name="allPlacements">所有已放置零件的位置</param>
        /// <param name="allShapes">所有已放置零件的形状</param>
        /// <param name="margin">扩展距离，一般取零件平均尺寸的 0.5～1 倍</param>
        /// <returns>邻近零件的索引列表</returns>
        private static List<int> GetNearbyParts(PolygonBounds candidateBounds,
                                          List<PlacementItem> allPlacements,
                                          List<NFP> allShapes,
                                          double margin)
        {
            var nearby = new List<int>();
            for (int i = 0; i < allPlacements.Count; i++)
            {
                var bounds = GeometryUtil.getPolygonBounds(allShapes[i]);
                bounds.x += allPlacements[i].x;
                bounds.y += allPlacements[i].y;

                // 检查包围盒是否在扩展范围内相交
                if (bounds.x + bounds.width >= candidateBounds.x - margin &&
                    bounds.x <= candidateBounds.x + candidateBounds.width + margin &&
                    bounds.y + bounds.height >= candidateBounds.y - margin &&
                    bounds.y <= candidateBounds.y + candidateBounds.height + margin)
                {
                    nearby.Add(i);
                }
            }
            return nearby;
        }
        #endregion
    }
    public class ClipCacheItem
    {
        public int index;
        public IntPoint[][] nfpp;
    }

    public class dbCache
    {
        public dbCache(windowUnk w)
        {
            window = w;
        }
        public bool has(DbCacheKey obj)
        {
            lock (lockobj)
            {
                var key = getKey(obj);
                //var key = "A" + obj.A + "B" + obj.B + "Arot" + (int)Math.Round(obj.ARotation)                + "Brot" + (int)Math.Round(obj.BRotation);
                if (window.nfpCache.ContainsKey(key))
                {
                    return true;
                }
                return false;
            }

        }

        public windowUnk window;
        public object lockobj = new object();

        string getKey(DbCacheKey obj)
        {
            var key = "A" + obj.A + "B" + obj.B + "Arot" + (int)Math.Round(obj.ARotation * 10000) + "Brot" + (int)Math.Round((obj.BRotation * 10000)) + ";" + obj.Type;
            return key;
        }
        internal void insert(DbCacheKey obj, bool inner = false)
        {

            var key = getKey(obj);
            //if (window.performance.memory.totalJSHeapSize < 0.8 * window.performance.memory.jsHeapSizeLimit)
            {
                lock (lockobj)
                {
                    if (!window.nfpCache.ContainsKey(key))
                    {
                        window.nfpCache.Add(key, Background.cloneNfp(obj.nfp, inner).ToList());
                    }
                    else
                    {
                        //throw new Exception("trouble .cache allready has such key");
                        window.nfpCache[key] = Background.cloneNfp(obj.nfp, inner).ToList();
                    }
                }
                //console.log('cached: ',window.cache[key].poly);
                //console.log('using', window.performance.memory.totalJSHeapSize/window.performance.memory.jsHeapSizeLimit);
            }
        }
        public NFP[] find(DbCacheKey obj, bool inner = false)
        {
            lock (lockobj)
            {
                var key = getKey(obj);
                //var key = "A" + obj.A + "B" + obj.B + "Arot" + (int)Math.Round(obj.ARotation) + "Brot" + (int)Math.Round((obj.BRotation));

                //console.log('key: ', key);
                if (window.nfpCache.ContainsKey(key))
                {
                    return Background.cloneNfp(window.nfpCache[key].ToArray(), inner);
                }

                return null;
            }
        }

    }
    public class windowUnk
    {
        public windowUnk()
        {
            db = new dbCache(this);
        }
        public Dictionary<string, List<NFP>> nfpCache = new Dictionary<string, List<NFP>>();
        public dbCache db;
    }
}
