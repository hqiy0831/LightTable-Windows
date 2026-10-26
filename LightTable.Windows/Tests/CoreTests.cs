using System.Security.Cryptography;
using System.Text.Json;
using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class GeometryTests
{
    [Fact] public void CameraZoomKeepsPointerWorldAnchor()
    {
        var r=new Random(172);
        for(int i=0;i<1000;i++)
        {
            var c=new Viewport{Scale=.05+r.NextDouble()*7.95,OffsetX=r.NextDouble()*10000-5000,OffsetY=r.NextDouble()*10000-5000};
            var p=new PointD(r.NextDouble()*2000,r.NextDouble()*1200);var before=c.World(p);c.Zoom(r.NextDouble()*10,p);var after=c.World(p);
            Assert.Equal(before.X,after.X,7);Assert.Equal(before.Y,after.Y,7);var round=c.Screen(c.World(p));Assert.Equal(p.X,round.X,7);Assert.Equal(p.Y,round.Y,7);
        }
    }
    [Theory][InlineData(0)][InlineData(37)][InlineData(90)][InlineData(173)][InlineData(270)]
    public void RotatedHitTracksImagePixels(double angle)
    {
        var p=new Placement(new(500,300),200,100,angle);
        Assert.True(p.Contains(p.Center+Geometry.Rotate(new(99,49),angle)));
        Assert.False(p.Contains(p.Center+Geometry.Rotate(new(101,49),angle)));
        var local=p.Local(p.Center+Geometry.Rotate(new(40,20),angle));Assert.Equal(40,local.X,7);Assert.Equal(20,local.Y,7);
    }
    [Fact] public void MarqueeUsesSeparatingAxesNotBoundingBox()
    {
        var p=new Placement(new(100,100),200,20,45);
        Assert.False(p.Intersects(new RectD(28,155,5,5)));Assert.True(p.Intersects(new RectD(98,98,4,4)));
    }
    [Theory][InlineData(100,1,256)][InlineData(257,1,384)][InlineData(512,2,1024)][InlineData(1100,2,2200)][InlineData(10000,2,2200)]
    public void PreviewBucketsMatchBaseline(double pixels,double dpi,int expected)=>Assert.Equal(expected,Geometry.PreviewBucket(pixels,pixels/1.5,dpi));
    [Fact] public void FitPreservesSourcesAnglesAndLayerIdentity()
    {
        var doc=new TableDocument();for(int i=0;i<100;i++){var path=Path.GetFullPath($"photo-{i}.jpg");doc.Photos.Add(new(){Path=path,Aspect=.5+i%7*.7});doc.Slides.Add(new(){Path=path,Angle=i%4*15});}
        var ids=doc.Slides.Select(s=>s.Id).ToArray();var angles=doc.Slides.Select(s=>s.Angle).ToArray();Geometry.Fit(doc,1180,550);
        Assert.Equal(ids,doc.Slides.Select(s=>s.Id));Assert.Equal(angles,doc.Slides.Select(s=>s.Angle));
        foreach(var s in doc.Slides){var p=Geometry.Place(doc,s);Assert.True(p.Center.X>0&&p.Center.X<1180&&p.Center.Y>0&&p.Center.Y<550);Assert.Equal(.38,s.Width);}
        Geometry.Fit(doc,1180,550,true);Assert.Equal(100,doc.Photos.Count);Assert.Equal(100,doc.Slides.Count);
    }
    [Fact] public void RepeatedPlacementMovesOneSlideAndBringsItForward()
    {
        var doc=new TableDocument();var a=doc.Place(Path.GetFullPath("A.jpg"),new(10,30));var b=doc.Place(Path.GetFullPath("B.jpg"),new(20,40));
        var repeated=doc.Place(a.Path,new(100,200));Assert.Same(a,repeated);Assert.Equal(2,doc.Slides.Count);Assert.Equal(a.Id,doc.TableIds.Last());Assert.Equal(100d/doc.WorldWidth,a.X);
    }
}
public sealed class AlbumTests
{
    [Theory][InlineData(FilmFormat.Film135,20)][InlineData(FilmFormat.Film120,9)]
    public void BaselineLatticeCapacity(FilmFormat format,int count)
    {
        var page=new AlbumPage();page.Fill(format);Assert.Equal(count,page.Pockets.Count);Assert.Null(page.Add(format));
        foreach(var p in page.Pockets)Assert.True(page.Accepts(p));
    }
    [Fact] public void MatchingPocketsSwapWithoutChangingPhotos()
    {
        var a=new AlbumPage();a.Fill(FilmFormat.Film135);var p=a.Pockets[0];var q=a.Pockets[1];p.Path="a.jpg";q.Path="b.jpg";
        Assert.True(a.Move(p.Id,q.Column,q.Row));Assert.Equal(3,p.Column);Assert.Equal(0,q.Column);Assert.Equal("a.jpg",p.Path);Assert.Equal("b.jpg",q.Path);
    }
    [Fact] public void MixedOverlapAndOutOfBoundsMovesAreAtomic()
    {
        var a=new AlbumPage();var p=a.Add(FilmFormat.Film120,0,0)!;var q=a.Add(FilmFormat.Film135,5,0)!;
        Assert.False(a.Move(p.Id,5,0));Assert.Equal(0,p.Column);Assert.Equal(5,q.Column);Assert.Null(a.Add(FilmFormat.Film120,14,10));
    }
}
public sealed class OpticsTests
{
    [Fact] public void IgnitionHasSharedPeakAndSettlesExactly()
    {
        Assert.Equal(new LightSample(0,0,0),LightResponse.Sample(.2,false));
        Assert.Equal(1,LightResponse.Sample(.16,true).Flare);Assert.Equal(1,LightResponse.Sample(.28,true).Flare);
        var last=1d;for(double t=.28;t<LightResponse.Settling;t+=.001){var v=LightResponse.Sample(t,true);Assert.InRange(v.Flare,0,last+1e-12);last=v.Flare;Assert.Equal(1,v.Transmission);}
        Assert.Equal(2,LightResponse.Settling);Assert.True(LightResponse.Sample(1.1,true).Flare>0);Assert.Equal(new LightSample(1,0,.5),LightResponse.Sample(2,true));Assert.Equal(0,LightResponse.Exposure(0,1));
    }
    [Fact] public void ReducedMotionAndInvalidPowerRemainFinite()
    {
        Assert.Equal(new LightSample(1,0,.5),LightResponse.Sample(0,true,true));
        Assert.Equal(1,LightResponse.Power(double.NaN));Assert.InRange(LightResponse.PanelWhite(1.6,1),.9,1);
    }
    [Theory][InlineData(1180,550)][InlineData(2200,1100)][InlineData(400,300)]
    public void ClosePowersDoNotDependOnApertureSize(double width,double height)
    {
        var s=new SizeD(width,height);
        foreach(var power in Enum.GetValues<LoupePower>())Assert.Equal(LoupePowers.Scale(power),LoupePose.Sample(s,1,LoupePowers.Scale(power)).Magnification,8);
        var center=LoupeGeometry.CloseCenter(new(0,0),s);Assert.True(center.X>=0&&center.Y>=0);
    }
    [Fact] public void MechanicalTravelHasDistinctDepartureBlackArrival()
    {
        Assert.Equal(.08,ProjectionMotion.DepartureDuration);Assert.Equal(.12,ProjectionMotion.BlackDuration);Assert.Equal(.18,ProjectionMotion.ArrivalDuration);
        var size=new SizeD(1920,1080);var image=ProjectionMotion.Fit(size,1.5);var incoming=ProjectionMotion.At(-1,size,image);var outgoing=ProjectionMotion.At(1,size,image);
        Assert.True(incoming.X<0&&incoming.Y<0&&incoming.Angle<0);Assert.True(outgoing.X<0&&outgoing.Y>0&&outgoing.Angle>0);
        Assert.Equal(new TravelPose(0,0,0),ProjectionMotion.At(0,size,image));
    }
}
public sealed class PersistenceTests
{
    [Fact] public void AtomicSaveBackupAndRecoveryPreserveOriginal()
    {
        var root=Path.Combine(Path.GetTempPath(),"LightTableTests",Guid.NewGuid().ToString());Directory.CreateDirectory(root);
        try
        {
            var original=Path.Combine(root,"照片 Unicode.jpg");File.WriteAllBytes(original,[1,2,3,4]);var hash=SHA256.HashData(File.ReadAllBytes(original));
            var store=new DocumentStore(Path.Combine(root,"data"));var d=new TableDocument();d.Photos.Add(new(){Path=original});var slide=d.Place(original,new(300,100));slide.Angle=37;d.Viewport.Zoom(2,new(50,50));
            var album=new AlbumPage{Position=new(40,80),Angle=12};album.Fill(FilmFormat.Film135);album.Pockets[0].Path=original;d.Albums.Add(album);d.BringToFront(album.Id);store.Save(d);
            var restored=store.Load(out var warning);Assert.Null(warning);Assert.Equal(37,restored.Slides[0].Angle);Assert.Equal(album.Id,restored.TableIds.Last());Assert.Equal(original,restored.Albums[0].Pockets[0].Path);
            d.Remove([slide.Id,album.Id]);store.Save(d);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(original)));Assert.Single(d.Photos);Assert.Single(d.Albums);Assert.Null(d.Albums[0].Position);
            File.WriteAllText(store.DocumentPath,"{ corrupt");var recovery=store.Load(out warning);Assert.NotNull(warning);Assert.Single(recovery.Slides);Assert.Equal(hash,SHA256.HashData(File.ReadAllBytes(original)));
        }
        finally{Directory.Delete(root,true);}
    }
    [Fact] public void SanitizeRejectsNonlocalPathsDuplicateIdentitiesAndInvalidTransforms()
    {
        var p=Path.GetFullPath("photo.jpg");var s=new Slide{Path=p,X=double.NaN,Y=double.PositiveInfinity,Width=-10,Angle=double.NaN};
        var d=new TableDocument{Slides=[s,new(){Id=s.Id,Path=Path.GetFullPath("other.jpg")},new(){Path="https://host/photo.jpg"}],WorldWidth=double.NaN};d.Sanitize();
        Assert.Single(d.Slides);Assert.Equal(.5,s.X);Assert.Equal(.12,s.Width);Assert.Equal(0,s.Angle);Assert.Equal(1180,d.WorldWidth);
    }
    [Fact] public void SchemaZeroWithoutAlbumFieldsMigrates()
    {
        var d=JsonSerializer.Deserialize<TableDocument>("{\"SchemaVersion\":0,\"Photos\":[],\"Slides\":[]}")!;d.Sanitize();Assert.Equal(TableDocument.CurrentSchema,d.SchemaVersion);Assert.Empty(d.Albums);
    }
}
public sealed class UpdateTests
{
    static string Feed(string name,string url="https://github.com/fuyuan9972-svg/LightTable/releases/download/v0.1.73/LightTable-Windows-Setup-x64.exe")=>JsonSerializer.Serialize(new[]{new{draft=false,prerelease=false,tag_name="v0.1.73",body="Full notes",html_url="https://github.com/fuyuan9972-svg/LightTable/releases/tag/v0.1.73",assets=new[]{new{name,browser_download_url=url}}}});
    [Fact] public void MacArmPackagesNeverSelected()=>Assert.Null(ReleasePolicy.Find(Feed("LightTable-v0.1.73-macOS-arm64.zip"),new(0,1,72,1)));
    [Fact] public void OnlyNewerWindowsX64HttpsInstallerIsAccepted()
    {
        Assert.NotNull(ReleasePolicy.Find(Feed("LightTable-Windows-Setup-x64.exe"),new(0,1,72,1)));
        Assert.Null(ReleasePolicy.Find(Feed("LightTable-Windows-Setup-x64.exe"),new(0,1,74)));
        Assert.Null(ReleasePolicy.Find(Feed("LightTable-Windows-Setup-x64.exe","http://github.com/fuyuan9972-svg/LightTable/test"),new(0,1,72)));
        Assert.Null(ReleasePolicy.Find(Feed("LightTable-Windows-Setup-x64.exe","https://evil.test/fake.exe"),new(0,1,72)));
    }
}

