namespace LightTable.Core;

public enum ProjectionPhase { Waiting, Departing, Black, Arriving, Settled }
public readonly record struct TravelPose(double X,double Y,double Angle);
public static class ProjectionMotion
{
    public const double DepartureDuration=.08,BlackDuration=.12,ArrivalDuration=.18;
    public static SizeD Fit(SizeD container,double aspect)
    {
        var w=container.Width*.84;var h=container.Height*.84;
        return new(Math.Min(w,h*aspect),Math.Min(h,w/aspect));
    }
    public static TravelPose At(double transport,SizeD container,SizeD image)
    {
        var distance=(container.Width+image.Width)*.52;
        if(transport>0){var t=Math.Min(1,transport);return new(-(distance+image.Height*.07)*t,container.Height*.025*t,8*t);}
        var p=Math.Clamp(-transport,0,1);return new(-distance*p,-container.Height*(.15*Math.Sin(Math.PI*p)+.035*p),-5*p);
    }
    public static double Bezier(double t,double x1,double y1,double x2,double y2)
    {
        if(t<=0)return 0;if(t>=1)return 1;double lo=0,hi=1,u=t;
        static double Evaluate(double u,double a,double b)=>3*(1-u)*(1-u)*u*a+3*(1-u)*u*u*b+u*u*u;
        for(int i=0;i<18;i++){u=(lo+hi)/2;if(Evaluate(u,x1,x2)<t)lo=u;else hi=u;}
        return Evaluate(u,y1,y2);
    }
}
