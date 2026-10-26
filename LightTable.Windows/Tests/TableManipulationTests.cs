using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class TableManipulationTests
{
    [Fact] public void SelectedPhotoDoesNotFollowBackgroundPanOrPinch()
    {
        var doc=new TableDocument();var slide=doc.Place(Path.GetFullPath("a.jpg"),new(300,200));var album=new AlbumPage{Position=new(800,400)};doc.Albums.Add(album);
        var before=(slide.X,slide.Y,slide.Width,slide.Angle,album.Position,album.Scale,album.Angle);
        var gesture=new TableManipulation(null,[slide.Id,album.Id]);Assert.False(gesture.MovesItems);
        for(int i=0;i<20;i++)gesture.Apply(doc,new(500,250),new(8,-3),1.01,2);
        Assert.Equal(before,(slide.X,slide.Y,slide.Width,slide.Angle,album.Position,album.Scale,album.Angle));
        Assert.NotEqual(1,doc.Viewport.Scale);Assert.NotEqual(0,doc.Viewport.OffsetX);
    }
    [Fact] public void HitPhotoMovesSelectedGroupOnceAndLeavesCameraFixed()
    {
        var doc=new TableDocument();doc.Viewport.Scale=2;
        var slide=doc.Place(Path.GetFullPath("a.jpg"),new(300,200));var other=doc.Place(Path.GetFullPath("b.jpg"),new(900,500));
        var album=new AlbumPage{Position=new(800,400)};doc.Albums.Add(album);
        var x=slide.X;var y=slide.Y;var width=slide.Width;var before=(other.X,other.Y);
        var selection=new HashSet<Guid>{slide.Id,album.Id};var gesture=new TableManipulation(slide.Id,selection);selection.Clear();selection.Add(other.Id);
        gesture.Apply(doc,new(100,100),new(40,-20),1.1,15);
        Assert.Equal(x+20/doc.WorldWidth,slide.X,8);Assert.Equal(y-10/doc.WorldHeight,slide.Y,8);Assert.Equal(width*1.1,slide.Width,8);Assert.Equal(15,slide.Angle);
        Assert.Equal(new PointD(820,390),album.Position);Assert.Equal(15,album.Angle);Assert.Equal(before,(other.X,other.Y));
        Assert.Equal(2,doc.Viewport.Scale);Assert.Equal(0,doc.Viewport.OffsetX);Assert.Equal(0,doc.Viewport.OffsetY);
    }
    [Fact] public void HitUnselectedItemDoesNotMoveOldSelection()
    {
        var doc=new TableDocument();var first=doc.Place(Path.GetFullPath("a.jpg"),new(100,100));var second=doc.Place(Path.GetFullPath("b.jpg"),new(400,300));
        var before=(first.X,first.Y);var gesture=new TableManipulation(second.Id,[first.Id]);
        gesture.Apply(doc,new(300,200),new(50,25),1,0);Assert.Equal(before,(first.X,first.Y));Assert.Equal(450/doc.WorldWidth,second.X,8);
    }
}
