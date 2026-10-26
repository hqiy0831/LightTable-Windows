using LightTable.Core;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace LightTable.Windows;

sealed class MagnificationSwitch : Grid
{
    const double SegmentWidth=48;
    readonly TranslateTransform position=new();
    readonly SmoothTransitionClock clock=new();
    readonly Button[] buttons=new Button[LoupePowers.Count];
    uint? pointerId;
    public LoupePower Power {get;private set;}
    public double ThumbPosition=>position.X;
    public event Action<LoupePower>? Selected;
    public MagnificationSwitch()
    {
        Width=SegmentWidth*LoupePowers.Count+4;Height=34;CornerRadius=new(17);Background=Brush(231);BorderBrush=Brush(194);BorderThickness=new(.5);
        var thumb=new Border{Width=SegmentWidth,Height=30,CornerRadius=new(15),Background=Brush(24),HorizontalAlignment=HorizontalAlignment.Left,VerticalAlignment=VerticalAlignment.Center,Margin=new(2,0,0,0),RenderTransform=position,IsHitTestVisible=false};Children.Add(thumb);
        var labels=new Grid{Margin=new(2,0,2,0)};
        foreach(var power in Enum.GetValues<LoupePower>())
        {
            int column=(int)power;labels.ColumnDefinitions.Add(new(){Width=new(1,GridUnitType.Star)});
            var title=$"{LoupePowers.Scale(power):0}×";
            var button=new Button{Content=title,FontSize=12,Padding=new(0),MinWidth=0,MinHeight=0,Height=30,CornerRadius=new(15),Background=new SolidColorBrush(Colors.Transparent),BorderThickness=new(0),HorizontalAlignment=HorizontalAlignment.Stretch};
            buttons[column]=button;AutomationProperties.SetName(button,title);Grid.SetColumn(button,column);labels.Children.Add(button);button.Click+=(_,_)=>Request(power);
        }
        Children.Add(labels);
        // One hit surface owns the complete drag. Child Buttons otherwise
        // capture/release independently and can swallow movement across segments.
        Children.Add(new Border{Background=new SolidColorBrush(Colors.Transparent)});
        AutomationProperties.SetName(this,"放大镜倍率");ToolTipService.SetToolTip(this,"倍率 1× / 2× / 4× / 8× · Tab 循环，也可点击或横向拖动");
        AddHandler(PointerPressedEvent,new PointerEventHandler((_,e)=>
        {
            var p=e.GetCurrentPoint(this);if(!p.Properties.IsLeftButtonPressed||pointerId!=null)return;
            if(!CapturePointer(e.Pointer))return;
            pointerId=e.Pointer.PointerId;Request(At(p.Position.X));e.Handled=true;
        }),true);
        AddHandler(PointerMovedEvent,new PointerEventHandler((_,e)=>
        {
            if(pointerId!=e.Pointer.PointerId)return;
            var p=e.GetCurrentPoint(this);if(!p.Properties.IsLeftButtonPressed){pointerId=null;ReleasePointerCaptures();return;}
            Request(At(p.Position.X));e.Handled=true;
        }),true);
        AddHandler(PointerReleasedEvent,new PointerEventHandler((_,e)=>
        {
            if(pointerId!=e.Pointer.PointerId)return;
            Request(At(e.GetCurrentPoint(this).Position.X));pointerId=null;ReleasePointerCaptures();e.Handled=true;
        }),true);
        PointerCaptureLost+=(_,e)=>{if(ReferenceEquals(e.OriginalSource,this))pointerId=null;};SetValue(LoupePower.One,false);
    }
    LoupePower At(double x)=>LoupePowers.At(x-2,ActualWidth-4);
    static SolidColorBrush Brush(byte gray)=>new(global::Windows.UI.Color.FromArgb(255,gray,gray,gray));
    void Request(LoupePower value){if(value==Power)return;SetValue(value,true);Selected?.Invoke(value);}
    public void SetValue(LoupePower value,bool animate)
    {
        Power=LoupePowers.Validate(value);clock.Target((int)Power,Rendering.TableRenderer.Seconds,animate?LoupeGeometry.FocalDuration:0);
        for(int i=0;i<buttons.Length;i++)
        {
            bool selected=i==(int)Power;buttons[i].Foreground=Brush(selected?(byte)255:(byte)92);
            buttons[i].FontWeight=selected?Microsoft.UI.Text.FontWeights.SemiBold:Microsoft.UI.Text.FontWeights.Normal;
            AutomationProperties.SetItemStatus(buttons[i],selected?"已选中":"未选中");
        }
        Advance(Rendering.TableRenderer.Seconds,!animate);
    }
    public void Advance(double now,bool reducedMotion)
    {
        if(reducedMotion)clock.Target((int)Power,now,0);
        position.X=SegmentWidth*clock.Value(now);
    }
}
