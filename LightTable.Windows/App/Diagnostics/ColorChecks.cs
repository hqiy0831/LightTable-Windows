using LightTable.Core;
using LightTable.Windows.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI;
using global::Windows.Graphics.DirectX;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    Task<string> CheckSrgbHighlightPrimaries()
    {
        using var tables=new OpticalTables(board.Device);
        var evidence=new List<object>();
        // These chromatic patches detect using P3 luminance weights on sRGB
        // pixels, and R/B axis transposition that neutral-only tests miss.
        foreach(var (r,g,b) in new[]{(0,255,0),(0,255,255),(230,230,51),(51,230,230),(128,128,128),(255,255,255)})
        {
            using var source=CanvasBitmap.CreateFromBytes(board.Device,[(byte)b,(byte)g,(byte)r,255],1,1,DirectXPixelFormat.B8G8R8A8UIntNormalized);
            using var effect=new TableTransfer3DEffect{Source=source,Table=tables.Highlights};
            using var target=new CanvasRenderTarget(board.Device,1,1,96);
            using(var ds=target.CreateDrawingSession()){ds.Clear(Colors.Transparent);ds.DrawImage(effect);}
            var pixel=target.GetPixelColors()[0];var luminance=SrgbOptics.Luminance(r/255d,g/255d,b/255d);var mask=Geometry.Smooth((luminance-.60)/.32);
            var delta=Math.Max(Math.Abs(pixel.R-r*mask),Math.Max(Math.Abs(pixel.G-g*mask),Math.Abs(pixel.B-b*mask)));
            if(delta>2)throw new InvalidOperationException($"sRGB彩色LUT不一致: input={r},{g},{b}, actual={pixel.R},{pixel.G},{pixel.B}, L={luminance}, delta={delta}");
            evidence.Add(new{Input=new[]{r,g,b},Actual=new[]{(int)pixel.R,pixel.G,pixel.B},Luminance=luminance,MaxByteDelta=delta});
        }
        return Task.FromResult("实际GPU彩色高光提取匹配线性sRGB亮度（32³ LUT插值差≤2/255），不等于P3端到端验收。"+System.Text.Json.JsonSerializer.Serialize(evidence));
    }
}
