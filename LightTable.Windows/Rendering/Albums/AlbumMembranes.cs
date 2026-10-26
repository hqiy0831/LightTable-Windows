using LightTable.Core;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using global::Windows.Foundation;
using global::Windows.UI;

namespace LightTable.Windows.Rendering;

// Immutable device-owned command lists keep membrane geometry out of motion
// frames. Vector commands retain sharp seams at every DPI without large rasters.
sealed class AlbumMembranes(CanvasDevice device) : IDisposable
{
    readonly Dictionary<string,CanvasCommandList> cache=[];
    public int Count=>cache.Count;
    public CanvasCommandList Get(AlbumPage page,bool front)
    {
        var key=$"{front}:"+string.Join(';',page.Pockets.Select(p=>$"{p.Column},{p.Row},{(int)p.Format}"));
        if(cache.TryGetValue(key,out var cached))return cached;
        var image=new CanvasCommandList(device);
        try
        {
            using(var ds=image.CreateDrawingSession())
            {
                if(!front)
                {
                    using var body=Perforated(new(1,1,618,688));using var binding=Perforated(new(2,2,31,686));
                    ds.FillGeometry(body,Gray(.78,.13));ds.FillGeometry(binding,Gray(.85,.10));
                }
                else
                {
                    ds.DrawRectangle(new Rect(2,2,616,686),Gray(1,.35),1);ds.DrawRectangle(new Rect(4,4,612,682),Gray(0,.17),1);
                    for(int row=0;row<9;row++)ds.DrawEllipse(16.5f,60.5f+row*72,7.5f,11.5f,Gray(1,.32),1);
                    ds.DrawLine(34,8,34,682,Gray(1,.32),1);
                    foreach(var pocket in page.Pockets)
                    {
                        var r=pocket.Rect;var rect=new Rect(r.X+1,r.Y+1,r.Width-2,r.Height-2);
                        ds.FillRectangle(rect,Gray(1,.018));ds.DrawRectangle(rect,Gray(0,.17),1);
                        ds.DrawRectangle(new Rect(rect.X+2,rect.Y+2,rect.Width-4,rect.Height-4),Gray(1,.36),.8f);
                        for(double x=rect.X+4;x<rect.Right-4;x+=3.6)ds.DrawLine((float)x,(float)(rect.Bottom-4),(float)Math.Min(x+1.2,rect.Right-4),(float)(rect.Bottom-4),Gray(1,.36),1.5f);
                        using var path=new CanvasPathBuilder(device);path.BeginFigure((float)(rect.X+5),(float)(rect.Y+7));
                        path.AddCubicBezier(new((float)(rect.X+rect.Width/2-18),(float)(rect.Y+10)),new((float)(rect.X+rect.Width/2+24),(float)(rect.Y+3)),new((float)(rect.Right-5),(float)(rect.Y+7)));path.EndFigure(CanvasFigureLoop.Open);
                        using var crease=CanvasGeometry.CreatePath(path);ds.DrawGeometry(crease,Gray(1,.44),1.1f);
                    }
                }
            }
            if(cache.Count>=32){foreach(var value in cache.Values)value.Dispose();cache.Clear();}cache[key]=image;return image;
        }
        catch{image.Dispose();throw;}
    }
    CanvasGeometry Perforated(Rect rect)
    {
        var shape=CanvasGeometry.CreateRectangle(device,rect);
        try
        {
            for(int row=0;row<9;row++)
            {
                using var hole=CanvasGeometry.CreateEllipse(device,16.5f,60.5f+row*72,6.5f,10.5f);
                var next=shape.CombineWith(hole,System.Numerics.Matrix3x2.Identity,CanvasGeometryCombine.Exclude);shape.Dispose();shape=next;
            }
            return shape;
        }
        catch{shape.Dispose();throw;}
    }
    static Color Gray(double gray,double alpha)=>Color.FromArgb((byte)Math.Round(alpha*255),(byte)Math.Round(gray*255),(byte)Math.Round(gray*255),(byte)Math.Round(gray*255));
    public void Dispose(){foreach(var value in cache.Values)value.Dispose();cache.Clear();}
}
