using System.Text.Json.Serialization;

namespace LightTable.Core;

public readonly record struct PointD(double X, double Y)
{
    public static PointD operator +(PointD a, PointD b) => new(a.X + b.X, a.Y + b.Y);
    public static PointD operator -(PointD a, PointD b) => new(a.X - b.X, a.Y - b.Y);
    public static PointD operator *(PointD a, double b) => new(a.X * b, a.Y * b);
}
public readonly record struct SizeD(double Width, double Height);
public readonly record struct RectD(double X, double Y, double Width, double Height)
{
    [JsonIgnore] public PointD Center => new(X + Width / 2, Y + Height / 2);
    public bool Contains(PointD p) => p.X >= X && p.Y >= Y && p.X <= X + Width && p.Y <= Y + Height;
    public bool Intersects(RectD b) => X < b.X + b.Width && X + Width > b.X && Y < b.Y + b.Height && Y + Height > b.Y;
    public static RectD Between(PointD a, PointD b) => new(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Abs(a.X-b.X),Math.Abs(a.Y-b.Y));
}

public sealed class Photo
{
    public string Path { get; set; } = "";
    public double Aspect { get; set; } = 1.5;
    public string? ContentHash { get; set; }
    public long SourceLength { get; set; }
    public long SourceWriteTicks { get; set; }
    [JsonIgnore] public string Name => System.IO.Path.GetFileNameWithoutExtension(Path);
}
public sealed class Slide
{
    public const double DefaultWidth = .38;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Path { get; set; } = "";
    public double X { get; set; } = .5;
    public double Y { get; set; } = .5;
    public double Width { get; set; } = DefaultWidth;
    public double Angle { get; set; }
    public void Sanitize()
    {
        X = Geometry.Finite(X,.5); Y = Geometry.Finite(Y,.5);
        Width = Math.Clamp(Geometry.Finite(Width, DefaultWidth),.12,.9);
        Angle = Geometry.Finite(Angle,0) % 360;
    }
}
public enum FilmFormat { Film135, Film120 }
public sealed class AlbumPocket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public FilmFormat Format { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }
    public string? Path { get; set; }
    [JsonIgnore] public int Columns => Format == FilmFormat.Film135 ? 3 : 5;
    [JsonIgnore] public int Rows => Format == FilmFormat.Film135 ? 3 : 4;
    [JsonIgnore] public RectD GridRect => new(Column,Row,Columns,Rows);
    [JsonIgnore] public RectD Rect => new(48 + Column * 36,38 + Row * 50,Columns * 36,Rows * 50);
}
public sealed class AlbumPage
{
    public const double Width = 620, Height = 690;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "相册页";
    public List<AlbumPocket> Pockets { get; set; } = [];
    public PointD? Position { get; set; }
    // Windows also allows whole-sheet rotation, while keeping pocket coordinates fixed.
    public double Angle { get; set; }
    public double Scale { get; set; } = 1;
    public bool Accepts(AlbumPocket p, params Guid[] excluding) => p.Column >= 0 && p.Row >= 0 &&
        p.Column + p.Columns <= 15 && p.Row + p.Rows <= 12 &&
        Pockets.All(o => o.Id == p.Id || excluding.Contains(o.Id) || !o.GridRect.Intersects(p.GridRect));
    public AlbumPocket? Add(FilmFormat format, int? column = null, int? row = null)
    {
        var p = new AlbumPocket { Format = format };
        for (int y = row ?? 0; y <= (row ?? 12-p.Rows); y++)
        for (int x = column ?? 0; x <= (column ?? 15-p.Columns); x++)
        {
            p.Column=x; p.Row=y;
            if (Accepts(p)) { Pockets.Add(p); return p; }
        }
        return null;
    }
    public void Fill(FilmFormat format)
    {
        if (Pockets.Count != 0) return;
        while (Add(format) != null) { }
    }
    public bool Move(Guid id, int column, int row)
    {
        var p = Pockets.Find(p=>p.Id==id); if(p == null) return false;
        var moved = new AlbumPocket { Id=p.Id,Format=p.Format,Column=column,Row=row,Path=p.Path };
        if (Accepts(moved)) { p.Column=column;p.Row=row;return true; }
        var other = Pockets.Find(o=>o.Id!=id && o.Column==column && o.Row==row && o.Format==p.Format);
        if (other == null) return false;
        var swapped = new AlbumPocket { Id=other.Id,Format=other.Format,Column=p.Column,Row=p.Row };
        if (!Accepts(moved,other.Id) || !Accepts(swapped,p.Id)) return false;
        other.Column=p.Column;other.Row=p.Row;p.Column=column;p.Row=row;return true;
    }
}
public sealed class Viewport
{
    public double Scale { get; set; } = 1;
    public double OffsetX { get; set; }
    public double OffsetY { get; set; }
    public PointD Screen(PointD p) => new(p.X*Scale+OffsetX,p.Y*Scale+OffsetY);
    public PointD World(PointD p) => new((p.X-OffsetX)/Scale,(p.Y-OffsetY)/Scale);
    public void Zoom(double value, PointD anchor)
    {
        if(!double.IsFinite(value)) return;
        var w=World(anchor); Scale=Math.Clamp(value,.05,8);
        OffsetX=anchor.X-w.X*Scale;OffsetY=anchor.Y-w.Y*Scale;
    }
}
public sealed class PhotoCollection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "栏目一";
    public List<string> Paths { get; set; } = [];
    public string SourceLabel { get; set; } = "你的照片";
    public CollectionTable? Table { get; set; }
}
public sealed class CollectionTable
{
    public List<Slide> Slides { get; set; } = [];
    public List<AlbumPage> Albums { get; set; } = [];
    public List<Guid> TableOrder { get; set; } = [];
    public Viewport Viewport { get; set; } = new();
    public double WorldWidth { get; set; } = 1180;
    public double WorldHeight { get; set; } = 550;
    public double FitScale { get; set; } = 1;
}
public sealed partial class TableDocument : IJsonOnSerializing
{
    public const int CurrentSchema = 3;
    public int SchemaVersion { get; set; } = CurrentSchema;
    public List<Photo> Photos { get; set; } = [];
    void IJsonOnSerializing.OnSerializing()=>SaveActiveCollection();
    public List<PhotoCollection> Collections { get; set; } = [];
    public Guid ActiveCollectionId { get; set; }
    public List<Slide> Slides { get; set; } = [];
    public List<AlbumPage> Albums { get; set; } = [];
    public List<Guid> TableOrder { get; set; } = [];
    public Viewport Viewport { get; set; } = new();
    public double WorldWidth { get; set; } = 1180;
    public double WorldHeight { get; set; } = 550;
    public double FitScale { get; set; } = 1;
    public string SourceLabel { get; set; } = "你的照片";
    [JsonIgnore] public IEnumerable<Guid> TableIds
    {
        get
        {
            var available=Slides.Select(s=>s.Id).Concat(Albums.Where(a=>a.Position!=null).Select(a=>a.Id)).ToHashSet();
            return TableOrder.Concat(Slides.Select(s=>s.Id)).Concat(Albums.Where(a=>a.Position!=null).Select(a=>a.Id)).Distinct().Where(available.Contains);
        }
    }
    public double Aspect(string path) => Photos.Find(p=>StringComparer.OrdinalIgnoreCase.Equals(p.Path,path))?.Aspect ?? 1.5;
    public Slide Place(string path, PointD world)
    {
        var s=Slides.Find(s=>StringComparer.OrdinalIgnoreCase.Equals(s.Path,path));
        if(s==null){s=new Slide{Path=path};Slides.Add(s);}
        s.X=world.X/WorldWidth;s.Y=world.Y/WorldHeight;
        BringToFront(s.Id); return s;
    }
    public void BringToFront(Guid id) {TableOrder=TableIds.Where(i=>i!=id).Append(id).ToList();}
    public void Remove(IEnumerable<Guid> ids)
    {
        var set=ids.ToHashSet();Slides.RemoveAll(s=>set.Contains(s.Id));
        foreach(var a in Albums.Where(a=>set.Contains(a.Id))) a.Position=null;
        TableOrder.RemoveAll(set.Contains);
    }
    [JsonIgnore] public IEnumerable<string> PlacedPaths => Slides.Select(s=>s.Path).Concat(Albums.Where(a=>a.Position!=null).SelectMany(a=>a.Pockets).Select(p=>p.Path).OfType<string>()).Distinct(StringComparer.OrdinalIgnoreCase);
    public void Sanitize()
    {
        Photos=(Photos??[]).Where(p=>IsLocal(p.Path)).DistinctBy(p=>p.Path,StringComparer.OrdinalIgnoreCase).ToList();
        foreach(var p in Photos)p.Aspect=Math.Clamp(Geometry.Finite(p.Aspect,1.5),.001,1000);
        SanitizeTable();NormalizeCollections();SchemaVersion=CurrentSchema;
    }
    void SanitizeTable()
    {
        WorldWidth=Math.Clamp(Geometry.Finite(WorldWidth,1180),1,100000);
        WorldHeight=Math.Clamp(Geometry.Finite(WorldHeight,550),1,100000);
        FitScale=Math.Clamp(Geometry.Finite(FitScale,1),.001,8);
        Viewport ??= new();Viewport.Scale=Math.Clamp(Geometry.Finite(Viewport.Scale,1),.05,8);
        Viewport.OffsetX=Geometry.Finite(Viewport.OffsetX,0);Viewport.OffsetY=Geometry.Finite(Viewport.OffsetY,0);
        Slides=(Slides??[]).Where(s=>IsLocal(s.Path)).DistinctBy(s=>s.Id).DistinctBy(s=>s.Path,StringComparer.OrdinalIgnoreCase).ToList();
        foreach(var s in Slides) s.Sanitize();
        Albums=(Albums??[]).DistinctBy(a=>a.Id).Where(a=>!Slides.Any(s=>s.Id==a.Id)).ToList();
        foreach(var a in Albums)
        {
            var old=a.Pockets??[];a.Pockets=[];
            foreach(var p in old.DistinctBy(p=>p.Id)) if(Enum.IsDefined(p.Format)&&a.Accepts(p)){if(p.Path!=null&&!IsLocal(p.Path))p.Path=null;a.Pockets.Add(p);}
            if(a.Position is {} pos && (!double.IsFinite(pos.X)||!double.IsFinite(pos.Y)))a.Position=null;
            a.Angle=Geometry.Finite(a.Angle,0)%360;a.Scale=Math.Clamp(Geometry.Finite(a.Scale,1),.001,3);
            if(string.IsNullOrWhiteSpace(a.Name))a.Name="相册页";
        }
        TableOrder??=[];TableOrder=TableIds.ToList();
        foreach(var path in PlacedPaths) if(!Photos.Any(p=>StringComparer.OrdinalIgnoreCase.Equals(p.Path,path)))Photos.Add(new Photo{Path=path});
    }
    public static bool IsLocal(string? path) => !string.IsNullOrWhiteSpace(path) && System.IO.Path.IsPathFullyQualified(path) && !path.Contains("://",StringComparison.Ordinal);
}
