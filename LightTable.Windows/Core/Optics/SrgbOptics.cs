namespace LightTable.Core;

// Current decoder output is ICC-converted sRGB, not Display P3. Keep luminance
// weights consistent with those primaries until a wide-gamut path is present.
public static class SrgbOptics
{
    public static double Linear(double encoded)=>encoded<=.04045?encoded/12.92:Math.Pow((encoded+.055)/1.055,2.4);
    public static double Luminance(double r,double g,double b)=>.2126*Linear(r)+.7152*Linear(g)+.0722*Linear(b);
}