public sealed class LoupePoseTests
{
    [Theory][InlineData(1180,900)][InlineData(2200,1100)][InlineData(400,300)]
    public void ApproachSharesOpticalAxisAndPreservesActualSelectedScale(double width,double height)
    {
        var size=new SizeD(width,height);
        foreach(var power in Enum.GetValues<LoupePower>())
        {
            double target=LoupePowers.Scale(power);var start=LoupePose.Sample(size,0,target);var end=LoupePose.Sample(size,1,target);
            Assert.Equal(260,start.Diameter);Assert.Equal(target,start.Magnification);Assert.Equal(0,start.Travel);
            Assert.Equal(LoupeGeometry.CloseDiameter(size),end.Diameter,8);Assert.Equal(target,end.Magnification,8);
            double diameter=0;
            for(int i=0;i<=100;i++)
            {
                var p=LoupePose.Sample(size,i/100d,target);Assert.True(p.Diameter>=diameter);
                Assert.Equal(target,p.Magnification,8);diameter=p.Diameter;
            }
        }
    }
    [Fact] public void EdgeWarpLeavesCentreStraightAndIsMonotonic()
    {
        Assert.Equal(0,LoupePose.EdgeInset(.89,130,1));Assert.Equal(.006*130,LoupePose.EdgeInset(1,130,1),8);
        double prior=-1;
        for(int i=0;i<=1000;i++){var r=i/1000d;var mapped=r-LoupePose.EdgeInset(r,130,1)/130;Assert.True(mapped>=prior);prior=mapped;}
        Assert.Equal(0,LoupePose.EdgeInset(1,130,0));
    }
    [Fact] public void InterruptedApproachClockDoesNotJump()
    {
        var clock=new TransitionClock();clock.Target(1,10,.5);Assert.Equal(.4,clock.Value(10.2),8);
        clock.Target(0,10.2,.34);Assert.Equal(.4,clock.Value(10.2),8);Assert.Equal(.2,clock.Value(10.37),8);
        clock.Target(1,10.37,.5);Assert.Equal(.2,clock.Value(10.37),8);Assert.Equal(1,clock.Value(10.87),8);
    }
}

