using System.Diagnostics;
using System.Text.Json;
using LightTable.Core;
using LightTable.Windows.Imaging;
using LightTable.Windows.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task<string> CheckFusedLampEquivalence()
    {
        const int width=96,height=64;var pixels=new byte[width*height*4];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            var p=(y*width+x)*4;int alpha=new[]{0,64,128,255}[y/16];
            pixels[p]=(byte)((x*29%256)*alpha/255);pixels[p+1]=(byte)((x*7%256)*alpha/255);pixels[p+2]=(byte)((x*13%256)*alpha/255);pixels[p+3]=(byte)alpha;
        }
        using var tables=new OpticalTables(board.Device);using var photo=new OpticalPhoto(board.Device,new DecodedPhoto("fused-alpha-colors","diagnostic-only",width,height,pixels,128),tables);
        byte[] Frame(LightSample sample,double brightness,bool reference)
        {
            using var target=new CanvasRenderTarget(board.Device,width,height,96);
            using(var ds=target.CreateDrawingSession()){ds.Clear(Colors.Transparent);ds.DrawImage(reference?photo.ReferenceView(sample,brightness):photo.View(sample,brightness,tables,false));}
            return target.GetPixelBytes();
        }
        var cases=new List<object>();var maximum=0;double total=0;long values=0;
        foreach(var brightness in new[]{0,.1,.5,1,1.6})foreach(var time in new[]{0,.04,.12,.16,.28,.6,1.1,1.6,2})
        {
            var sample=LightResponse.Sample(time,true);var expected=Frame(sample,brightness,true);var actual=Frame(sample,brightness,false);
            int max=0;double sum=0;
            for(int i=0;i<expected.Length;i++){int delta=Math.Abs(expected[i]-actual[i]);max=Math.Max(max,delta);sum+=delta;}
            maximum=Math.Max(maximum,max);total+=sum;values+=expected.Length;cases.Add(new{Brightness=brightness,Time=time,Maximum=max,Mean=sum/expected.Length});
            if(max>2)throw new InvalidOperationException($"融合光学与旧GPU图像不同: brightness={brightness},time={time},max={max},mean={sum/expected.Length:F5}");
        }
        var result=new{Cases=cases,Maximum=maximum,Mean=total/values,AlphaLevels=new[]{0,64,128,255},Scope="两条实际GPU路径同图像、同尺寸、同设备；45个亮度/点灯状态，不证明完整P3输出"};
        await File.WriteAllTextAsync(Path.Combine(session.Storage.Root,"logs","fused-lamp.json"),JsonSerializer.Serialize(result,DocumentStore.JsonOptions));
        return $"单次四输入shader与原七节点GPU图对照：45状态含0/64/128/255alpha；最大差{maximum}/255，平均差{total/values:F6}/255。";
    }
}
