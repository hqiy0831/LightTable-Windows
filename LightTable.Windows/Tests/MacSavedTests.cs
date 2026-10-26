using System.Security.Cryptography;
using System.Text.Json;
using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class MacSavedTests : IDisposable
{
    [Fact] public void UpstreamSeventyFiveTraysMigrateIntoIndependentWindowsWorkspaces()
    {
        var a=FileAt("a.jpg",[1,2]);var b=FileAt("b.jpg",[3,4]);var first=Guid.NewGuid();var second=Guid.NewGuid();
        var bytes=JsonSerializer.SerializeToUtf8Bytes(new{urls=Array.Empty<string>(),label="第二栏",trays=new[]{new{id=first,name="第一栏",urls=new[]{Url("/Mac/a.jpg")}},new{id=second,name="第二栏",urls=new[]{Url("/Mac/b.jpg")}}},activeTrayID=second,layout=new{slides=new[]{Slide(Guid.NewGuid(),"/Mac/a.jpg"),Slide(Guid.NewGuid(),"/Mac/b.jpg")}}});
        var result=MacSavedImporter.Import(bytes,[new("/Mac",root)]);Assert.Empty(result.MissingPaths);Assert.Equal(2,result.Document.Collections.Count);Assert.Equal(b,result.Document.Slides.Single().Path);result.Document.SwitchCollection(first);Assert.Equal(a,result.Document.Slides.Single().Path);Assert.Equal(a,result.Document.ActivePhotos.Single().Path);
    }
    readonly string root=Path.Combine(Path.GetTempPath(),"LightTableMigration",Guid.NewGuid().ToString("N"));
    public MacSavedTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    static string Url(string path)=>"file://"+string.Join('/',path.Split('/').Select(Uri.EscapeDataString));
    byte[] Saved(string[] paths,object[]? slides=null,object[]? albums=null,Guid[]? order=null)=>JsonSerializer.SerializeToUtf8Bytes(new{urls=paths.Select(Url),layout=new{slides=slides??[]},label="Mac 中文照片",fitScale=.42,worldWidth=1480,worldHeight=880,viewport=new{scale=1.7,offsetX=-230,offsetY=81},albums=albums??[],tableOrder=order??[]});
    static object Slide(Guid id,string path,double x=.7)=>new{id,url=Url(path),x,y=-1.2,width=.23,angle=37};
    string FileAt(string relative,byte[] data){var path=Path.GetFullPath(Path.Combine(root,relative));Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(path,data);return path;}
    [Fact] public void SwiftSavedRestoresGeometryCameraAlbumsAndMixedOrder()
    {
        var slideId=Guid.NewGuid();var pageId=Guid.NewGuid();var pocketId=Guid.NewGuid();var original=FileAt("照片/风景 01.jpg",[1,2,3]);var onlyAlbum=FileAt("照片/只在相册.png",[4,5]);
        var bytes=Saved(["/Users/mac/Pictures/风景 01.jpg"],[Slide(slideId,"/Users/mac/Pictures/风景 01.jpg")],
            [new{id=pageId,name="135混合页",position=new[]{-420d,2100},pockets=new[]{new{id=pocketId,format="120",column=5,row=4,url=Url("/Users/mac/Pictures/只在相册.png")}}}],[pageId,slideId]);
        var result=MacSavedImporter.Import(bytes,[new("/Users/mac/Pictures",Path.Combine(root,"照片"))]);var doc=result.Document;
        Assert.Empty(result.MissingPaths);Assert.Equal(2,doc.Photos.Count);Assert.Equal(original,doc.Slides[0].Path);Assert.Equal(slideId,doc.Slides[0].Id);Assert.Equal(.7,doc.Slides[0].X);Assert.Equal(-1.2,doc.Slides[0].Y);Assert.Equal(.23,doc.Slides[0].Width);Assert.Equal(37,doc.Slides[0].Angle);
        Assert.Equal(1480,doc.WorldWidth);Assert.Equal(880,doc.WorldHeight);Assert.Equal(.42,doc.FitScale);Assert.Equal(1.7,doc.Viewport.Scale);Assert.Equal(-230,doc.Viewport.OffsetX);Assert.Equal(81,doc.Viewport.OffsetY);
        Assert.Equal(new PointD(-420,2100),doc.Albums[0].Position);Assert.Equal(FilmFormat.Film120,doc.Albums[0].Pockets[0].Format);Assert.Equal(onlyAlbum,doc.Albums[0].Pockets[0].Path);Assert.Equal([pageId,slideId],doc.TableIds);
    }
    [Fact] public void LongestExplicitRootWinsAndMissingReferencesRemain()
    {
        var bytes=Saved(["/Users/mac/Pictures/a.jpg","/Users/mac/Pictures/Archive/missing.jpg"]);
        var result=MacSavedImporter.Import(bytes,[new("/Users/mac/Pictures",root),new("/Users/mac/Pictures/Archive",Path.Combine(root,"old"))]);
        Assert.Equal(Path.Combine(root,"a.jpg"),result.Document.Photos[0].Path);Assert.Equal(Path.Combine(root,"old","missing.jpg"),result.Document.Photos[1].Path);Assert.Equal(2,result.MissingPaths.Count);
    }
    [Fact] public void WholeWindowsDriveIsAValidExplicitDestinationRoot()
    {
        var drive=Path.GetPathRoot(root)!;var result=MacSavedImporter.Import(Saved(["/Volumes/Photos/relative/folder/image.png"]),[new("/Volumes/Photos",drive)]);
        Assert.Equal(Path.Combine(drive,"relative","folder","image.png"),result.Document.Photos[0].Path);
    }
    [Fact] public void OnlyVerifiedLegacyDragCopiesMergeAndUppermostIdentitySurvives()
    {
        var drag="com.apple.SwiftUI.Drag-"+Guid.NewGuid();var other="com.apple.SwiftUI.Drag-"+Guid.NewGuid();var orphan="com.apple.SwiftUI.Drag-"+Guid.NewGuid();
        string original="/Users/mac/Pictures/one.png",cached=$"/Users/mac/Library/Caches/{drag}/one.png",different=$"/Users/mac/Library/Caches/{other}/one.png",genuine="/Users/mac/Other/one.png",missing=$"/Users/mac/Library/Caches/{orphan}/orphan.png";
        FileAt("Pictures/one.png",[1,2,3]);FileAt($"Library/Caches/{drag}/one.png",[1,2,3]);FileAt($"Library/Caches/{other}/one.png",[1,2,4]);FileAt("Other/one.png",[1,2,3]);FileAt($"Library/Caches/{orphan}/orphan.png",[9]);
        var old=Guid.NewGuid();var top=Guid.NewGuid();var distinct=Guid.NewGuid();var bytes=Saved([original,cached,different,genuine,missing],[Slide(old,original,.2),Slide(distinct,different),Slide(top,cached,.72)]);
        var result=MacSavedImporter.Import(bytes,[new("/Users/mac",root)]);Assert.Equal(1,result.LegacyAliases);Assert.Equal(4,result.Document.Photos.Count);Assert.Equal(2,result.Document.Slides.Count);
        Assert.Equal(top,result.Document.Slides[^1].Id);Assert.Equal(.72,result.Document.Slides[^1].X);Assert.Equal(Path.Combine(root,"Pictures","one.png"),result.Document.Slides[^1].Path);Assert.Equal(distinct,result.Document.Slides[0].Id);
        Assert.Equal([1,2,3],File.ReadAllBytes(Path.Combine(root,"Library","Caches",drag,"one.png")));
    }
    [Theory][InlineData("/Users/mac/PicturesX/photo.jpg")][InlineData("/Users/mac/Pictures/../../etc/photo.jpg")][InlineData("/Users/mac/Pictures/CON.jpg")][InlineData("/Users/mac/Pictures/photo.jpg:stream")]
    public void PrefixSiblingsTraversalAndUnsafeWindowsNamesCannotEscape(string path)
        =>Assert.Throws<InvalidDataException>(()=>MacSavedImporter.Import(Saved([path]),[new("/Users/mac/Pictures",root)]));
    [Fact] public void RemoteUrlsAndUnrelatedJsonAreRejected()
    {
        Assert.Throws<InvalidDataException>(()=>MacSavedImporter.Import("{}"u8.ToArray(),[new("/",root)]));
        var remote=JsonSerializer.SerializeToUtf8Bytes(new{urls=new[]{"https://example.test/photo.jpg"},layout=new{slides=Array.Empty<object>()},label="bad"});
        Assert.Throws<InvalidDataException>(()=>MacSavedImporter.Import(remote,[new("/",root)]));
    }
    [Fact] public void CaseCollisionIsReportedInsteadOfDroppingDistinctMacFiles()
        =>Assert.Throws<InvalidDataException>(()=>MacSavedImporter.Import(Saved(["/Photos/a.jpg","/Photos/A.jpg"]),[new("/Photos",root)]));
    [Fact] public void ImportArchivesExactMacAndWindowsBytesBeforeAtomicSave()
    {
        var store=new DocumentStore(Path.Combine(root,"data"));store.Save(new(){SourceLabel="Windows old"});var oldBytes=File.ReadAllBytes(store.DocumentPath);
        var source=Saved(["/Photos/missing.jpg"]);var hash=SHA256.HashData(source);var converted=MacSavedImporter.Import(source,[new("/Photos",root)]);
        var archive=store.Import(converted.Document,source);Assert.Equal(source,File.ReadAllBytes(Path.Combine(archive,"macOS-layout-v1.json")));Assert.Equal(oldBytes,File.ReadAllBytes(Path.Combine(archive,"windows-before-import.json")));
        var restored=store.Load(out var warning);Assert.Null(warning);Assert.Equal("Mac 中文照片",restored.SourceLabel);Assert.Single(restored.Photos);Assert.Equal(hash,SHA256.HashData(source));
    }
    [Fact] public void OlderSavedWithoutOptionalFieldsAndOffTableAlbumReferencesRemainReadable()
    {
        var id=Guid.NewGuid();var pocket=Guid.NewGuid();var source=JsonSerializer.SerializeToUtf8Bytes(new{urls=Array.Empty<string>(),label="old",layout=new{slides=Array.Empty<object>()},albums=new[]{new{id,name="off table",pockets=new[]{new{id=pocket,format="135",column=0,row=0,url=Url("/Photos/one.png")}}}}});
        var result=MacSavedImporter.Import(source,[new("/Photos",root)]);Assert.Single(result.Document.Photos);Assert.Null(result.Document.Albums[0].Position);Assert.Equal(1180,result.Document.WorldWidth);Assert.Equal(1,result.Document.Viewport.Scale);
    }
}
