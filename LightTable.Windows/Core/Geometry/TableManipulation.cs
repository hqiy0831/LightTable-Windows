namespace LightTable.Core;

// A gesture owns the items hit at its origin. Existing selection alone must
// never turn a background/camera gesture into a photo translation.
public sealed class TableManipulation
{
    readonly HashSet<Guid> targets;
    public bool MovesItems=>targets.Count>0;
    public TableManipulation(Guid? hit,IEnumerable<Guid> selection)
    {
        targets=hit is {} id ? selection.Contains(id)?selection.ToHashSet():[id] : [];
    }
    public void Apply(TableDocument document,PointD anchor,PointD translation,double scale,double rotation)
    {
        if(!MovesItems)
        {
            document.Viewport.Zoom(document.Viewport.Scale*scale,anchor);
            document.Viewport.OffsetX+=translation.X;document.Viewport.OffsetY+=translation.Y;return;
        }
        var shift=translation*(1/document.Viewport.Scale);
        foreach(var slide in document.Slides.Where(s=>targets.Contains(s.Id)))
        {
            slide.Width*=scale;slide.Angle+=rotation;slide.X+=shift.X/document.WorldWidth;slide.Y+=shift.Y/document.WorldHeight;slide.Sanitize();
        }
        foreach(var album in document.Albums.Where(a=>targets.Contains(a.Id)&&a.Position!=null))
        {
            album.Scale=Math.Clamp(album.Scale*scale,.1,3);album.Angle+=rotation;album.Position+=shift;
        }
    }
}
