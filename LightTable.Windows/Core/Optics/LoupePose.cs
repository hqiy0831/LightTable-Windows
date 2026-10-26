namespace LightTable.Core;

public readonly record struct LoupePose(double Diameter,double Magnification,double Surround,double Travel)
{
    // closeMagnification is a scale relative to the board in screen DIPs,
    // independent of aperture size, viewport zoom and decoded pixel count.
    public static LoupePose Sample(SizeD size,double progress,double closeMagnification)
    {
        var p=Math.Clamp(Geometry.Finite(progress,0),0,1);
        var target=Math.Clamp(Geometry.Finite(closeMagnification,1),1,8);
        var finalAperture=LoupeGeometry.CloseDiameter(size)/LoupeGeometry.Diameter;
        var aperture=Geometry.Quintic(p);
        // The selected scale applies to the ordinary lens as well as Space.
        // Space changes aperture/position, never silently replaces the power with 1.5x.
        return new(LoupeGeometry.Diameter*(1+(finalAperture-1)*aperture),target,Geometry.Quintic((p-.12)/.78),aperture);
    }
    public static double EdgeInset(double normalizedRadius,double radius,double strength)=>radius*(.0045*Geometry.Smooth((normalizedRadius-.90)/.10)+.0015*Geometry.Smooth((normalizedRadius-.97)/.03))*Math.Clamp(strength,0,1);
}

// An interruptible clock preserves the current value on a fast Space reversal.
// Geometry owns easing; the approach clock is linear, as in the SwiftUI source.
public sealed class TransitionClock
{
    double from,to,started,duration;
    public bool Active(double now)=>now-started<duration&&from!=to;
    public double Value(double now)
    {
        if(duration<=0)return to;
        return from+(to-from)*Math.Clamp((now-started)/duration,0,1);
    }
    public void Target(double target,double now,double seconds)
    {
        if(seconds<=0){from=to=target;started=now;duration=0;return;}
        if(target==to)return;from=Value(now);to=target;started=now;duration=seconds;
    }
}
