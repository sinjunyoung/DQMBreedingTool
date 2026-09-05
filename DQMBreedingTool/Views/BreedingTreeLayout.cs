using DQMBreedingTool.Models;

namespace DQMBreedingTool.Views;

public class LayoutBox
{
    public RecipeNode Node { get; set; } = null!;
    public double X { get; set; }
    public double Y { get; set; }
}

public class LayoutEdge
{
    public LayoutBox From { get; set; } = null!;
    public LayoutBox To { get; set; } = null!;
}

public class TreeLayoutResult
{
    public List<LayoutBox> Boxes { get; } = [];
    public List<LayoutEdge> Edges { get; } = [];
    public double Width { get; set; }
    public double Height { get; set; }
}

public static class BreedingTreeLayout
{
    public const double BoxWidth = 64;
    public const double BoxHeight = 64;
    public const double ColumnGap = 50;
    public const double RowGap = 12;

    public static TreeLayoutResult Compute(RecipeNode root, HashSet<RecipeNode> collapsedNodes)
    {
        var result = new TreeLayoutResult();
        int maxDepth = GetMaxDepth(root, 0);
        double slotHeight = BoxHeight + RowGap;
        var yPositions = new Dictionary<RecipeNode, double>();
        double nextLeafY = 0;

        double AssignY(RecipeNode node, int depth)
        {
            if (node.IsRecipeGroup) 
                return 0;

            bool isCollapsed = collapsedNodes.Contains(node);
            var validChildren = node.Children.Where(c => !c.IsRecipeGroup).ToList();

            if (validChildren.Count == 0 || isCollapsed)
            {
                double y = nextLeafY;

                nextLeafY += slotHeight;
                yPositions[node] = y;

                return y;
            }
            else
            {
                double childSum = 0;

                foreach (var child in validChildren)
                    childSum += AssignY(child, depth + 1);

                double y = childSum / validChildren.Count;

                yPositions[node] = y;

                return y;
            }
        }

        AssignY(root, 0);

        var nodeToBox = new Dictionary<RecipeNode, LayoutBox>();

        void BuildBoxes(RecipeNode node, int depth)
        {
            if (node.IsRecipeGroup)
                return;

            if (!yPositions.TryGetValue(node, out double value))
                return;

            var box = new LayoutBox
            {
                Node = node,
                X = (maxDepth - depth) * (BoxWidth + ColumnGap),
                Y = value
            };

            nodeToBox[node] = box;
            result.Boxes.Add(box);

            if (!collapsedNodes.Contains(node))
            {
                foreach (var child in node.Children)
                {
                    BuildBoxes(child, depth + 1);

                    if (nodeToBox.TryGetValue(child, out var childBox))
                        result.Edges.Add(new LayoutEdge { From = childBox, To = box });
                }
            }
        }

        BuildBoxes(root, 0);
        ResolveOverlaps(result.Boxes, slotHeight);

        result.Width = (maxDepth + 1) * (BoxWidth + ColumnGap);
        result.Height = result.Boxes.Count > 0 ? result.Boxes.Max(b => b.Y) + BoxHeight + RowGap : BoxHeight;

        return result;
    }

    static void ResolveOverlaps(List<LayoutBox> boxes, double minGap)
    {
        var byColumn = boxes.GroupBy(b => Math.Round(b.X));

        foreach (var column in byColumn)
        {
            var sorted = column.OrderBy(b => b.Y).ToList();

            for (int i = 1; i < sorted.Count; i++)
            {
                double minY = sorted[i - 1].Y + minGap;

                if (sorted[i].Y < minY)
                {
                    double diff = minY - sorted[i].Y;

                    for (int j = i; j < sorted.Count; j++)
                        sorted[j].Y += diff;
                }
            }
        }
    }

    static int GetMaxDepth(RecipeNode node, int depth)
    {
        int max = depth;

        foreach (var child in node.Children)
        {
            int childDepth = node.IsRecipeGroup ? depth : depth + 1;

            max = Math.Max(max, GetMaxDepth(child, childDepth));
        }

        return max;
    }
}