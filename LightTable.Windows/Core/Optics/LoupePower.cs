namespace LightTable.Core;

public enum LoupePower { One, Two, Four, Eight }

public static class LoupePowers
{
    public const int Count=4;
    public static LoupePower Validate(LoupePower power)=>Enum.IsDefined(power)?power:LoupePower.One;
    public static double Scale(LoupePower power)=>Math.Pow(2,(int)Validate(power));
    public static double ScaleAt(double position)=>Math.Pow(2,Math.Clamp(Geometry.Finite(position,0),0,Count-1));
    public static LoupePower Next(LoupePower power)=>(LoupePower)(((int)Validate(power)+1)%Count);
    public static LoupePower At(double x,double width)
    {
        if(!double.IsFinite(x)||!double.IsFinite(width)||width<=0)return LoupePower.One;
        return (LoupePower)Math.Clamp((int)Math.Floor(x/width*Count),0,Count-1);
    }
}

// Ease elapsed time within each transition, never the absolute thumb position.
// Retarget from the currently rendered value, including on a fast reversal.
public sealed class SmoothTransitionClock
{
    double from,to,started,duration;
    public bool Active(double now)=>now-started<duration&&from!=to;
    public double Value(double now)
    {
        if(duration<=0)return to;
        var t=Math.Clamp((now-started)/duration,0,1);
        return from+(to-from)*ProjectionMotion.Bezier(t,.42,0,.58,1);
    }
    public void Target(double target,double now,double seconds)
    {
        if(seconds<=0){from=to=target;started=now;duration=0;return;}
        if(target==to)return;
        from=Value(now);to=target;started=now;duration=seconds;
    }
}
