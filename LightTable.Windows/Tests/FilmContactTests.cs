using LightTable.Core;
using Xunit;

namespace LightTable.Tests;
public sealed class FilmContactTests
{
    [Fact] public void AllVariantsKeepEveryPhotoInteriorPixelTransparent()
    {
        foreach(var variant in Enum.GetValues<FilmContactVariant>())
        {
            var pixels=FilmContactTexture.Make(variant);Assert.Contains(pixels.Where((_,i)=>i%4==3),a=>a>0);
            for(int y=12;y<84;y++)for(int x=12;x<84;x++)Assert.Equal(0,pixels[(y*96+x)*4+3]);
        }
    }
    [Fact] public void CornerChoiceIsPersistentAndCurlVariantsAreDistinct()
    {
        var id=Guid.Parse("b582da31-14a6-413a-8297-54abe8504f39");Assert.Equal(FilmContactTexture.ForFilm(id),FilmContactTexture.ForFilm(Guid.Parse(id.ToString())));
        Assert.False(FilmContactTexture.Make(FilmContactVariant.LeftCurl).SequenceEqual(FilmContactTexture.Make(FilmContactVariant.RightCurl)));
    }
}
