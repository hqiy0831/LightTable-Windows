using LightTable.Core;
using LightTable.Windows.Rendering;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using global::Windows.UI.Core;

namespace LightTable.Windows;

sealed partial class MainWindow
{
    PointD? press,last;
    Guid? pressedId;
    uint? gesturePointer;
    TableManipulation? touchManipulation;
    bool dragging,marquee,panning,rotating,resizing;
    bool tabHeld,spaceHeld;
    double initialAngle,initialWidth;
    Dictionary<Guid,PointD> dragOrigins=[];
    PointD dragPanStart;
    static bool Down(VirtualKey key)=>(InputKeyboardSource.GetKeyStateForCurrentThread(key)&CoreVirtualKeyStates.Down)!=0;
    static PointD Pt(global::Windows.Foundation.Point p)=>new(p.X,p.Y);
    void Zoom(double factor,PointD? at=null){session.Document.Viewport.Zoom(session.Document.Viewport.Scale*factor,at??new(session.Width/2,session.Height/2));session.Notify();}
    void SetupInput()
    {
        board.AddHandler(UIElement.PointerPressedEvent,new PointerEventHandler((_,_)=>tray.SelectedItems.Clear()),true);
        board.PointerPressed+=(_,e)=>
        {
            if(session.Busy||dialogOpen||albumOpen||session.Loupe)return;
            var p=e.GetCurrentPoint(board);if(p.Properties.IsRightButtonPressed)return;
            if(p.PointerDeviceType==PointerDeviceType.Touch)return;
            if(gesturePointer!=null||!p.Properties.IsLeftButtonPressed&&!p.Properties.IsMiddleButtonPressed)return;
            press=last=Pt(p.Position);dragPanStart=new(session.Document.Viewport.OffsetX,session.Document.Viewport.OffsetY);pressedId=renderer.Hit(press.Value);dragging=false;
            marquee=Down(VirtualKey.Shift)&&!Down(VirtualKey.Control);panning=p.Properties.IsMiddleButtonPressed||pressedId==null&&!marquee;
            rotating=Down(VirtualKey.Menu)&&pressedId!=null&&!panning&&!marquee;resizing=false;
            if(pressedId is {} id&&!marquee&&!panning)
            {
                if(Down(VirtualKey.Control))session.Select(id,true);
                else if(!session.Selection.Contains(id))session.Select(id,false,false);
                else {session.Document.BringToFront(id);session.ShowSelection=false;}
                if(session.Document.Slides.Find(s=>s.Id==id) is {} slide)
                {
                    var placement=Geometry.Place(session.Document,slide);var local=placement.Local(press.Value);
                    resizing=Math.Abs(local.X-placement.Width/2)<12&&Math.Abs(local.Y-placement.Height/2)<12;
                    initialAngle=slide.Angle;initialWidth=slide.Width;
                }
            }
            dragOrigins=session.Document.Slides.Where(s=>session.Selection.Contains(s.Id)).ToDictionary(s=>s.Id,s=>new PointD(s.X*session.Document.WorldWidth,s.Y*session.Document.WorldHeight));
            foreach(var a in session.Document.Albums.Where(a=>session.Selection.Contains(a.Id)&&a.Position!=null))dragOrigins[a.Id]=a.Position!.Value;
            if(!board.CapturePointer(e.Pointer)){CancelGesture();return;}
            gesturePointer=e.Pointer.PointerId;board.Focus(FocusState.Pointer);e.Handled=true;TraceTableInput("press");
        };
        board.PointerMoved+=(_,e)=>
        {
            var current=e.GetCurrentPoint(board);var pointer=Pt(current.Position);renderer.Pointer=pointer;renderer.PointerInside=true;
            if(gesturePointer==e.Pointer.PointerId&&!current.Properties.IsLeftButtonPressed&&!current.Properties.IsMiddleButtonPressed){CancelGesture();board.ReleasePointerCaptures();}
            if(gesturePointer==e.Pointer.PointerId&&press is {} origin&&last is {} prev)
            {
                var delta=pointer-origin;if(!dragging&&Math.Sqrt(delta.X*delta.X+delta.Y*delta.Y)>3){dragging=true;session.ShowSelection=false;}
                if(dragging)
                {
                    if(marquee){renderer.Marquee=RectD.Between(origin,pointer);renderer.SelectMarquee(renderer.Marquee.Value);}
                    else if(panning){session.Document.Viewport.OffsetX+=pointer.X-prev.X;session.Document.Viewport.OffsetY+=pointer.Y-prev.Y;}
                    else if(rotating&&pressedId is {} rotationId&&session.Document.Slides.Find(s=>s.Id==rotationId) is {} rs){rs.Angle=initialAngle+delta.X*.5;rs.Sanitize();}
                    else if(resizing&&pressedId is {} resizeId&&session.Document.Slides.Find(s=>s.Id==resizeId) is {} ss){ss.Width=initialWidth*Math.Exp(delta.X*.008);ss.Sanitize();}
                    else
                    {
                        var worldDelta=(delta+dragPanStart-new PointD(session.Document.Viewport.OffsetX,session.Document.Viewport.OffsetY))*(1/session.Document.Viewport.Scale);
                        foreach(var s in session.Document.Slides.Where(s=>dragOrigins.ContainsKey(s.Id))){var w=dragOrigins[s.Id]+worldDelta;s.X=w.X/session.Document.WorldWidth;s.Y=w.Y/session.Document.WorldHeight;}
                        foreach(var a in session.Document.Albums.Where(a=>dragOrigins.ContainsKey(a.Id)))a.Position=dragOrigins[a.Id]+worldDelta;
                    }
                    session.Notify();
                }
                last=pointer;
            }
            board.Invalidate();
        };
        board.PointerReleased+=(_,e)=>
        {
            if(gesturePointer!=e.Pointer.PointerId)return;
            if(press!=null&&!dragging&&!marquee&&!Down(VirtualKey.Control))
            {
                if(pressedId is {} id)session.Select(id);else{session.Selection.Clear();session.ShowSelection=false;session.Notify(false);}
            }
            TraceTableInput("release");CancelGesture();board.ReleasePointerCaptures();
        };
        board.PointerCaptureLost+=(_,e)=>{if(gesturePointer==e.Pointer.PointerId)CancelGesture();};
        board.PointerCanceled+=(_,e)=>{if(gesturePointer==e.Pointer.PointerId)CancelGesture();touchManipulation=null;};
        board.PointerExited+=(_,_)=>{if(!session.CloseViewing)renderer.PointerInside=false;board.Invalidate();};
        board.PointerWheelChanged+=(_,e)=>
        {
            if(dialogOpen||session.Busy)return;
            var p=e.GetCurrentPoint(board);var delta=p.Properties.MouseWheelDelta;
            if(Down(VirtualKey.Control))Zoom(Math.Exp(delta*.0015),Pt(p.Position));
            else if(Down(VirtualKey.Menu)){session.TransformSelection(rotation:delta/120d*3);}
            else if(Down(VirtualKey.Shift)||p.Properties.IsHorizontalMouseWheel)session.Document.Viewport.OffsetX+=delta*.4;
            else session.Document.Viewport.OffsetY+=delta*.4;
            session.Notify();e.Handled=true;
        };
        board.RightTapped+=(_,e)=>
        {
            if(session.Loupe)return;var id=renderer.Hit(Pt(e.GetPosition(board)));if(id!=null&&!session.Selection.Contains(id.Value))session.Select(id.Value);
            var f=new MenuFlyout();
            if(id!=null)
            {
                MenuItem(f,"收回底栏",()=>session.RemoveSelection());MenuItem(f,"摆正",()=>session.TransformSelection(straighten:true));
                MenuItem(f,"逆时针旋转 15°",()=>session.TransformSelection(rotation:-15));MenuItem(f,"顺时针旋转 15°",()=>session.TransformSelection(rotation:15));
                MenuItem(f,"缩小胶片",()=>session.TransformSelection(1/1.12));MenuItem(f,"放大胶片",()=>session.TransformSelection(1.12));
                if(session.Document.Albums.Any(a=>a.Id==id))MenuItem(f,"编辑相册页",async()=>await ShowAlbums(id));
            }
            else{MenuItem(f,"一键平铺",()=>session.Fit(true));MenuItem(f,"整理全局",()=>session.Fit());}
            f.ShowAt(board,e.GetPosition(board));e.Handled=true;
        };
        board.DragOver+=(_,e)=>{if(e.DataView.Properties.ContainsKey("LightTable.Path")||e.DataView.Contains(StandardDataFormats.StorageItems))e.AcceptedOperation=DataPackageOperation.Copy;};
        board.Drop+=async(_,e)=>
        {
            if(session.Busy||dialogOpen||albumOpen||collectionCancellation!=null)return;
            var at=Pt(e.GetPosition(board));var anchor=DropAnchor.Capture(session.Document);var boardWidth=session.Width;
            if(e.DataView.Properties.TryGetValue("LightTable.Album",out var a)&&Guid.TryParse(a as string,out var id)&&session.Document.Albums.Find(p=>p.Id==id) is {} album)session.PlaceAlbum(album,at);
            else if(e.DataView.Properties.TryGetValue("LightTable.Path",out var p)&&p is string path&&session.Document.Photos.Any(photo=>StringComparer.OrdinalIgnoreCase.Equals(photo.Path,path)))session.Place(path,at);
            else if(e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var input=await e.DataView.GetStorageItemsAsync();var imported=await session.ImportAsync(input.Select(i=>i.Path));int i=0;
                if(imported!=null)foreach(var photo in imported.AcceptedPaths){var placed=anchor.Place(session.Document,photo,at+new PointD(i*.035*boardWidth,0));session.Select(placed.Id,false,false);i++;}
                status.Text=session.Document.SourceLabel;session.Notify();
            }
        };
        root.AddHandler(UIElement.PreviewKeyDownEvent,new KeyEventHandler(KeyDown),true);
        root.AddHandler(UIElement.PreviewKeyUpEvent,new KeyEventHandler(KeyUp),true);
        board.ManipulationMode=ManipulationModes.Scale|ManipulationModes.Rotate|ManipulationModes.TranslateX|ManipulationModes.TranslateY;
        board.ManipulationStarting+=(_,e)=>e.Container=board;
        board.ManipulationStarted+=(_,e)=>
        {
            touchManipulation=null;
            // Mouse/pen already use the captured-pointer route above. Processing
            // their manipulation events too applies a second, selection-based move.
            if(e.PointerDeviceType!=PointerDeviceType.Touch||session.Loupe||dialogOpen||albumOpen||session.Busy){e.Complete();return;}
            var hit=renderer.Hit(Pt(e.Position));
            if(hit is {} id&&!session.Selection.Contains(id))session.Select(id,false,false);
            touchManipulation=new(hit,session.Selection);e.Handled=true;
        };
        board.ManipulationDelta+=(_,e)=>
        {
            if(e.PointerDeviceType!=PointerDeviceType.Touch||touchManipulation==null||session.Loupe||dialogOpen||albumOpen||session.Busy)return;
            touchManipulation.Apply(session.Document,Pt(e.Position),new(e.Delta.Translation.X,e.Delta.Translation.Y),e.Delta.Scale,e.Delta.Rotation);
            session.Notify();e.Handled=true;
        };
        board.ManipulationCompleted+=(_,_)=>{TraceTableInput("manipulation-completed");touchManipulation=null;};
    }
    void CancelGesture(){press=last=null;pressedId=null;gesturePointer=null;touchManipulation=null;dragOrigins.Clear();dragging=marquee=panning=rotating=resizing=false;renderer.Marquee=null;board.Invalidate();}
    void TraceTableInput(string stage)
    {
        if(!traceViewingInput)return;
        session.Storage.Log("table-input "+System.Text.Json.JsonSerializer.Serialize(new{Stage=stage,Pressed=pressedId,Dragging=dragging,Panning=panning,session.Document.Viewport,Slides=session.Document.Slides.Select(s=>new{s.Id,s.X,s.Y,s.Width,s.Angle}),Albums=session.Document.Albums.Select(a=>new{a.Id,a.Position,a.Scale,a.Angle})}));
    }
    void TickTableDrag(double seconds)
    {
        if(!dragging||panning||marquee||rotating||resizing||last is not {} pointer||pressedId==null)return;
        var velocity=SleeveMotion.EdgeVelocity(pointer,new(session.Width,session.Height));if(velocity.X==0&&velocity.Y==0)return;
        var delta=velocity*seconds;session.Document.Viewport.OffsetX+=delta.X;session.Document.Viewport.OffsetY+=delta.Y;var world=delta*(1/session.Document.Viewport.Scale);
        foreach(var slide in session.Document.Slides.Where(s=>session.Selection.Contains(s.Id))){slide.X-=world.X/session.Document.WorldWidth;slide.Y-=world.Y/session.Document.WorldHeight;}
        foreach(var page in session.Document.Albums.Where(a=>session.Selection.Contains(a.Id)&&a.Position!=null))page.Position-=world;session.Notify();
    }
    void KeyDown(object sender,KeyRoutedEventArgs e)
    {
        try{HandleKeyDown(e);}finally{TraceViewingKey("down",e);}
    }
    void HandleKeyDown(KeyRoutedEventArgs e)
    {
        if(collectionCancellation!=null){if(e.Key==VirtualKey.Escape)collectionCancellation.Cancel();e.Handled=true;return;}
        if(renderer.Flight!=null){if(e.Key==VirtualKey.Escape){CancelTrayFlight();e.Handled=true;}return;}
        if(dialogOpen||albumOpen||startup.Visibility==Visibility.Visible||session.Busy)return;
        if(Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(root.XamlRoot) is TextBox or PasswordBox)return;
        bool ctrl=Down(VirtualKey.Control),shift=Down(VirtualKey.Shift);
        if(ctrl&&e.Key==VirtualKey.O){if(shift)_=ChoosePhotos();else _=ChooseFolder();e.Handled=true;return;}
        if(ctrl&&e.Key==VirtualKey.L){if(!e.KeyStatus.WasKeyDown)session.ToggleLight(TableRenderer.Seconds);e.Handled=true;return;}
        if(ctrl)return;
        switch(e.Key)
        {
            case VirtualKey.A:session.Loupe=false;session.CloseViewing=false;break;
            case VirtualKey.V:session.Loupe=true;break;
            case VirtualKey.Tab:if(!session.Loupe)return;if(tabHeld){e.Handled=true;return;}tabHeld=true;session.Power=LoupePowers.Next(session.Power);break;
            case VirtualKey.Space:if(!session.Loupe)return;spaceHeld=true;if(renderer.PointerInside||session.CloseViewing)session.CloseViewing=true;break;
            case VirtualKey.Z:if(!shift)return;session.Fit();break;
            case VirtualKey.Delete:if(collectionPicker.FocusState!=FocusState.Unfocused)return;if(FocusIsInTray()&&tray.SelectedItems.OfType<TrayItem>().Any(i=>i.AlbumId==null))RemoveLibrarySelection();else session.RemoveSelection();break;
            case VirtualKey.Escape:tray.SelectedItems.Clear();session.CloseViewing=false;session.Selection.Clear();CancelGesture();break;
            default:return;
        }
        e.Handled=true;session.Notify(false);
    }
    void KeyUp(object sender,KeyRoutedEventArgs e)
    {
        if(e.Key==VirtualKey.Tab){if(tabHeld)e.Handled=true;tabHeld=false;}
        if(e.Key==VirtualKey.Space&&(spaceHeld||session.Loupe&&!dialogOpen&&!albumOpen&&FocusManager.GetFocusedElement(root.XamlRoot) is not TextBox and not PasswordBox))
        {spaceHeld=false;session.CloseViewing=false;e.Handled=true;session.Notify(false);}
        TraceViewingKey("up",e);
    }
    void TraceViewingKey(string stage,KeyRoutedEventArgs e)
    {
        if(!traceViewingInput||e.Key is not (VirtualKey.Space or VirtualKey.Tab))return;
        session.Storage.Log($"view-key {stage} key={e.Key} focus={FocusManager.GetFocusedElement(root.XamlRoot)?.GetType().Name} handled={e.Handled} held={spaceHeld} loupe={session.Loupe} close={session.CloseViewing} lit={session.IsLit} power={LoupePowers.Scale(session.Power)}");
    }
}
