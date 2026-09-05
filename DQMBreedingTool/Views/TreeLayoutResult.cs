namespace DQMBreedingTool.Views;

public class TreeLayoutResult
{
    public List<LayoutBox> Boxes { get; set; } = [];
    public List<LayoutEdge> Edges { get; set; } = [];
    public double Width { get; set; }
    public double Height { get; set; }
}