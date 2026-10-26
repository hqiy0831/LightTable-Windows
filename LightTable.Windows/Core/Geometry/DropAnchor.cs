namespace LightTable.Core;

// A drop owns the camera and reference dimensions at pointer release. Imports
// may finish after the user has panned, zoomed or explicitly rearranged the table.
public readonly record struct DropAnchor(double Scale,double OffsetX,double OffsetY,double WorldWidth,double WorldHeight)
{
    public static DropAnchor Capture(TableDocument document)=>new(document.Viewport.Scale,document.Viewport.OffsetX,document.Viewport.OffsetY,document.WorldWidth,document.WorldHeight);
    public Slide Place(TableDocument document,string path,PointD screen)
    {
        var x=(screen.X-OffsetX)/Scale/WorldWidth;var y=(screen.Y-OffsetY)/Scale/WorldHeight;
        return document.Place(path,new(x*document.WorldWidth,y*document.WorldHeight));
    }
}
