using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class LampRegressionTests
{
    [Fact] public void SlowVisiblePhotoDoesNotConsumeTheTwoSecondIgnitionBeforeItCanDraw()
    {
        var lamp=new LampState();lamp.Toggle();Assert.True(lamp.Pending);Assert.False(lamp.TryIgnite(100,false));Assert.Equal(new(0,0,0),lamp.Sample(104));
        Assert.True(lamp.TryIgnite(105,true));Assert.Equal(105,lamp.StartedAt);Assert.Equal(1,lamp.Sample(105.16).Flare,7);
        Assert.True(lamp.Sample(106.1).Flare>0);Assert.Equal(new(1,0,.5),lamp.Sample(107));Assert.False(lamp.TryIgnite(110,true));Assert.Equal(105,lamp.StartedAt);
    }
    [Fact] public void CancellationAndLateResourcesCannotSwitchLampBackOn()
    {
        var lamp=new LampState();lamp.Toggle();lamp.Toggle();Assert.False(lamp.IsLit);Assert.False(lamp.Pending);Assert.False(lamp.TryIgnite(200,true));Assert.Equal(new(0,0,0),lamp.Sample(201));
    }
    [Fact] public void SpaceReturnAndRepeatedTargetsStayContinuousWithoutChangingLamp()
    {
        var lamp=new LampState();lamp.Toggle();lamp.TryIgnite(10,true);var approach=new TransitionClock();approach.Target(1,10,.5);
        for(int i=1;i<10;i++){var time=10+i*.05;var before=approach.Value(time);approach.Target(1,time,.5);Assert.Equal(before,approach.Value(time));}
        Assert.Equal(1,approach.Value(10.5));approach.Target(0,10.5,.34);Assert.InRange(approach.Value(10.67),.49,.51);Assert.Equal(0,approach.Value(10.84),7);Assert.True(lamp.IsLit);Assert.Equal(10,lamp.StartedAt);
    }
    [Fact] public void FocalReversalKeepsCurrentValueAndReducedMotionCanSnapSameTarget()
    {
        var focal=new TransitionClock();focal.Target(1,0,.26);Assert.Equal(.5,focal.Value(.13),8);focal.Target(0,.13,.26);Assert.Equal(.5,focal.Value(.13));Assert.InRange(focal.Value(.26),.249,.251);
        focal.Target(0,.26,0);Assert.Equal(0,focal.Value(.26));Assert.False(focal.Active(.26));Assert.Equal(0,LightResponse.Brightness(0));Assert.Equal(LightResponse.Level(0),LightResponse.Level(.1));
    }
}
