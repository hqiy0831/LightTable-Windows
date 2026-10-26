using LightTable.Core;
using LightTable.Windows.Imaging;
using Microsoft.UI.Dispatching;

namespace LightTable.Windows;

sealed class TableSession
{
    public DocumentStore Storage {get;}
    public TableDocument Document {get;private set;}
    public ImageStore Images {get;}
    public HashSet<Guid> Selection {get;}=[];
    public bool ShowSelection {get;set;}
    public bool Loupe {get;set;}
    LoupePower power;
    public LoupePower Power {get=>power;set=>power=LoupePowers.Validate(value);}
    public bool CloseViewing {get;set;}
    public LampState Lamp {get;}=new();
    public bool IsLit=>Lamp.IsLit;
    double brightness=1,brightnessChanged=-10;
    public double Brightness
    {
        get=>brightness;
        set{value=LightResponse.Brightness(value);if(brightness==value)return;brightness=value;brightnessChanged=System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency;}
    }
    public bool BrightnessSettled=>System.Diagnostics.Stopwatch.GetTimestamp()/(double)System.Diagnostics.Stopwatch.Frequency-brightnessChanged>=.12;
    public double IgnitionAt=>Lamp.StartedAt;
    public double Width {get;set;}=1180;
    public double Height {get;set;}=550;
    public bool Busy {get;set;}
    public long LibraryRevision {get;private set;}
    public void LibraryChanged(){LibraryRevision++;Notify();}
    public bool ReduceMotion {get;set;}
    public string? StartupWarning {get;}
    public event Action? Changed;
    public event Action<string>? Notice;
    readonly DispatcherQueueTimer saveTimer;
    public TableSession(DispatcherQueue dispatcher,string? dataRoot=null)
    {
        Storage=new(dataRoot);Document=Storage.Load(out var warning);StartupWarning=warning;Images=new(Storage);
        saveTimer=dispatcher.CreateTimer();saveTimer.Interval=TimeSpan.FromMilliseconds(300);saveTimer.IsRepeating=false;saveTimer.Tick+=(_,_)=>Flush();
        if(warning!=null)Storage.Log(warning);
        Storage.Log($"启动，schema={Document.SchemaVersion}，照片={Document.Photos.Count}，散片={Document.Slides.Count}");
    }
    public void Notify(bool save=true){if(save){saveTimer.Stop();saveTimer.Start();}Changed?.Invoke();}
    public void Tell(string text)=>Notice?.Invoke(text);
    public void Flush(){saveTimer.Stop();try{Storage.Save(Document);}catch(Exception ex){Storage.Log($"保存失败 {ex}");Tell($"布局保存失败：{ex.Message}");}}
    public string ImportMacDocument(TableDocument document,byte[] sourceBytes)
    {
        saveTimer.Stop();Storage.Save(Document);var archive=Storage.Import(document,sourceBytes);Document=document;LibraryRevision++;Selection.Clear();ShowSelection=false;CloseViewing=false;Notify(false);return archive;
    }
    public void ToggleLight(double seconds){Lamp.Toggle();Notify(false);}
    public void Select(Guid id,bool extend=false,bool indicator=true)
    {
        if(!extend)Selection.Clear();
        if(!Selection.Add(id)&&extend)Selection.Remove(id);
        ShowSelection=indicator;Document.BringToFront(id);Notify();
    }
    public void RemoveSelection(){Document.Remove(Selection);Selection.Clear();ShowSelection=false;Notify();}
    public void Clear(){Document.Slides.Clear();foreach(var a in Document.Albums)a.Position=null;Document.TableOrder.Clear();Selection.Clear();Document.Viewport=new();Document.FitScale=1;Notify();}
    public void Fit(bool replace=false){Geometry.Fit(Document,Width,Height,replace);Selection.Clear();Notify();}
    public void Place(string path,PointD screen)
    {
        var slide=Document.Place(path,Document.Viewport.World(screen));Select(slide.Id,false,false);Notify();
    }
    public void PlaceAlbum(AlbumPage page,PointD? screen=null)
    {
        page.Position=Document.Viewport.World(screen??new(Width/2,Height/2));Document.BringToFront(page.Id);Select(page.Id,false,false);Notify();
    }
    public void TransformSelection(double size=1,double rotation=0,bool straighten=false)
    {
        foreach(var s in Document.Slides.Where(s=>Selection.Contains(s.Id))){s.Width*=size;s.Angle=straighten?0:s.Angle+rotation;s.Sanitize();}
        foreach(var a in Document.Albums.Where(a=>Selection.Contains(a.Id))){a.Scale=Math.Clamp(a.Scale*size,.1,3);a.Angle=straighten?0:a.Angle+rotation;}
        Notify();
    }
    public async Task<ImportResult?> ImportAsync(IEnumerable<string> paths,bool replace=false,IProgress<(int Done,int Total)>? progress=null)
    {
        if(Busy)return null;Busy=true;Notify(false);
        try
        {
            var collection=Document.EnsureCollection();var input=paths.ToList();var result=await new PhotoImporter(Images).ImportAsync(input,replace?[]:Document.ActivePhotos,progress,catalog:Document.Photos);
            if(result.Photos.Count>0)
            {
                Document.SetCollectionPhotos(collection,result.Photos,replace);
                if(replace){collection.SourceLabel=System.IO.Path.GetFileName(input[0]);Document.SourceLabel=collection.SourceLabel;}
                LibraryRevision++;
                Notify();
            }
            var text=$"导入 {result.Photos.Count} 张"+(result.Duplicates>0?$"，跳过 {result.Duplicates} 张重复照片":"");
            if(result.Errors.Count>0){Storage.Log(string.Join("\n",result.Errors));Tell(text+$"。{result.Errors.Count} 项无法读取：\n"+string.Join("\n",result.Errors.Take(5)));}
            else if(result.AcceptedPaths.Count==0)Tell(text+"。请选择可由系统解码的图片。");
            return result;
        }
        catch(Exception ex){Storage.Log(ex.ToString());Tell($"导入失败：{ex.Message}");return null;}
        finally{Busy=false;Notify(false);}
    }
}
