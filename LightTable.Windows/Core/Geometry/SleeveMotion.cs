namespace LightTable.Core;

public static class SleeveMotion
{
    public static double Expansion(RectD origin,PointD pointer,PointD start)=>Geometry.Smooth((start.Y-pointer.Y-origin.Height)/110);
    public static RectD Frame(RectD origin,PointD pointer,PointD start,SizeD target)
    {
        var t=Expansion(origin,pointer,start);var width=origin.Width+(target.Width-origin.Width)*t;var height=origin.Height+(target.Height-origin.Height)*t;
        var gx=Math.Clamp((start.X-origin.X)/Math.Max(1,origin.Width),0,1);var gy=Math.Clamp((start.Y-origin.Y)/Math.Max(1,origin.Height),0,1);
        return new(pointer.X-width*gx,pointer.Y-height*gy,width,height);
    }
    public static PointD EdgeVelocity(PointD pointer,SizeD board)
    {
        static double Axis(double p,double length)
        {
            var band=Math.Min(48,length/4);if(band<=0)return 0;
            if(p<band)return 420*Math.Clamp((band-p)/band,0,1);
            if(p>length-band)return -420*Math.Clamp((p-length+band)/band,0,1);return 0;
        }
        return new(Axis(pointer.X,board.Width),Axis(pointer.Y,board.Height));
    }
    public static RectD Interpolate(RectD a,RectD b,double t)=>new(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Width+(b.Width-a.Width)*t,a.Height+(b.Height-a.Height)*t);
}
