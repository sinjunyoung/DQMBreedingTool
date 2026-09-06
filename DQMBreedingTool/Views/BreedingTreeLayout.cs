using DQMBreedingTool.Models;

namespace DQMBreedingTool.Views;

public static class BreedingTreeLayout
{
    public const double BoxWidth = 40;
    public const double BoxHeight = 42;
    public const double ColumnGap = 15;
    public const double RowGap = 5;

    public static TreeLayoutResult Compute(RecipeNode root, HashSet<RecipeNode> collapsedNodes)
    {
        var result = new TreeLayoutResult();

        if (root == null) 
            return result;

        int maxDepth = GetMaxDepth(root, 0);
        double slotHeight = BoxHeight + RowGap;
        var yPositions = new Dictionary<RecipeNode, double>();
        double nextLeafY = 0;

        double AssignY(RecipeNode node, int depth)
        {
            if (node == null)
                return 0;

            bool isCollapsed = collapsedNodes.Contains(node);

            if (node.Children == null || node.Children.Count == 0 || isCollapsed)
            {
                double y = nextLeafY;

                nextLeafY += slotHeight;
                yPositions[node] = y;

                return y;
            }
            else
            {
                double childSum = 0;

                foreach (var child in node.Children)
                    childSum += AssignY(child, depth + 1);

                double y = childSum / node.Children.Count;

                yPositions[node] = y;

                return y;
            }
        }

        AssignY(root, 0);

        var nodeToBox = new Dictionary<RecipeNode, LayoutBox>();

        void BuildTree(RecipeNode node, int depth)
        {
            if (node == null || !yPositions.TryGetValue(node, out double value)) 
                return;

            var box = new LayoutBox
            {
                Node = node,
                X = (maxDepth - depth) * (BoxWidth + ColumnGap),
                Y = value
            };

            nodeToBox[node] = box;
            result.Boxes.Add(box);

            if (!collapsedNodes.Contains(node) && node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    BuildTree(child, depth + 1);

                    if (nodeToBox.TryGetValue(child, out var childBox))
                        result.Edges.Add(new LayoutEdge { From = childBox, To = box });
                }
            }
        }

        BuildTree(root, 0);
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
        if (node == null) 
            return depth;

        int max = depth;

        if (node.Children != null && node.Children.Count > 0)
        {
            foreach (var child in node.Children)
                max = Math.Max(max, GetMaxDepth(child, depth + 1));
        }

        return max;
    }
}