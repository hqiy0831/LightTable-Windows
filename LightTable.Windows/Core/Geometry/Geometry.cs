namespace LightTable.Core;

public readonly record struct Placement(PointD Center,double Width,double Height,double Angle)
{
    public RectD Rect=>new(Center.X-Width/2,Center.Y-Height/2,Width,Height);
    public PointD Local(PointD p) => Geometry.Rotate(p-Center,-Angle);
    public bool Contains(PointD p){var q=Local(p);return Math.Abs(q.X)<=Width/2&&Math.Abs(q.Y)<=Height/2;}
    public bool Intersects(RectD r)
    {
        var rad=Angle*Math.PI/180;var c=Math.Cos(rad);var s=Math.Sin(rad);var d=r.Center-Center;
        double rw=r.Width/2,rh=r.Height/2,pw=Width/2,ph=Height/2;
        return Math.Abs(d.X)<=rw+pw*Math.Abs(c)+ph*Math.Abs(s)&&Math.Abs(d.Y)<=rh+pw*Math.Abs(s)+ph*Math.Abs(c)&&
            Math.Abs(d.X*c+d.Y*s)<=pw+rw*Math.Abs(c)+rh*Math.Abs(s)&&Math.Abs(-d.X*s+d.Y*c)<=ph+rw*Math.Abs(s)+rh*Math.Abs(c);
    }
}
public static class Geometry
{
    public static double Finite(double v,double fallback)=>double.IsFinite(v)?v:fallback;
    public static PointD Rotate(PointD p,double degrees){var a=degrees*Math.PI/180;return new(p.X*Math.Cos(a)-p.Y*Math.Sin(a),p.X*Math.Sin(a)+p.Y*Math.Cos(a));}
    public static Placement Place(TableDocument doc,Slide slide)=>Place(doc,slide,doc.Aspect(slide.Path));
    public static Placement Place(TableDocument doc,Slide slide,double aspect)
    {
        var width=Math.Min(doc.WorldWidth,doc.WorldHeight)*slide.Width*doc.FitScale;
        return new(doc.Viewport.Screen(new(slide.X*doc.WorldWidth,slide.Y*doc.WorldHeight)),width*doc.Viewport.Scale,width/aspect*doc.Viewport.Scale,slide.Angle);
    }
    public static Placement Place(TableDocument doc,AlbumPage page)=>new(doc.Viewport.Screen(page.Position??new(0,0)),AlbumPage.Width*doc.Viewport.Scale*page.Scale,AlbumPage.Height*doc.Viewport.Scale*page.Scale,page.Angle);
    public static int PreviewBucket(double width,double height,double dpi)
    {
        var px=Math.Ceiling(Math.Max(Finite(width,0),Finite(height,0))*Math.Max(1,Finite(dpi,1)));
        return new[]{256,384,512,768,1024,1536,2200}.FirstOrDefault(b=>b>=px,2200);
    }
    public static void Fit(TableDocument doc,double width,double height,bool replace=false)
    {
        if(width<=0||height<=0)return;
        if(replace){doc.Slides.Clear();foreach(var p in doc.ActivePhotos)doc.Slides.Add(new Slide{Path=p.Path});}
        doc.WorldWidth=width;doc.WorldHeight=height;doc.FitScale=1;doc.Viewport=new();
        var ids=doc.TableIds.ToList();if(ids.Count==0)return;
        var slideIndex=doc.Slides.ToDictionary(s=>s.Id);var albumIndex=doc.Albums.ToDictionary(a=>a.Id);var aspects=doc.Photos.ToDictionary(p=>p.Path,p=>p.Aspect,StringComparer.OrdinalIgnoreCase);
        var bounds=ids.Select(id=>slideIndex.TryGetValue(id,out var slide) ? FilmBounds(slide,aspects.GetValueOrDefault(slide.Path,1.5),width,height) : AlbumBounds(albumIndex[id])).ToArray();
        double maxWidth=bounds.Max(b=>b.Width),maxHeight=bounds.Max(b=>b.Height);
        double gap=ids.Any(albumIndex.ContainsKey)?28:Math.Min(20,Math.Min(width,height)*.035);
        double aw=width-2*Math.Max(Math.Min(24,Math.Min(width,height)*.05),width*.03),ah=height-2*Math.Max(Math.Min(24,Math.Min(width,height)*.05),height*.03);
        int columns=1;double best=-1,shape=double.PositiveInfinity;
        for(int c=1;c<=ids.Count;c++)
        {
            var rows=(ids.Count+c-1)/c;
            var scale=Math.Min(1,Math.Min((aw-(c-1)*gap)/c/maxWidth,(ah-(rows-1)*gap)/rows/maxHeight));
            if(scale<=0)continue;
            var gridW=c*maxWidth*scale+(c-1)*gap;var gridH=rows*maxHeight*scale+(rows-1)*gap;
            var score=Math.Abs(Math.Log(gridW/gridH/(aw/ah)));
            if(scale>best+1e-10||Math.Abs(scale-best)<1e-10&&score<shape){columns=c;best=scale;shape=score;}
        }
        if(best<=0)return;
        double fw=maxWidth*best,fh=maxHeight*best;
        int totalRows=(ids.Count+columns-1)/columns;double gh=totalRows*fh+(totalRows-1)*gap;
        var albums=doc.Albums.Any(a=>a.Position!=null);
        if(!albums)doc.FitScale=best;
        for(int i=0;i<ids.Count;i++)
        {
            int row=i/columns,col=i%columns,rowCount=Math.Min(columns,ids.Count-row*columns);
            var center=new PointD((width-(rowCount*fw+(rowCount-1)*gap))/2+fw/2+col*(fw+gap),(height-gh)/2+fh/2+row*(fh+gap));
            if(slideIndex.TryGetValue(ids[i],out var slide)){slide.Width=Slide.DefaultWidth;slide.X=center.X/width;slide.Y=center.Y/height;}
            if(albumIndex.TryGetValue(ids[i],out var album)){album.Position=center;album.Scale=best;}
        }
        if(albums)doc.FitScale=best;
        doc.TableOrder=ids;
    }
    private static SizeD FilmBounds(Slide s,double aspect,double w,double h)
    {
        var b=Math.Min(w,h)*Slide.DefaultWidth;var r=s.Angle*Math.PI/180;var c=Math.Abs(Math.Cos(r));var a=Math.Abs(Math.Sin(r));
        return new(b*(c+a/aspect),b*(a+c/aspect));
    }
    private static SizeD AlbumBounds(AlbumPage page)
    {var angle=page.Angle*Math.PI/180;var c=Math.Abs(Math.Cos(angle));var s=Math.Abs(Math.Sin(angle));return new(AlbumPage.Width*c+AlbumPage.Height*s,AlbumPage.Width*s+AlbumPage.Height*c);}
    public static double Smooth(double t){t=Math.Clamp(t,0,1);return t*t*(3-2*t);}
    public static double Quintic(double t){t=Math.Clamp(t,0,1);return Math.Clamp(t*t*t*(t*(t*6-15)+10),0,1);}
}
