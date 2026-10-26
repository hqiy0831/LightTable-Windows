namespace LightTable.Core;

// Lamp power and temporary loupe gestures have separate owners. An ignition
// starts only after the already visible films can display its highlight pass.
public sealed class LampState
{
    public bool IsLit {get;private set;}
    public bool Pending {get;private set;}
    public double StartedAt {get;private set;}=-10;
    public void Toggle(){IsLit=!IsLit;Pending=IsLit;}
    public bool TryIgnite(double now,bool surfacesReady)
    {
        if(!IsLit||!Pending||!surfacesReady)return false;StartedAt=now;Pending=false;return true;
    }
    public LightSample Sample(double now,bool reducedMotion=false)=>LightResponse.Sample(now-StartedAt,IsLit&&!Pending,reducedMotion);
    public bool Animating(double now,bool reducedMotion)=>IsLit&&(Pending||!reducedMotion&&now-StartedAt<LightResponse.Settling);
}
