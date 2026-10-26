using LightTable.Core;
using LightTable.Windows.Imaging;
using Microsoft.Graphics.Canvas;
using System.Runtime.InteropServices.WindowsRuntime;
using global::Windows.Graphics.Imaging;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    async Task<string> CheckIndependentWorkspace()
    {
        var doc=session.Document;doc.SaveActiveCollection();var first=doc.EnsureCollection();var ids=doc.TableIds.ToArray();var oldSelection=session.Selection.ToArray();var originalCollections=doc.Collections.Select(c=>c.Id).ToHashSet();
        try
        {
            session.NewCollection();renderer.DocumentChanged();var second=doc.EnsureCollection();
            if(doc.ActivePhotos.Count!=0||doc.Slides.Count!=0||doc.Albums.Count!=0)throw new InvalidOperationException("新栏目不是完整空白工作区");
            using(var empty=renderer.RenderSnapshot(true,0,true))if(empty.GetPixelColors().Distinct().Count()!=1)throw new InvalidOperationException("新栏目残留上一个台面缓存像素");
            var photo=doc.Photos[0];doc.SetCollectionPhotos(second,[photo],false);session.Place(photo.Path,new(session.Width/2,session.Height/2));await renderer.PrepareVisibleTable();
            if(doc.Slides.Count!=1)throw new InvalidOperationException("第二栏目没有独立散片");var secondId=doc.Slides[0].Id;session.Flush();
            var restored=new DocumentStore(session.Storage.Root).Load(out var warning);if(warning!=null||restored.ActiveCollectionId!=second.Id||restored.Slides.Single().Id!=secondId)throw new InvalidOperationException("活动栏目存取失败");
            restored.SwitchCollection(first.Id);if(!restored.TableIds.SequenceEqual(ids))throw new InvalidOperationException("非活动栏目未独立保存");
            session.SwitchCollection(first.Id);renderer.DocumentChanged();if(!doc.TableIds.SequenceEqual(ids))throw new InvalidOperationException("切回第一栏目未恢复原摆放");
            session.SwitchCollection(second.Id);renderer.DocumentChanged();if(doc.Slides.Single().Id!=secondId)throw new InvalidOperationException("切回第二栏目未恢复对应散片");
            return "生产Session新建/切换：第二栏目完整空白且GPU无旧台面残影；独立装入一张散片，原子保存/新Store恢复及往返切换均保留各自项目。";
        }
        finally
        {
            session.SwitchCollection(first.Id);doc.Collections.RemoveAll(c=>!originalCollections.Contains(c.Id));session.Selection.Clear();session.Selection.UnionWith(oldSelection);session.LibraryChanged();renderer.DocumentChanged();
        }
    }
    async Task<string> CheckMetadataImport(string[] originals)
    {
        var images=new ImageStore(new DocumentStore(Path.Combine(session.Storage.Root,"metadata-check")));
        var importer=new PhotoImporter(images);var first=await importer.ImportAsync(originals,[]);
        if(first.Photos.Count!=originals.Length||images.DecodeCount!=0||first.Photos.Any(p=>p.SourceLength<=0||p.ContentHash==null))throw new InvalidOperationException("导入阶段没有使用纯元数据管线");
        var before=images.MetadataReads;var second=await importer.ImportAsync(originals,[],catalog:first.Photos);
        if(second.Photos.Count!=originals.Length||images.MetadataReads!=before||images.DecodeCount!=0)throw new InvalidOperationException("跨栏目没有复用未变化的元数据");
        return $"{first.Photos.Count} 张导入，像素解码=0；新栏目复用后额外元数据读取=0，照片顺序相同={first.Photos.Select(p=>p.Path).SequenceEqual(second.Photos.Select(p=>p.Path))}";
    }
    async Task<string> CheckActualLoupeSampling()
    {
        var doc=session.Document;var oldPhotos=doc.Photos;var oldSlides=doc.Slides;var oldAlbums=doc.Albums;var oldOrder=doc.TableOrder;var oldCollections=doc.Collections;var oldActive=doc.ActiveCollectionId;
        var oldViewport=doc.Viewport;var oldWidth=doc.WorldWidth;var oldHeight=doc.WorldHeight;var oldFit=doc.FitScale;var oldPointer=renderer.Pointer;var oldBrightness=session.Brightness;
        var path=Path.Combine(session.Storage.Root,"loupe-gradient.png");const int side=512;var pixels=new byte[side*side*4];
        for(int y=0;y<side;y++)for(int x=0;x<side;x++){int i=(y*side+x)*4;pixels[i]=pixels[i+1]=pixels[i+2]=(byte)(32+x*190/(side-1));pixels[i+3]=255;}
        using(var file=File.Create(path))using(var random=file.AsRandomAccessStream())
        {var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,random);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,side,side,96,96,pixels);await encoder.FlushAsync();}
        var evidence=new List<object>();
        try
        {
            doc.Photos=[new(){Path=path,Aspect=1}];doc.Slides=[new(){Path=path,Width=.85}];doc.Albums=[];doc.TableOrder=[];doc.Collections=[new(){Paths=[path]}];doc.ActiveCollectionId=doc.Collections[0].Id;
            doc.WorldWidth=session.Width;doc.WorldHeight=session.Height;doc.FitScale=1;doc.Viewport=new();session.Brightness=1;renderer.Pointer=Geometry.Place(doc,doc.Slides[0]).Center;renderer.DocumentChanged();await renderer.PrepareAsync(path,2200);
            using var reference=renderer.RenderSnapshot(true,0);
            foreach(var close in new[]{0d,1d})foreach(var power in Enum.GetValues<LoupePower>())
            {
                var scale=LoupePowers.Scale(power);using var lens=renderer.RenderLensSnapshot(close,(int)power);
                var origin=renderer.Pointer+renderer.BoardOrigin;var center=close==0?origin:LoupeGeometry.CloseCenter(origin,new(lens.Size.Width,lens.Size.Height));
                const int offset=80;
                var actual=lens.GetPixelColors((int)Math.Round(center.X)+offset,(int)Math.Round(center.Y),1,1)[0];
                var expected=reference.GetPixelColors((int)Math.Round(renderer.Pointer.X+offset/scale),(int)Math.Round(renderer.Pointer.Y),1,1)[0];
                var error=Math.Abs(actual.R-expected.R);if(error>3)throw new InvalidOperationException($"实际采样比例错误 close={close},power={scale},R={actual.R},expected={expected.R},error={error}");
                await lens.SaveAsync(Path.Combine(session.Storage.Root,"logs",$"actual-loupe-{close}-{scale}x.png"),CanvasBitmapFileFormat.Png);
                evidence.Add(new{Close=close,Power=scale,Actual=actual.R,Expected=expected.R,Error=error});
            }
        }
        finally
        {
            doc.Photos=oldPhotos;doc.Slides=oldSlides;doc.Albums=oldAlbums;doc.TableOrder=oldOrder;doc.Collections=oldCollections;doc.ActiveCollectionId=oldActive;doc.Viewport=oldViewport;doc.WorldWidth=oldWidth;doc.WorldHeight=oldHeight;doc.FitScale=oldFit;renderer.Pointer=oldPointer;session.Brightness=oldBrightness;renderer.DocumentChanged();
        }
        return "实际GPU采样与台面对应坐标像素差≤3/255，普通/凑近共8帧："+System.Text.Json.JsonSerializer.Serialize(evidence);
    }
}
