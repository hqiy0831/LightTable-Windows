using System.Text.Json;
using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class PhotoCollectionTests
{
    static Photo Photo(string name)=>new(){Path=Path.GetFullPath(name),Aspect=1.7};
    [Fact] public void LegacyLayoutMigratesWithoutLosingOrderingOrPlacement()
    {
        var a=Photo("a.jpg");var b=Photo("b.jpg");
        var doc=new TableDocument{SchemaVersion=1,Photos=[a,b],SourceLabel="旧图库",Slides=[new(){Path=a.Path,X=.7,Angle=21}]};
        doc.Sanitize();Assert.Equal(TableDocument.CurrentSchema,doc.SchemaVersion);Assert.Single(doc.Collections);
        Assert.Equal(new[]{a.Path,b.Path},doc.EnsureCollection().Paths);Assert.Equal("旧图库",doc.EnsureCollection().SourceLabel);Assert.Equal(21,doc.Slides[0].Angle);
        doc.Sanitize();Assert.Single(doc.Collections);Assert.Equal(2,doc.ActivePhotos.Count);
    }
    [Fact] public void NewCollectionStartsEmptyAndSurvivesSaveRestore()
    {
        var a=Photo("a.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();var second=doc.AddCollection("第二组");
        Assert.Empty(doc.ActivePhotos);doc.SetCollectionPhotos(second,[a,Photo("b.jpg")],false);Assert.Single(first.Paths);Assert.Equal(2,doc.ActivePhotos.Count);
        var restored=JsonSerializer.Deserialize<TableDocument>(JsonSerializer.Serialize(doc))!;restored.Sanitize();
        Assert.Equal(second.Id,restored.ActiveCollectionId);Assert.Equal("第二组",restored.EnsureCollection().Name);Assert.Equal(2,restored.ActivePhotos.Count);
        restored.SwitchCollection(first.Id);Assert.Single(restored.ActivePhotos);
    }
    [Fact] public void RemovingMembershipPreservesOtherColumnsPlacedPhotoAndOriginalBytes()
    {
        var path=Path.GetTempFileName();try
        {
            File.WriteAllBytes(path,[1,7,4,9]);var p=new Photo{Path=path};var doc=new TableDocument{Photos=[p],Slides=[new(){Path=path}]};var first=doc.EnsureCollection();var second=doc.AddCollection();doc.SetCollectionPhotos(second,[p],false);
            doc.Place(path,new(200,100));Assert.Equal(1,doc.RemoveFromCollection([path]));Assert.Empty(doc.ActivePhotos);Assert.Single(first.Paths);Assert.Single(doc.Slides);Assert.Equal(new byte[]{1,7,4,9},File.ReadAllBytes(path));
            doc.Sanitize();Assert.Empty(doc.ActivePhotos);Assert.Single(doc.Photos);
        }
        finally{File.Delete(path);}
    }
    [Fact] public void ReplaceAndFitOnlyUseTheActiveColumn()
    {
        var a=Photo("a.jpg");var b=Photo("b.jpg");var c=Photo("c.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();var second=doc.AddCollection();doc.SetCollectionPhotos(second,[b],false);doc.SetCollectionPhotos(second,[c],true);
        Geometry.Fit(doc,1180,550,true);Assert.Equal(c.Path,Assert.Single(doc.Slides).Path);Assert.Equal(a.Path,Assert.Single(first.Paths));
        doc.SwitchCollection(first.Id);Geometry.Fit(doc,1180,550,true);Assert.Equal(a.Path,Assert.Single(doc.Slides).Path);
    }
    [Fact] public void InvalidMembershipAndActiveSelectionAreRepairedWithoutFillingEmptyColumns()
    {
        var a=Photo("a.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();var empty=doc.AddCollection();first.Paths.Add(a.Path.ToUpperInvariant());first.Paths.Add("https://bad.invalid/a.jpg");doc.ActiveCollectionId=Guid.NewGuid();doc.Sanitize();
        Assert.Equal(first.Id,doc.ActiveCollectionId);Assert.Single(first.Paths);Assert.Empty(empty.Paths);
    }
    [Fact] public void WorkspaceSwitchRestoresIndependentSlidesAlbumsAndCamera()
    {
        var a=Photo("a.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();
        var slide=doc.Place(a.Path,new(220,70));slide.Angle=31;doc.Viewport.Zoom(2,new(100,100));var firstOffset=doc.Viewport.OffsetX;
        var album=new AlbumPage{Name="第一栏相册",Position=new(30,40)};doc.Albums.Add(album);doc.BringToFront(album.Id);var order=doc.TableIds.ToArray();
        var second=doc.AddCollection();Assert.Empty(doc.Slides);Assert.Empty(doc.Albums);Assert.Empty(doc.ActivePhotos);Assert.Equal(1,doc.Viewport.Scale);
        doc.SetCollectionPhotos(second,[a],false);var secondSlide=doc.Place(a.Path,new(500,200));secondSlide.Angle=83;doc.Viewport.Zoom(.5,new(50,50));
        doc.SwitchCollection(first.Id);Assert.Same(slide,Assert.Single(doc.Slides));Assert.Equal(31,doc.Slides[0].Angle);Assert.Equal(order,doc.TableIds);Assert.Same(album,Assert.Single(doc.Albums));Assert.Equal(2,doc.Viewport.Scale);Assert.Equal(firstOffset,doc.Viewport.OffsetX);
        doc.SwitchCollection(second.Id);Assert.Same(secondSlide,Assert.Single(doc.Slides));Assert.Empty(doc.Albums);Assert.Equal(83,doc.Slides[0].Angle);Assert.Equal(.5,doc.Viewport.Scale);
        var third=doc.AddCollection();Assert.Empty(doc.Slides);doc.SwitchCollection(first.Id);Assert.Single(doc.Slides);doc.SwitchCollection(third.Id);Assert.Empty(doc.Slides);
    }
    [Fact] public void EveryWorkspaceSurvivesSaveAndReloadIncludingTheInactiveOnes()
    {
        var a=Photo("a.jpg");var b=Photo("b.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();doc.Place(a.Path,new(300,100));var second=doc.AddCollection();doc.SetCollectionPhotos(second,[b],false);doc.Place(b.Path,new(800,200));
        var restored=JsonSerializer.Deserialize<TableDocument>(JsonSerializer.Serialize(doc))!;restored.Sanitize();
        Assert.Equal(b.Path,Assert.Single(restored.Slides).Path);restored.SwitchCollection(first.Id);Assert.Equal(a.Path,Assert.Single(restored.Slides).Path);Assert.Equal(a.Path,Assert.Single(restored.ActivePhotos).Path);
        restored.SwitchCollection(second.Id);Assert.Equal(b.Path,Assert.Single(restored.Slides).Path);
    }
    [Fact] public void SchemaTwoSharedTableMigratesOnlyIntoThePreviouslyActiveColumn()
    {
        var a=Photo("a.jpg");var b=Photo("b.jpg");var first=new PhotoCollection{Paths=[a.Path]};var second=new PhotoCollection{Paths=[b.Path]};
        var doc=new TableDocument{SchemaVersion=2,Photos=[a,b],Collections=[first,second],ActiveCollectionId=first.Id,Slides=[new(){Path=a.Path,Angle=37}]};
        doc.Sanitize();Assert.Equal(37,Assert.Single(doc.Slides).Angle);doc.SwitchCollection(second.Id);Assert.Empty(doc.Slides);Assert.Equal(b.Path,Assert.Single(doc.ActivePhotos).Path);doc.SwitchCollection(first.Id);Assert.Equal(a.Path,Assert.Single(doc.Slides).Path);
    }
    [Fact] public void LegacySharedTableIsPartitionedEvenWhenSecondColumnWasActive()
    {
        var a=Photo("a.jpg");var b=Photo("b.jpg");var first=new PhotoCollection{Paths=[a.Path]};var second=new PhotoCollection{Paths=[b.Path]};
        var doc=new TableDocument{SchemaVersion=2,Photos=[a,b],Collections=[first,second],ActiveCollectionId=second.Id,Slides=[new(){Path=a.Path},new(){Path=b.Path}]};
        doc.Sanitize();Assert.Equal(b.Path,Assert.Single(doc.Slides).Path);doc.SwitchCollection(first.Id);Assert.Equal(a.Path,Assert.Single(doc.Slides).Path);
    }
}
