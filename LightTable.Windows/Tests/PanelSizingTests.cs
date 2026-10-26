using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class PanelSizingTests
{
    [Fact] public void DragLimitsPreserveBothPanelsAcrossWindowSizes()
    {
        foreach(var window in new[]{340d,500,900,1400})foreach(var request in new[]{-300d,0,100,180,400,9000})
        {var result=PanelSizing.TrayHeight(request,window);Assert.InRange(result,120,600);Assert.True(result<=Math.Max(120,window-220));}
    }
    [Fact] public void DefaultAndInvalidSettingsProduceUsableCompactPanels()
    {Assert.Equal(180,PanelSizing.TrayHeight(double.NaN,900));Assert.Equal(152,PanelSizing.TrayHeight(-100,900));Assert.Equal(180,PanelSizing.TrayHeight(double.PositiveInfinity,900));}
    [Fact] public void ThumbnailSizingStaysBoundedWhileDragging()
    {Assert.Equal(.65,PanelSizing.ThumbnailScale(-50));Assert.Equal(1.45,PanelSizing.ThumbnailScale(500));for(int i=30;i<200;i++)Assert.True(PanelSizing.ThumbnailScale(i+1)>=PanelSizing.ThumbnailScale(i));}
}
