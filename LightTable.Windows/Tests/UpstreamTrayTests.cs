using LightTable.Core;
using System.Text.Json;
using Xunit;

namespace LightTable.Tests;

public sealed class UpstreamTrayTests
{
    static Photo P(string name)=>new(){Path=Path.GetFullPath(name)};
    [Fact] public void TakingAndReturningFilmsPreservesOriginalTrayOrder()
    {
        var a=P("1.jpg");var b=P("2.jpg");var c=P("3.jpg");var doc=new TableDocument{Photos=[a,b,c]};doc.EnsureCollection();
        var slide=doc.Place(b.Path,new(200,100));Assert.Equal(new[]{a.Path,c.Path},doc.AvailablePhotos.Select(p=>p.Path));doc.Remove([slide.Id]);Assert.Equal(new[]{a.Path,b.Path,c.Path},doc.AvailablePhotos.Select(p=>p.Path));
    }
    [Fact] public void PhotoDeletionRemovesOnlyItsCurrentLooseFilmAndKeepsAlbumReferences()
    {
        var a=P("1.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();doc.Place(a.Path,new(100,200));
        var second=doc.AddCollection();doc.SetCollectionPhotos(second,[a],false);doc.Place(a.Path,new(200,100));var album=new AlbumPage();album.Fill(FilmFormat.Film135);album.Pockets[0].Path=a.Path;doc.Albums.Add(album);
        doc.RemoveFromCollection([a.Path]);Assert.Empty(doc.Slides);Assert.Empty(doc.ActivePhotos);Assert.Equal(a.Path,album.Pockets[0].Path);
        doc.SwitchCollection(first.Id);Assert.Single(doc.Slides);Assert.Single(doc.ActivePhotos);
    }
    [Fact] public void DeletingAColumnKeepsOthersAndDeletingTheLastCreatesFreshEmptyDestination()
    {
        var a=P("1.jpg");var b=P("2.jpg");var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();doc.Place(a.Path,new(100,100));var second=doc.AddCollection();doc.SetCollectionPhotos(second,[b],false);doc.Place(b.Path,new(200,100));
        doc.DeleteCollection(second.Id);Assert.Equal(first.Id,doc.ActiveCollectionId);Assert.Equal(a.Path,doc.Slides.Single().Path);Assert.Equal(a.Path,doc.Photos.Single().Path);
        doc.DeleteCollection(first.Id);Assert.NotEqual(first.Id,doc.ActiveCollectionId);Assert.Single(doc.Collections);Assert.Empty(doc.ActivePhotos);Assert.Empty(doc.Slides);Assert.Empty(doc.Photos);
    }
    [Fact] public void IgnitionExposureMatchesUpstreamPointEightEvAndKeepsUserTwoSecondTiming()
    {Assert.Equal(.8,LightResponse.Exposure(1,1));Assert.Equal(0,LightResponse.Exposure(0,1));Assert.Equal(2,LightResponse.Settling);}
    [Fact] public void WindowsPreviewUpdateTagsAreExplicitlyOptInAndMacRepositoryIsRejected()
    {
        var prefix="https://github.com/hqiy0831/LightTable-Windows/releases/";
        var json=JsonSerializer.Serialize(new[]{new{draft=false,prerelease=true,tag_name="v0.1.75.2-windows-preview",body="Windows notes",html_url=prefix+"tag/v0.1.75.2-windows-preview",assets=new[]{new{name="LightTable-Windows-Setup-x64.exe",browser_download_url=prefix+"download/v0.1.75.2-windows-preview/LightTable-Windows-Setup-x64.exe"}}}});
        Assert.Null(ReleasePolicy.Find(json,new(0,1,75,1)));Assert.NotNull(ReleasePolicy.Find(json,new(0,1,75,1),true));Assert.Null(ReleasePolicy.Find(json,new(0,1,75,2),true));
        Assert.False(ReleasePolicy.Allowed(new("https://github.com/fuyuan9972-svg/LightTable/releases/download/v0.1.75/LightTable.dmg")));Assert.False(ReleasePolicy.Allowed(new("https://github.com:444/hqiy0831/LightTable-Windows/releases/test")));
    }
}
