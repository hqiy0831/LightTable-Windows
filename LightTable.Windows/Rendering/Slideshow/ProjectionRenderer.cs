using System.Numerics;
using LightTable.Core;
using Microsoft.Graphics.Canvas;
using global::Windows.Foundation;

namespace LightTable.Windows.Rendering;

static class ProjectionRenderer
{
    public static void Draw(CanvasDrawingSession ds,ProjectionPhoto photo,ProjectionFrame frame,SizeD size)
    {
        if(frame.Shutter<=0)return;
        var fit=ProjectionMotion.Fit(size,photo.Aspect);var travel=ProjectionMotion.At(frame.Transport,size,fit);var old=ds.Transform;
        ds.Transform=Matrix3x2.CreateRotation((float)(travel.Angle*Math.PI/180))*Matrix3x2.CreateTranslation((float)(size.Width/2+travel.X),(float)(size.Height/2+travel.Y))*old;
        try
        {
            var rect=new Rect(-fit.Width/2,-fit.Height/2,fit.Width,fit.Height);var spill=Math.Max(fit.Width,fit.Height)*photo.GlowOutset;
            using(ds.CreateLayer((float)frame.Shutter))
            {
                ds.DrawImage(photo.Glow,new Rect(rect.X-spill,rect.Y-spill,fit.Width+spill*2,fit.Height+spill*2),new Rect(0,0,photo.Glow.Size.Width,photo.Glow.Size.Height),.20f);
                ds.DrawImage(photo.Soft,rect,new Rect(0,0,photo.Soft.Size.Width,photo.Soft.Size.Height),(float)(1-frame.Focus));
                ds.DrawImage(photo.Sharp,rect,new Rect(0,0,photo.Sharp.Size.Width,photo.Sharp.Size.Height),(float)frame.Focus,CanvasImageInterpolation.HighQualityCubic,CanvasComposite.Add);
            }
        }
        finally{ds.Transform=old;}
    }
}
