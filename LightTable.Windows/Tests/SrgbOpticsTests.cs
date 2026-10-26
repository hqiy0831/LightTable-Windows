using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class SrgbOpticsTests
{
    [Fact] public void PrimariesAndNeutralUseLinearSrgbLuminance()
    {
        Assert.Equal(.2126,SrgbOptics.Luminance(1,0,0),8);
        Assert.Equal(.7152,SrgbOptics.Luminance(0,1,0),8);
        Assert.Equal(.0722,SrgbOptics.Luminance(0,0,1),8);
        Assert.Equal(1,SrgbOptics.Luminance(1,1,1),8);Assert.Equal(0,SrgbOptics.Luminance(0,0,0));
        Assert.Equal(.21404114048223255,SrgbOptics.Luminance(.5,.5,.5),8);
    }
    [Fact] public async Task ColorRecipeCannotReusePreviousP3WeightedOpticalLayers()
    {
        var root=Path.Combine(Path.GetTempPath(),"LightTable-color-"+Guid.NewGuid().ToString("N"));
        try
        {
            var storage=new DocumentStore(root);var old=new OpticalLayerCache(storage,"srgb-bgra8-scatter-v3-zero-floor");
            await old.GetOrBuildAsync("source",1,()=>new(1,1,Enumerable.Range(0,5).Select(_=>new byte[]{1,2,3,255}).ToArray()));
            var current=new OpticalLayerCache(storage);Assert.NotEqual(old.EntryPath("source",1),current.EntryPath("source",1));
            await current.GetOrBuildAsync("source",1,()=>new(1,1,Enumerable.Range(0,5).Select(_=>new byte[]{4,5,6,255}).ToArray()));
            Assert.Equal(0,current.DiskHits);Assert.Equal(1,current.BuildCount);
        }
        finally{if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
