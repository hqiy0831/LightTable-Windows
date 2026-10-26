namespace LightTable.Core;

public readonly record struct LightSample(double Transmission,double Flare,double Glow);
public static class LightResponse
{
    public const double Rise=.12,Peak=.16,BloomRelease=.28,Settling=2.0;
    public static LightSample Sample(double elapsed,bool lit,bool reduceMotion=false)
    {
        if(!lit)return new(0,0,0);
        if(reduceMotion||elapsed>=Settling)return new(1,0,.5);
        var time=Math.Max(0,Geometry.Finite(elapsed,0));var transmission=Geometry.Smooth(time/Rise);
        var flare=time<=Peak?Geometry.Smooth(time/Peak):time<=BloomRelease?1:1-Geometry.Quintic((time-BloomRelease)/(Settling-BloomRelease));
        return new(transmission,flare,(.5+.22*flare)*transmission);
    }
    public static double Power(double b)=>Math.Clamp(Geometry.Finite(b,1),.1,1.6);
    public static double Brightness(double b)=>Math.Clamp(Geometry.Finite(b,1),0,1.6);
    public static double Level(double b)=>Math.Sqrt(Math.Min(1,Power(b)));
    public static double BrightnessMix(double b)=>Math.Clamp((Power(b)-1)/.6,0,1);
    public static double GlowMix(double f,double b)=>Math.Clamp(Geometry.Finite(f,0),0,1)*Math.Sqrt(Math.Min(1,Power(b)));
    public static double Exposure(double f,double b)=>.80*GlowMix(f,b);
    public static readonly (double R,double G,double B) UnlitPanel=(.84,.81,.70);
    public static double PanelWhite(double b,double f)
    {
        var p=Power(b);var steady=p<=1?.32+(.90-.32)*Math.Sqrt(p):.90+(p-1)*.08;
        return steady+Math.Min(.10,1-steady)*GlowMix(f,p);
    }
}
public static class LoupeGeometry
{
    public const double Diameter=260,Magnification=1.5,EnterDuration=.50,ExitDuration=.34,FocalDuration=.26;
    public static double CloseDiameter(SizeD size)=>Math.Max(Diameter,Math.Min(size.Width,size.Height)*.94);
    public static PointD CloseCenter(PointD origin,SizeD size)
    {
        var radius=CloseDiameter(size)/2;var inset=radius+Math.Max(8,radius*.025);
        double Axis(double p,double length)=>length<=inset*2?length/2:Math.Clamp(p,inset,length-inset);
        return new(Axis(origin.X,size.Width),Axis(origin.Y,size.Height));
    }
}
