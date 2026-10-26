using LightTable.Core;

namespace LightTable.Windows.Rendering;

enum FlightPhase { Dragging, Landing, Returning }
sealed class TrayFlight
{
    public Guid Id {get;}=Guid.NewGuid();
    public string Path {get;init;}="";
    public Guid? AlbumId {get;init;}
    public RectD Origin {get;init;}
    public PointD Start {get;init;}
    public RectD Frame {get;set;}
    public double Expansion {get;set;}
    public double Lift {get;set;}
    public double Angle {get;set;}
    public FlightPhase Phase {get;set;}
}
