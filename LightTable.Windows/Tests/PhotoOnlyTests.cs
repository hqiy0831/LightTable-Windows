using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class PhotoOnlyTests
{
    static Photo Photo(int i)=>new(){Path=Path.GetFullPath($"photo-{i}.jpg")};
    [Fact] public void HundredPhotosKeepOrderWithSixtyPlacedAndReturned()
    {
        var photos=Enumerable.Range(0,100).Select(Photo).ToArray();var doc=new TableDocument{Photos=photos.ToList()};doc.EnsureCollection();
        foreach(var p in photos.Take(60))doc.Place(p.Path,new(10,10));
        Assert.Equal(photos,doc.ActivePhotos);Assert.Equal(60,doc.ActivePhotos.Count(p=>doc.Slides.Any(s=>s.Path==p.Path)));Assert.Equal(40,doc.ActivePhotos.Count(p=>!doc.Slides.Any(s=>s.Path==p.Path)));
        doc.ReturnToCollection(doc.Slides.Take(10).Select(s=>s.Id).ToArray());Assert.Equal(photos,doc.ActivePhotos);Assert.Equal(50,doc.Slides.Count);
    }
    [Fact] public void LegacyAlbumsBecomePhotosInTheirOwnColumnsAndConversionIsIdempotent()
    {
        var a=Photo(1);var b=Photo(2);var c=Photo(3);var doc=new TableDocument{Photos=[a]};var first=doc.EnsureCollection();var slide=doc.Place(a.Path,new(2,3));var page=new AlbumPage();page.Fill(FilmFormat.Film135);page.Pockets[0].Path=b.Path;page.Pockets[1].Path=b.Path;doc.Albums.Add(page);doc.TableOrder.Add(page.Id);
        var second=doc.AddCollection();var other=new AlbumPage();other.Fill(FilmFormat.Film120);other.Pockets[0].Path=c.Path;doc.Albums.Add(other);
        Assert.Equal(2,doc.ConvertLegacyAlbumsToPhotos());Assert.Empty(doc.Albums);Assert.Equal(new[]{c.Path},doc.ActivePhotos.Select(p=>p.Path));
        doc.SwitchCollection(first.Id);Assert.Empty(doc.Albums);Assert.Equal(new[]{a.Path,b.Path},doc.ActivePhotos.Select(p=>p.Path));Assert.Equal(slide.Id,doc.Slides.Single().Id);Assert.DoesNotContain(page.Id,doc.TableIds);Assert.Equal(0,doc.ConvertLegacyAlbumsToPhotos());
        doc.SwitchCollection(second.Id);Assert.Equal(c.Path,doc.ActivePhotos.Single().Path);
    }
    [Fact] public void AlbumConversionCreatesIndependentRecoverableBackupBeforeSaving()
    {
        var root=Path.Combine(Path.GetTempPath(),"LightTable-photo-only-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new DocumentStore(root);var a=Photo(1);var doc=new TableDocument{Photos=[a]};doc.EnsureCollection();var page=new AlbumPage();page.Fill(FilmFormat.Film135);page.Pockets[0].Path=a.Path;doc.Albums.Add(page);store.Save(doc);
            var archive=store.MigrateLegacyAlbums(doc);Assert.NotNull(archive);Assert.True(File.Exists(archive));var backup=System.Text.Json.JsonSerializer.Deserialize<TableDocument>(File.ReadAllText(archive),DocumentStore.JsonOptions)!;Assert.Single(backup.Albums);
            var loaded=store.Load(out var warning);Assert.Null(warning);Assert.Empty(loaded.Albums);Assert.Equal(a.Path,loaded.ActivePhotos.Single().Path);Assert.Null(store.MigrateLegacyAlbums(loaded));
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
    [Fact] public void FailedBackupDoesNotRemoveLegacyAlbum()
    {
        var root=Path.Combine(Path.GetTempPath(),"LightTable-photo-only-"+Guid.NewGuid().ToString("N"));
        try
        {
            var store=new DocumentStore(root);File.WriteAllText(Path.Combine(root,"layouts","before-album-removal"),"block directory creation");var doc=new TableDocument();doc.EnsureCollection();doc.Albums.Add(new());
            Assert.Throws<IOException>(()=>store.MigrateLegacyAlbums(doc));Assert.Single(doc.Albums);
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
