namespace LightTable.Core;

public static class PanelSizing
{
    public const double DefaultTrayHeight=180;
    public static double TrayHeight(double requested,double windowHeight)
    {
        var available=Math.Max(0,Geometry.Finite(windowHeight,900));
        var maximum=Math.Max(120,Math.Min(600,available-220));var minimum=Math.Min(152,maximum);
        return Math.Clamp(Geometry.Finite(requested,DefaultTrayHeight),minimum,maximum);
    }
    public static double ThumbnailScale(double listHeight)=>Math.Clamp((Geometry.Finite(listHeight,100)-28)/85,.65,1.45);
}
