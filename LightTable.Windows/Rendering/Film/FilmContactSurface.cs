using LightTable.Core;
using Microsoft.Graphics.Canvas;
using global::Windows.Foundation;
using global::Windows.Graphics.DirectX;

namespace LightTable.Windows.Rendering;

sealed class FilmContactSurface : IDisposable
{
    readonly CanvasDevice device;
    readonly CanvasBitmap[] textures;
    readonly Dictionary<(FilmContactVariant,double,double),(CanvasCommandList Commands,long Used)> frames=[];
    long serial;
    public long TextureBytes=>3L*FilmContactTexture.Pixels*FilmContactTexture.Pixels*4;
    public FilmContactSurface(CanvasDevice device)
    {
        this.device=device;textures=Enum.GetValues<FilmContactVariant>().Select(v=>CanvasBitmap.CreateFromBytes(device,FilmContactTexture.Make(v),FilmContactTexture.Pixels,FilmContactTexture.Pixels,DirectXPixelFormat.B8G8R8A8UIntNormalized,96,CanvasAlphaMode.Premultiplied)).ToArray();
    }
    public void Draw(CanvasDrawingSession ds,Rect photo,Guid id)
    {
        var variant=FilmContactTexture.ForFilm(id);var width=Math.Round(photo.Width,3);var height=Math.Round(photo.Height,3);var key=(variant,width,height);serial++;
        if(!frames.TryGetValue(key,out var frame))
        {
            var commands=new CanvasCommandList(device);using(var draw=commands.CreateDrawingSession())
            {
                double[] source=[0,12,20,32];var w=width+8;var h=height+8;var cx=Math.Min(12,w/2);var cy=Math.Min(12,h/2);
                double[] x=[-4,-4+cx,width+4-cx,width+4],y=[-4,-4+cy,height+4-cy,height+4];
                for(int row=0;row<3;row++)for(int column=0;column<3;column++)
                {
                    if(x[column+1]<=x[column]||y[row+1]<=y[row])continue;
                    draw.DrawImage(textures[(int)variant],new Rect(x[column],y[row],x[column+1]-x[column],y[row+1]-y[row]),new Rect(source[column]*3,source[row]*3,(source[column+1]-source[column])*3,(source[row+1]-source[row])*3));
                }
            }
            frame=(commands,serial);frames[key]=frame;
            if(frames.Count>2048){var old=frames.Where(p=>p.Key!=key).MinBy(p=>p.Value.Used);old.Value.Commands.Dispose();frames.Remove(old.Key);}
        }
        else frames[key]=(frame.Commands,serial);
        ds.DrawImage(frame.Commands,(float)photo.X,(float)photo.Y);
    }
    public void Dispose(){foreach(var frame in frames.Values)frame.Commands.Dispose();frames.Clear();foreach(var bitmap in textures)bitmap.Dispose();}
}
