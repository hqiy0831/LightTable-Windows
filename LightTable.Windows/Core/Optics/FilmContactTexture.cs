namespace LightTable.Core;

// Derived from upstream v0.1.75 FilmContactSurface.swift (MIT; see LICENSE).
public enum FilmContactVariant { Settled, LeftCurl, RightCurl }
public static class FilmContactTexture
{
    public const int Scale=3,Side=32,Pixels=Side*Scale,Padding=4,Cap=12;
    public static FilmContactVariant ForFilm(Guid id)
    {
        ulong hash=14695981039346656037;
        foreach(var value in id.ToByteArray(bigEndian:true))hash=unchecked((hash^value)*1099511628211);
        return (FilmContactVariant)(hash%3);
    }
    public static byte[] Make(FilmContactVariant variant)
    {
        var bytes=new byte[Pixels*Pixels*4];const double lower=Padding,upper=Side-Padding;
        for(int row=0;row<Pixels;row++)for(int column=0;column<Pixels;column++)
        {
            var x=(column+.5)/Scale;var y=(row+.5)/Scale;if(x>=lower&&x<upper&&y>=lower&&y<upper)continue;
            var cornerX=variant==FilmContactVariant.LeftCurl?lower:upper;
            var corner=Math.Max(0,1-Math.Sqrt((x-cornerX)*(x-cornerX)+(y-upper)*(y-upper))/(Cap-Padding));
            var lift=variant==FilmContactVariant.Settled?0:corner*corner*.72;
            var dx=Math.Max(Math.Max(lower+.28-x,0),x-upper-.28);var offsetY=.58+lift*.72;
            var dy=Math.Max(Math.Max(lower+offsetY-y,0),y-upper-offsetY);
            var falloff=Math.Max(0,1-Math.Sqrt(dx*dx+dy*dy)/(1.35+lift*.75));var alpha=(.27-lift*.07)*falloff*falloff;double white=0;
            var alongX=Math.Clamp((x-lower)/(upper-lower),0,1);var alongY=Math.Clamp((y-lower)/(upper-lower),0,1);
            var right=.40+alongY*.14+lift*.12;var bottom=.50+alongX*.22+lift*.16;
            if(x>=lower+.28&&x<upper+right&&y>=lower+.38&&y<upper+bottom)
            {
                var sideAlpha=(.74-lift*.08)*Math.Min(Math.Clamp((upper+right-x)*Scale,0,1),Math.Clamp((upper+bottom-y)*Scale,0,1));
                var combined=sideAlpha+alpha*(1-sideAlpha);white=combined>0?(y>=upper?.065:.13)*sideAlpha/combined:0;alpha=combined;
            }
            if(x>=lower-.24&&x<upper&&y>=lower-.24&&y<upper)
            {var edge=y<lower?.18-alongX*.07:.12-alongY*.04;var combined=edge+alpha*(1-edge);white=(edge+white*alpha*(1-edge))/combined;alpha=combined;}
            var i=(row*Pixels+column)*4;bytes[i]=bytes[i+1]=bytes[i+2]=(byte)Math.Clamp(Math.Round(white*alpha*255),0,255);bytes[i+3]=(byte)Math.Clamp(Math.Round(alpha*255),0,255);
        }
        return bytes;
    }
}
