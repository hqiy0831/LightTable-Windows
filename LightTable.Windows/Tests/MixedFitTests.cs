using System.Text.Json;
using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class MixedFitTests
{
    [Fact] public void SlowDropUsesReleaseCameraAndReferenceEvenAfterLaterViewChanges()
    {
        var document=new TableDocument{WorldWidth=1480,WorldHeight=880,Viewport=new(){Scale=1.7,OffsetX=-230,OffsetY=81}};
        var at=new PointD(400,250);var originalWorld=document.Viewport.World(at);var anchor=DropAnchor.Capture(document);
        document.Viewport.Zoom(4,new(20,20));Geometry.Fit(document,760,500);var slide=anchor.Place(document,Path.GetFullPath("drop.jpg"),at);
        Assert.Equal(originalWorld.X/1480,slide.X,9);Assert.Equal(originalWorld.Y/880,slide.Y,9);
        var repeated=anchor.Place(document,slide.Path,at+new PointD(30,0));Assert.Same(slide,repeated);Assert.Single(document.Slides);
    }
    [Theory][InlineData(1)][InlineData(40)][InlineData(300)]
    public void RotatedAlbumsAndFilmsFitAndRetainTheirScaleAfterRecovery(int count)
    {
        var doc=new TableDocument();for(int i=0;i<count;i++)doc.Albums.Add(new(){Angle=i*17%180,Position=new(-5000,7000),Pockets=[new(){Format=FilmFormat.Film135}]});
        var path=Path.GetFullPath("fixture.jpg");doc.Photos.Add(new(){Path=path,Aspect=.7});doc.Place(path,new(400,200));doc.Slides[0].Angle=31;
        var order=doc.TableIds.ToArray();Geometry.Fit(doc,1180,550);
        foreach(var page in doc.Albums)
        {
            var p=Geometry.Place(doc,page);foreach(var x in new[]{-p.Width/2,p.Width/2})foreach(var y in new[]{-p.Height/2,p.Height/2})
            {var corner=p.Center+Geometry.Rotate(new(x,y),p.Angle);Assert.InRange(corner.X,-1e-6,1180+1e-6);Assert.InRange(corner.Y,-1e-6,550+1e-6);}
        }
        var copy=JsonSerializer.Deserialize<TableDocument>(JsonSerializer.Serialize(doc))!;copy.Sanitize();Assert.Equal(order,copy.TableIds);
        Assert.Equal(doc.Albums.Select(p=>p.Scale),copy.Albums.Select(p=>p.Scale));Assert.All(copy.Albums,p=>Assert.Single(p.Pockets));Assert.Equal(31,copy.Slides[0].Angle);
    }
}
