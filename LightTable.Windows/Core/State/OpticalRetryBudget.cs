namespace LightTable.Core;

// Matches upstream's bounded 16.7/50/150ms recovery, scoped to an input revision.
public sealed class OpticalRetryBudget
{
    public long Revision {get;private set;}
    int attempts;
    public void BeginInput(){Revision++;attempts=0;}
    public void Succeeded()=>attempts=0;
    public TimeSpan? NextDelay()=>attempts++ switch{0=>TimeSpan.FromSeconds(1d/60),1=>TimeSpan.FromMilliseconds(50),2=>TimeSpan.FromMilliseconds(150),_=>null};
}
