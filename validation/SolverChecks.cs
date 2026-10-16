using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DeepNestLib;
using ClipperLib;

internal static class SolverChecks
{
    static int checks;
    const double Scale = 1000000;
    static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
        checks++;
    }
    static NFP Ring(int id, int source, params double[] xy)
    {
        return new NFP { id = id, source = source, Points = Enumerable.Range(0, xy.Length / 2)
            .Select(i => new SvgPoint(xy[2 * i], xy[2 * i + 1])).ToArray() };
    }
    static NFP Rect(int id, int source, double x, double y, double w, double h)
    {
        return Ring(id, source, x,y,x+w,y,x+w,y+h,x,y+h);
    }
    static List<List<IntPoint>> Solid(NFP polygon, double angle = 0, double dx = 0, double dy = 0)
    {
        double c = Math.Cos(angle * Math.PI / 180), s = Math.Sin(angle * Math.PI / 180);
        var result = new List<List<IntPoint>>();
        Action<NFP> append = null;
        append = p => {
            result.Add(p.Points.Select(v => new IntPoint(
                (long)Math.Round((v.x*c-v.y*s+dx)*Scale),
                (long)Math.Round((v.x*s+v.y*c+dy)*Scale))).ToList());
            if (p.children != null) foreach (var child in p.children) append(child);
        };
        append(polygon);
        return result;
    }
    static double Area(List<List<IntPoint>> first, List<List<IntPoint>> second, ClipType operation)
    {
        var clipper = new Clipper();
        clipper.AddPaths(first, PolyType.ptSubject, true);
        clipper.AddPaths(second, PolyType.ptClip, true);
        var output = new List<List<IntPoint>>();
        Check(clipper.Execute(operation, output, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd), "boolean operation");
        return Math.Abs(output.Sum(p => Clipper.Area(p))) / (Scale * Scale);
    }
    static string Snapshot(NFP p)
    {
        return p.id+":"+p.source+":"+p.rotation+":"+string.Join(";",p.Points.Select(v=>v.x+","+v.y))
            + (p.children == null ? "" : "["+string.Join("|",p.children.Select(Snapshot))+"]");
    }
    static string Layout(SheetPlacement result)
    {
        return string.Join(";",result.placements.SelectMany(g=>g).SelectMany(s=>s.sheetplacements.Select(p=>
            s.sheetId+":"+p.id+":"+p.rotation+":"+p.x+":"+p.y)).OrderBy(s=>s));
    }
    static void Reset()
    {
        Background.UseParallel = false;
        Background.UseExternalDll = false;
        Background.window = new windowUnk();
        Background.cacheProcess = new Dictionary<string,NFP[]>();
    }
    static SvgNestConfig Config()
    {
        return new SvgNestConfig { rotations=4, placementType=PlacementTypeEnum.gravity,
            PlaceParts2Starts=3, PlaceParts2BeamWidth=3, PlaceParts2Lookahead=3,
            PlaceParts2Candidates=3, PlaceParts2TimeLimitMs=10000, PlaceParts2Seed=1 };
    }
    static int Validate(string name, SheetPlacement result, NFP[] sheets, NFP[] parts)
    {
        Check(result!=null && result.placements!=null, name+" result");
        Check(result.fitness.HasValue && !double.IsNaN(result.fitness.Value) && !double.IsInfinity(result.fitness.Value), name+" fitness");
        var ids = new HashSet<int>();
        foreach(var group in result.placements)
        foreach(var item in group)
        {
            var sheet = sheets.Single(s=>s.id==item.sheetId);
            var placed = new List<List<List<IntPoint>>>();
            foreach(var p in item.sheetplacements)
            {
                Check(ids.Add(p.id), name+" unique ID");
                Check(!double.IsNaN(p.x) && !double.IsInfinity(p.x) && !double.IsNaN(p.y) && !double.IsInfinity(p.y), name+" transform");
                var original = parts.Single(v=>v.id==p.id);
                Check(Math.Abs((p.rotation-original.rotation)/90-Math.Round((p.rotation-original.rotation)/90))<1e-6, name+" angle");
                var shape=Solid(original,p.rotation,p.x,p.y);
                Check(Area(shape,Solid(sheet),ClipType.ctDifference)<1e-4,name+" containment");
                foreach(var other in placed) Check(Area(shape,other,ClipType.ctIntersection)<1e-4,name+" overlap");
                placed.Add(shape);
            }
        }
        Console.WriteLine(name+": placed="+ids.Count+" sheets="+result.placements.SelectMany(g=>g).Count(s=>s.sheetplacements.Count>0)+" fitness="+result.fitness);
        return ids.Count;
    }
    static void Case(string name, NFP[] sheets, NFP[] parts, int expected)
    {
        Reset();
        var config=Config(); SvgNest.Config=config;
        string before=string.Join("|",sheets.Concat(parts).Select(Snapshot));
        var watch=Stopwatch.StartNew();
        var result=Background.placeParts2(sheets,parts,config,0);
        watch.Stop();
        Check(Validate(name,result,sheets,parts)==expected,name+" expected count");
        Check(before==string.Join("|",sheets.Concat(parts).Select(Snapshot)),name+" immutable input");
        var warm=Background.placeParts2(sheets,parts,config,0);
        Validate(name+" warm",warm,sheets,parts);
        Check(Layout(result)==Layout(warm),name+" warm deterministic");
        Reset();
        var cold=Background.placeParts2(sheets,parts,config,0);
        Check(Layout(result)==Layout(cold),name+" cold deterministic");
        Console.WriteLine(name+": elapsedMs="+watch.ElapsedMilliseconds);
    }
    static void BudgetAndSpacing()
    {
        Reset();
        var config=Config(); config.PlaceParts2TimeLimitMs=0;
        SvgNest.Config=config;
        var sheets=new[]{Rect(100,1000,0,0,100,100)};
        var parts=new[]{Rect(0,10,0,0,30,30),Rect(1,10,0,0,30,30)};
        var watch=Stopwatch.StartNew();
        var result=Background.placeParts2(sheets,parts,config,0);
        Check(Validate("zero search budget",result,sheets,parts)==2,"greedy completion with zero budget");
        Check(watch.ElapsedMilliseconds<30000,"soft budget smoke limit");
        Reset(); config=Config(); config.spacing=4; config.sheetSpacing=3;
        SvgNest.Config=config;
        var context=new NestingContext(); context.AddSheet(100,100,1000);
        context.AddRectanglePart(10,30,30); context.AddRectanglePart(10,30,30);
        context.StartNest(); context.NestIterate();
        Validate("spacing integrated",context.Current,context.Sheets.ToArray(),context.Polygons.ToArray());
        var placed=context.Current.placements.SelectMany(g=>g).SelectMany(s=>s.sheetplacements).ToArray();
        Check(placed.Length==2,"spacing placed count");
        var boxes=new List<Tuple<double,double,double,double>>();
        foreach(var p in placed)
        {
            var poly=context.Polygons.Single(v=>v.id==p.id);
            var path=Solid(poly,p.rotation,p.x,p.y)[0];
            double x0=path.Min(v=>v.X)/Scale, y0=path.Min(v=>v.Y)/Scale;
            double x1=path.Max(v=>v.X)/Scale, y1=path.Max(v=>v.Y)/Scale;
            Check(x0>=3-1e-5 && y0>=3-1e-5 && x1<=97+1e-5 && y1<=97+1e-5,"sheet edge spacing");
            foreach(var b in boxes)
            {
                double dx=Math.Max(0,Math.Max(b.Item1-x1,x0-b.Item3));
                double dy=Math.Max(0,Math.Max(b.Item2-y1,y0-b.Item4));
                Check(Math.Sqrt(dx*dx+dy*dy)>=4-1e-5,"part spacing");
            }
            boxes.Add(Tuple.Create(x0,y0,x1,y1));
        }
        var preparedSheets=context.Sheets.Select(p=>Background.clone(p)).ToArray();
        var preparedParts=context.Polygons.Select(p=>Background.clone(p)).ToArray();
        for(int i=0;i<preparedSheets.Length;i++)
        {
            preparedSheets[i].id=context.Sheets[i].id; preparedSheets[i].source=1000+i;
            SvgNest.offsetTree(preparedSheets[i],-(config.sheetSpacing-config.spacing/2),config,true);
        }
        for(int i=0;i<preparedParts.Length;i++)
        {
            preparedParts[i].id=i; preparedParts[i].source=10;
            SvgNest.offsetTree(preparedParts[i],config.spacing/2,config);
        }
        Reset();
        var direct=Background.placeParts2(preparedSheets,preparedParts,config,0);
        Check(Validate("spacing direct third solver",direct,context.Sheets.ToArray(),context.Polygons.ToArray())==2,"third solver spacing placed count");
        config.EnablePlaceParts2=false;
        context.StartNest();context.NestIterate();
        Check(context.Current!=null,"disabled third solver preserves pipeline");
    }
    static void ComparePaths()
    {
        var config=Config(); SvgNest.Config=config;
        var sheets=new[]{Rect(100,1000,0,0,100,100),Rect(101,1000,0,0,100,100)};
        var parts=Enumerable.Range(0,8).Select(i=>Rect(i,i<4?10:11,0,0,i<4?40:25,i<4?30:20)).ToArray();
        var methods=new Func<NFP[],NFP[],SvgNestConfig,int,SheetPlacement>[]
            {Background.placeParts,Background.placeParts1,Background.placeParts2};
        SheetPlacement best=null;
        for(int i=0;i<methods.Length;i++)
        {
            Reset(); var watch=Stopwatch.StartNew();
            var result=methods[i](sheets,parts,config,0); watch.Stop();
            Validate("path "+i,result,sheets,parts);
            Console.WriteLine("Path "+i+": elapsedMs="+watch.ElapsedMilliseconds);
            if(SheetPlacement.IsBetterThan(result,best))best=result;
            Check(!SheetPlacement.IsBetterThan(result,best),"portfolio keeps best path "+i);
        }
    }
    public static void Main()
    {
        Case("empty",new[]{Rect(100,1000,0,0,100,100)},new NFP[0],0);
        Case("no sheets",new NFP[0],new[]{Rect(0,10,0,0,20,20)},0);
        Case("single",new[]{Rect(100,1000,0,0,100,100)},new[]{Rect(0,10,0,0,30,20)},1);
        Case("duplicates",new[]{Rect(100,1000,0,0,100,100)},Enumerable.Range(0,4).Select(i=>Rect(i,10,0,0,30,30)).ToArray(),4);
        Case("skip empty small sheet",new[]{Rect(100,1000,0,0,10,10),Rect(101,1001,0,0,120,80)},new[]{Rect(0,10,0,0,90,60)},1);
        Case("concave offset sheet",new[]{Ring(100,1000,100,200,220,200,220,240,150,240,150,320,100,320)},new[]{Rect(0,10,5,7,30,30),Rect(1,10,5,7,30,30)},2);
        var holeSheet=Rect(100,1000,100,200,120,120);
        holeSheet.children=new List<NFP>{Rect(900,9000,140,240,40,40)};
        Case("sheet hole",new[]{holeSheet},new[]{Rect(0,10,0,0,30,30),Rect(1,10,0,0,30,30)},2);
        Case("rotation required",new[]{Rect(100,1000,0,0,30,50)},new[]{Rect(0,10,0,0,40,20)},1);
        Case("concave part",new[]{Rect(100,1000,0,0,100,100)},new[]{Ring(0,10,0,0,60,0,60,20,20,20,20,60,0,60),Rect(1,11,0,0,25,25)},2);
        Case("unplaceable",new[]{Rect(100,1000,0,0,100,100)},new[]{Rect(0,10,0,0,200,200),Rect(1,11,0,0,20,20)},1);
        Case("multi sheet",new[]{Rect(100,1000,0,0,50,50),Rect(101,1000,0,0,50,50)},new[]{Rect(0,10,0,0,40,40),Rect(1,10,0,0,40,40)},2);
        BudgetAndSpacing();
        ComparePaths();
        Console.WriteLine("SOLVER CHECKS COMPLETE: "+checks);
    }
}
