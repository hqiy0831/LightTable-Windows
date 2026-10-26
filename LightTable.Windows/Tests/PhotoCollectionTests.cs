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
        restored.ActiveCollectionId=first.Id;Assert.Single(restored.ActivePhotos);
    }
    [Fact] public void RemovingMembershipPreservesOtherColumnsPlacedPhotoAndOriginalBytes()
    {
        var path=Path.GetTempFileName();try
        {
            File.WriteAllBytes(path,[1,7,4,9]);var p=new Photo{Path=path};var doc=new TableDocument{Photos=[p],Slides=[new(){Path=path}]};var first=doc.EnsureCollection();var second=doc.AddCollection();doc.SetCollectionPhotos(second,[p],false);
            Assert.Equal(1,doc.RemoveFromCollection([path]));Assert.Empty(doc.ActivePhotos);Assert.Single(first.Paths);Assert.Single(doc.Slides);Assert.Equal(new byte[]{1,7,4,9},File.ReadAllBytes(path));
            doc.Sanitize();Assert.Empty(doc.ActivePhotos);Assert.Single(doc.Photos);
        }
        finally{File.Delete(path);}
    }
    [Fact] public void ReplaceAndFitOnlyUseTheActiveColumn()
    {
        var a=Photo("a.jpg");var b=Photo("b.jpg");var c=Photo("c.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();var second=doc.AddCollection();doc.SetCollectionPhotos(second,[b],false);doc.SetCollectionPhotos(second,[c],true);
        Geometry.Fit(doc,1180,550,true);Assert.Equal(c.Path,Assert.Single(doc.Slides).Path);Assert.Equal(a.Path,Assert.Single(first.Paths));
        doc.ActiveCollectionId=first.Id;Geometry.Fit(doc,1180,550,true);Assert.Equal(a.Path,Assert.Single(doc.Slides).Path);
    }
    [Fact] public void InvalidMembershipAndActiveSelectionAreRepairedWithoutFillingEmptyColumns()
    {
        var a=Photo("a.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();var empty=doc.AddCollection();first.Paths.Add(a.Path.ToUpperInvariant());first.Paths.Add("https://bad.invalid/a.jpg");doc.ActiveCollectionId=Guid.NewGuid();doc.Sanitize();
        Assert.Equal(first.Id,doc.ActiveCollectionId);Assert.Single(first.Paths);Assert.Empty(empty.Paths);
    }
}
