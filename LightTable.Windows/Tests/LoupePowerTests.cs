using LightTable.Core;
using Xunit;

namespace LightTable.Tests;

public sealed class LoupePowerTests
{
    [Fact] public void AllFourPowersCycleAndMatchSegmentHitRegions()
    {
        var power=LoupePower.One;
        for(int i=0;i<12;i++)
        {
            Assert.Equal(i%4,(int)power);Assert.Equal(Math.Pow(2,i%4),LoupePowers.Scale(power));
            Assert.Equal(power,LoupePowers.At((i%4+.5)*48,192));power=LoupePowers.Next(power);
        }
        Assert.Equal(LoupePower.One,LoupePowers.At(-50,192));Assert.Equal(LoupePower.Eight,LoupePowers.At(900,192));
        Assert.Equal(LoupePower.Two,LoupePowers.At(48,192));Assert.Equal(LoupePower.One,LoupePowers.At(double.NaN,192));
        Assert.Equal(LoupePower.One,LoupePowers.At(50,0));Assert.Equal(1,LoupePowers.Scale((LoupePower)999));
    }
    [Fact] public void EveryIntermediatePowerIsContinuousAndIndependentOfWindowSize()
    {
        for(int i=0;i<=300;i++)
        {
            var coordinate=i/100d;var actual=LoupePowers.ScaleAt(coordinate);
            Assert.Equal(actual,LoupePose.Sample(new(1180,550),1,actual).Magnification,8);
            Assert.Equal(actual,LoupePose.Sample(new(2200,1100),1,actual).Magnification,8);
            Assert.Equal(actual,LoupePose.Sample(new(1180,550),0,actual).Magnification,8);
            Assert.Equal(actual,LoupePose.Sample(new(1180,550),.45,actual).Magnification,8);
        }
        Assert.Equal(2,LoupePowers.ScaleAt(1));Assert.Equal(4,LoupePowers.ScaleAt(2));Assert.Equal(8,LoupePowers.ScaleAt(3));
    }
    [Fact] public void RapidRetargetUsesCurrentRenderedValueInEitherDirection()
    {
        var clock=new SmoothTransitionClock();clock.Target(3,0,.26);
        var before=clock.Value(.08);Assert.InRange(before,.01,2.99);
        clock.Target(1,.08,.26);Assert.Equal(before,clock.Value(.08));
        var midway=clock.Value(.14);clock.Target(2,.14,.26);Assert.Equal(midway,clock.Value(.14));
        Assert.Equal(2,clock.Value(.4),8);Assert.False(clock.Active(.41));
        clock.Target(0,.5,.26);var returning=clock.Value(.6);clock.Target(3,.6,.26);Assert.Equal(returning,clock.Value(.6));
        clock.Target(3,.61,0);Assert.Equal(3,clock.Value(.61));Assert.False(clock.Active(.61));
    }
}
