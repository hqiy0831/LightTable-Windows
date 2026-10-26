using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

namespace LightTable.Windows;

sealed class PanelResizeGrip : UserControl
{
    public Thumb Handle {get;}=new(){IsTabStop=true};
    public PanelResizeGrip()
    {
        Height=12;IsTabStop=false;ProtectedCursor=InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth);
        AutomationProperties.SetName(Handle,"拖动调整照片区与底栏高度；上下键微调，Home 恢复默认");
        Handle.Template=(ControlTemplate)XamlReader.Load("""
            <ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='Thumb'>
              <Grid Background='Transparent' Height='12'>
                <Border Height='1' Margin='24,0' Background='#555555' VerticalAlignment='Center'/>
                <Border Height='3' Width='48' CornerRadius='2' Background='#999999' VerticalAlignment='Center' HorizontalAlignment='Center'/>
              </Grid>
            </ControlTemplate>
            """);
        Content=Handle;
    }
}
