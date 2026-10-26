using System.Runtime.InteropServices.WindowsRuntime;
using LightTable.Core;
using LightTable.Windows.Imaging;
using LightTable.Windows.Rendering;
using Microsoft.Graphics.Canvas;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using global::Windows.Graphics.Imaging;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task<string> CheckIgnitionAndZeroBaseline()
    {
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        const int width=300,height=100;var pixels=new byte[width*height*4];byte[] levels=[0,64,128,200,235];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++){int p=(y*width+x)*4;pixels[p]=pixels[p+1]=pixels[p+2]=levels[x/60];pixels[p+3]=255;}
        var decoded=new DecodedPhoto("user-bug-photometric-stripes","diagnostic-only",width,height,pixels,512);using var tables=new OpticalTables(board.Device);using var photo=new OpticalPhoto(board.Device,decoded,tables);
        CanvasRenderTarget Frame(double time,bool lit,double brightness)
        {
            var result=new CanvasRenderTarget(board.Device,width,height,96);using var ds=result.CreateDrawingSession();ds.Clear(Colors.Transparent);ds.DrawImage(photo.View(LightResponse.Sample(time,lit),brightness,tables,false));return result;
        }
        using var off=Frame(5,false,1);using var zero=Frame(5,true,0);var offBytes=off.GetPixelBytes();var zeroBytes=zero.GetPixelBytes();
        Require(offBytes.Zip(zeroBytes).All(p=>Math.Abs(p.First-p.Second)<=1),"关灯照片与开灯0%像素不一致");
        using var steady=Frame(2,true,1);var baseline=steady.GetPixelColors();var samples=new List<object>();double previous=double.PositiveInfinity;
        foreach(var time in new[]{.16,.28,.6,1.1,1.6,2.0})
        {
            using var current=Frame(time,true,1);var actual=current.GetPixelColors();double Delta(int stripe)=>Enumerable.Range(35,30).SelectMany(y=>Enumerable.Range(stripe*60+20,20).Select(x=>(actual[y*width+x].R-baseline[y*width+x].R))).Average();
            var black=Delta(0);var shadow=Delta(1);var midtone=Delta(2);var highlight=Delta(3);var white=Delta(4);
            Require(Math.Abs(black)<=1&&Math.Abs(shadow)<=1&&Math.Abs(midtone)<=1,$"点灯时非高光被整体提亮：t={time},dark={black},mid={midtone}");
            if(time==.16)Require(highlight>4&&white>3,$"没有实际高光过曝：high={highlight},white={white}");
            Require(highlight<=previous+1,"高光回落不单调");previous=highlight;if(time==2)Require(actual.SequenceEqual(baseline),"2秒后没有精确回到稳态");
            await current.SaveAsync(Path.Combine(session.Storage.Root,"logs",$"ignition-{time:F2}.png"),CanvasBitmapFileFormat.Png);samples.Add(new{Time=time,BlackDelta=black,ShadowDelta=shadow,MidtoneDelta=midtone,HighlightDelta=highlight,WhiteDelta=white});
        }
        var previousBrightness=session.Brightness;try
        {
            session.Brightness=0;using var tableOff=renderer.RenderSnapshot(false,0);using var tableZero=renderer.RenderSnapshot(true,0);
            await tableOff.SaveAsync(Path.Combine(session.Storage.Root,"logs","zero-table-off.png"),CanvasBitmapFileFormat.Png);await tableZero.SaveAsync(Path.Combine(session.Storage.Root,"logs","zero-table-on.png"),CanvasBitmapFileFormat.Png);
            var differences=tableOff.GetPixelBytes().Zip(tableZero.GetPixelBytes()).Select(p=>Math.Abs(p.First-p.Second)).ToArray();Require(differences.All(p=>p<=1),$"整台关灯与开灯0%基准不同：max={differences.Max()},mean={differences.Average():F5},count={differences.Count(p=>p>1)}");
        }
        finally{session.Brightness=previousBrightness;session.Notify(false);}
        return "实际GPU：关灯/开灯0%照片及整台差≤1/255；高光先过曝再2秒回到精确稳态，暗部/中间调差≤1/255。"+System.Text.Json.JsonSerializer.Serialize(samples);
    }
    async Task<string> CheckRealTimeViewingAnimations()
    {
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        async Task Wait(Func<bool> condition){var until=TableRenderer.Seconds+2;while(!condition()&&TableRenderer.Seconds<until)await Task.Delay(16);Require(condition(),"实际UI动画时钟没有完成");}
        var wasReduced=session.ReduceMotion;var wasLoupe=session.Loupe;var wasClose=session.CloseViewing;var wasPower=session.Power;var wasLit=session.IsLit;
        Require(!wasReduced,"本应用新数据目录默认仍关闭动画");var values=new List<object>();
        try
        {
            session.Loupe=true;session.Power=LoupePower.One;session.CloseViewing=false;renderer.Pointer=Geometry.Place(session.Document,session.Document.Slides[0]).Center;renderer.PointerInside=true;session.Notify(false);await Wait(()=>renderer.CloseProgress<=.001&&renderer.FocalProgress<=.001);
            if(!session.IsLit)session.ToggleLight(TableRenderer.Seconds);await Wait(()=>!session.Lamp.Pending);var ignitionStarted=session.IgnitionAt;
            session.CloseViewing=true;session.Notify(false);await Task.Delay(80);Require(renderer.CloseProgress>0&&renderer.CloseProgress<1,"凑近进入直接跳到终点");values.Add(new{Stage="enter",Progress=renderer.CloseProgress});
            using(var frame=renderer.RenderLensSnapshot(renderer.CloseProgress,renderer.FocalProgress))await frame.SaveAsync(Path.Combine(session.Storage.Root,"logs","space-enter-midway.png"),CanvasBitmapFileFormat.Png);
            await Wait(()=>renderer.CloseProgress==1&&renderer.RenderedCloseProgress==1);session.Power=LoupePower.Eight;session.Notify(false);await Task.Delay(65);Require(renderer.FocalProgress>0&&renderer.FocalProgress<3,"按Tab对应的倍率状态切换没有动画");Require(magnification.ThumbPosition>0&&magnification.ThumbPosition<144,"胶囊滑块没有中间位置");
            values.Add(new{Stage="focal",Progress=renderer.FocalProgress,Thumb=magnification.ThumbPosition});await Wait(()=>renderer.FocalProgress==3&&renderer.RenderedFocalProgress==3&&magnification.ThumbPosition>=143.99);
            var capsule=new RenderTargetBitmap();await capsule.RenderAsync(magnification);var buffer=await capsule.GetPixelsAsync();
            using(var file=File.Create(Path.Combine(session.Storage.Root,"logs","magnification-capsule.png")))using(var stream=file.AsRandomAccessStream())
            {var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,(uint)capsule.PixelWidth,(uint)capsule.PixelHeight,96,96,buffer.ToArray());await encoder.FlushAsync();}
            session.Power=LoupePower.One;session.Notify(false);await Task.Delay(65);Require(renderer.FocalProgress>0&&renderer.FocalProgress<3,"倍率反向切换没有平滑回程");await Wait(()=>renderer.FocalProgress==0&&renderer.RenderedFocalProgress==0);
            foreach(var power in Enum.GetValues<LoupePower>())
            {
                session.Power=power;session.Notify(false);await Wait(()=>renderer.FocalProgress==(int)power&&renderer.RenderedFocalProgress==(int)power&&Math.Abs(magnification.ThumbPosition-48*(int)power)<.01);
                Require(Math.Abs(magnification.ThumbPosition-48*(int)power)<.01,"四档滑块终点错误");
                Require(Math.Abs(renderer.RenderedMagnification-LoupePowers.Scale(power))<.0001,"实际镜片变换倍率错误");
                using var frame=renderer.RenderLensSnapshot(1,(int)power);await frame.SaveAsync(Path.Combine(session.Storage.Root,"logs",$"loupe-{LoupePowers.Scale(power):0}x.png"),CanvasBitmapFileFormat.Png);
                values.Add(new{Stage="power",Power=power.ToString(),Scale=renderer.RenderedMagnification,Thumb=magnification.ThumbPosition});
            }
            session.CloseViewing=false;session.Notify(false);await Task.Delay(65);Require(renderer.CloseProgress>0&&renderer.CloseProgress<1,"松开空格对应回程直接跳变");values.Add(new{Stage="exit",Progress=renderer.CloseProgress});await Wait(()=>renderer.CloseProgress==0&&renderer.RenderedCloseProgress==0);Require(session.IsLit&&session.IgnitionAt==ignitionStarted,"凑近状态返回改变灯开关或重启点灯");
            return "实际DispatcherQueue计时、GPU进入/回程中途帧、倍率双向中间值、单胶囊滑块位置和XAML截图均通过。"+System.Text.Json.JsonSerializer.Serialize(values)+"。此项设置生产状态，不冒充真实长按键盘输入。";
        }
        finally{session.ReduceMotion=wasReduced;session.Loupe=wasLoupe;session.CloseViewing=wasClose;session.Power=wasPower;if(session.IsLit!=wasLit)session.ToggleLight(TableRenderer.Seconds);session.Notify(false);}
    }
}