public sealed class RecoveryAndSleeveTests
{
    [Fact] public void TwoDamagedDocumentsArePreservedAndNewSessionUsesSeparateFile()
    {
        var root=Path.Combine(Path.GetTempPath(),"LightTableTests",Guid.NewGuid().ToString());var store=new DocumentStore(root);
        try
        {
            var primary=store.DocumentPath;File.WriteAllText(primary,"damaged-original");File.WriteAllText(primary+".bak","damaged-backup");
            var doc=store.Load(out var warning);Assert.NotNull(warning);Assert.EndsWith("table.recovered.json",store.DocumentPath);store.Save(doc);
            Assert.Equal("damaged-original",File.ReadAllText(primary));Assert.Equal("damaged-backup",File.ReadAllText(primary+".bak"));
            var next=new DocumentStore(root);next.Load(out warning);Assert.EndsWith("table.recovered.json",next.DocumentPath);
        }
        finally{Directory.Delete(root,true);}
    }
    [Fact] public void SleevePullClearsMouthThenExpandsWithoutChangingGrabPoint()
    {
        var origin=new RectD(100,700,90,56);var start=new PointD(145,728);var same=SleeveMotion.Frame(origin,start,start,new(240,160));
        Assert.Equal(origin,same);Assert.Equal(0,SleeveMotion.Expansion(origin,new(145,680),start));
        var pointer=new PointD(300,500);var full=SleeveMotion.Frame(origin,pointer,start,new(240,160));
        Assert.Equal(1,SleeveMotion.Expansion(origin,pointer,start));Assert.Equal(pointer,full.Center);Assert.Equal(240,full.Width);Assert.Equal(160,full.Height);
        var centre=SleeveMotion.EdgeVelocity(new(590,290),new(1180,580));Assert.Equal(new PointD(0,0),centre);
        Assert.True(SleeveMotion.EdgeVelocity(new(1,1),new(1180,580)).X>0);Assert.True(SleeveMotion.EdgeVelocity(new(1179,579),new(1180,580)).Y<0);
    }
}
