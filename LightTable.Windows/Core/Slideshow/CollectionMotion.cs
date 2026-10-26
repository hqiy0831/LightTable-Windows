namespace LightTable.Core;

public readonly record struct CollectionPose(PointD Center,double Scale,double Angle,double Tilt,bool BehindFront,double Opacity)
{
    public static CollectionPose At(RectD frame,PointD destination,int index,double progress)
    {
        var t=Math.Clamp(Geometry.Finite(progress,0),0,1);var q=1-t;var start=frame.Center;
        var rise=Math.Clamp(Math.Abs(start.Y-destination.Y)*.60,90,150);var high=Math.Min(start.Y,destination.Y)-rise;
        var first=new PointD(start.X+(start.X<destination.X?-24:24),high);
        var second=new PointD(destination.X+(start.X-destination.X)*.16,high);
        var center=start*(q*q*q)+first*(3*q*q*t)+second*(3*q*t*t)+destination*(t*t*t);
        var eased=Geometry.Smooth(t);
        return new(center,1-.28*eased,(index%5-2)*3*Math.Sin(Math.PI*t),58*eased,t>=.84,Math.Clamp((1-t)/.03,0,1));
    }
}
public static class CollectionMotion
{
    public const double CommitDelay=.032,FeedStart=.152,FlightDuration=.72;
    public static double Spacing(int count)=>Math.Min(.09,.99/Math.Max(1,count-1));
    public static double TailStart(int count)=>FeedStart+Math.Max(0,count-1)*Spacing(count)+.66+.10;
    public static double Duration(int count)=>TailStart(count)+.20;
    public static PointD BoxCenter(SizeD size)=>new(size.Width/2,Math.Max(70,Math.Min(size.Height-130,size.Height*.62)));
    public static double Progress(double elapsed,int index,int count)=>ProjectionMotion.Bezier((elapsed-FeedStart-index*Spacing(count))/FlightDuration,.35,.02,.52,1);
    public static double BlackOpacity(double elapsed,int count)
    {
        var initial=.91*ProjectionMotion.Bezier((elapsed-CommitDelay)/.26,0,0,.58,1);
        var tail=ProjectionMotion.Bezier((elapsed-TailStart(count))/.16,.42,0,1,1);return initial+(1-initial)*tail;
    }
    public static double BoxOpacity(double elapsed,int count)=>ProjectionMotion.Bezier((elapsed-CommitDelay)/.18,0,0,.58,1)*(1-ProjectionMotion.Bezier((elapsed-TailStart(count))/.16,.42,0,1,1));
}
