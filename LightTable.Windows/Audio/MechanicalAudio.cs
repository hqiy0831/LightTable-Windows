using Windows.Media.Core;
using Windows.Media.Playback;

namespace LightTable.Windows.Audio;

// Original synthesized mechanical impulse + offline room reflections. No third
// party recording is redistributed. User-authorized WAVs can override each cue.
sealed class MechanicalAudio : IDisposable
{
    readonly MediaPlayer outgoing=new(){Volume=.20,AutoPlay=false},arrival=new(){Volume=.20,AutoPlay=false};
    readonly TaskCompletionSource outgoingReady=new(TaskCreationOptions.RunContinuationsAsynchronously),arrivalReady=new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Action<string>? log;
    public bool IsPrepared {get;private set;}
    public string? Error {get;private set;}
    internal double Volume=>outgoing.Volume;
    internal double PositionSeconds=>Math.Max(outgoing.PlaybackSession.Position.TotalSeconds,arrival.PlaybackSession.Position.TotalSeconds);
    public MechanicalAudio(string dataRoot,Action<string>? log=null)
    {
        this.log=log;
        outgoing.MediaOpened+=(_,_)=>outgoingReady.TrySetResult();arrival.MediaOpened+=(_,_)=>arrivalReady.TrySetResult();
        outgoing.MediaFailed+=(_,e)=>outgoingReady.TrySetException(new InvalidDataException(e.ErrorMessage));
        arrival.MediaFailed+=(_,e)=>arrivalReady.TrySetException(new InvalidDataException(e.ErrorMessage));
        var dir=Path.Combine(dataRoot,"config","audio");Directory.CreateDirectory(dir);
        var cache=Path.Combine(dataRoot,"cache");
        string FileFor(string name,bool first)
        {
            var custom=Path.Combine(dir,name+".wav");if(File.Exists(custom))return custom;
            var generated=Path.Combine(cache,name+"-synth-v1.wav");if(!File.Exists(generated))Synthesize(generated,first);return generated;
        }
        outgoing.Source=MediaSource.CreateFromUri(new Uri(FileFor("ShutterClickRoom",false)));
        arrival.Source=MediaSource.CreateFromUri(new Uri(FileFor("ShutterArrivalRoom",true)));
    }
    public async Task PrepareAsync()
    {
        if(IsPrepared||Error!=null)return;
        try{await Task.WhenAll(outgoingReady.Task,arrivalReady.Task).WaitAsync(TimeSpan.FromSeconds(5));IsPrepared=true;}
        catch(Exception ex){Error=ex.Message;log?.Invoke($"幻灯声音准备失败：{ex}");}
    }
    public void Play(bool first=false)
    {
        Stop();if(!IsPrepared)return;var player=first?arrival:outgoing;player.PlaybackSession.Position=TimeSpan.Zero;player.Play();
    }
    public void Stop(){outgoing.Pause();arrival.Pause();}
    static void Synthesize(string path,bool first)
    {
        const int rate=48000;var n=(int)(rate*.38);var baseWave=new double[n];var rng=new Random(first?172:171);
        for(int i=0;i<n;i++)
        {
            var t=(double)i/rate;var click=0d;
            foreach(var (start,gain) in first?new[]{(0d,.65),(.04,.30)}:new[]{(0d,.70),(.045,.45),(.105,.35)})
            {
                var q=t-start;if(q>=0&&q<.08)click+=gain*Math.Exp(-q*95)*((rng.NextDouble()*2-1)*.7+Math.Sin(q*2*Math.PI*920)*.3);
            }
            baseWave[i]=i==0?0:click;
        }
        using var file=File.Create(path);using var w=new BinaryWriter(file);
        w.Write("RIFF"u8);w.Write(36+n*4);w.Write("WAVEfmt "u8);w.Write(16);w.Write((short)1);w.Write((short)2);w.Write(rate);w.Write(rate*4);w.Write((short)4);w.Write((short)16);w.Write("data"u8);w.Write(n*4);
        for(int i=0;i<n;i++)
        {
            double Channel(bool right)
            {
                var v=baseWave[i];foreach(var (delay,gain) in new[]{(.018,.18),(.041,.10),(.073,.06),(.131,.03)}){int k=i-(int)((delay+(right?.0015:0))*rate);if(k>=0)v+=baseWave[k]*gain;}
                return v;
            }
            w.Write((short)(Math.Clamp(Channel(false),-1,1)*26000));w.Write((short)(Math.Clamp(Channel(true),-1,1)*26000));
        }
    }
    public void Dispose(){outgoing.Dispose();arrival.Dispose();}
}
