using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DQMBreedingTool.Models;

namespace DQMBreedingTool.Views;

public partial class BreedingTreeView : UserControl
{
    private RecipeNode? _currentRoot;
    private readonly HashSet<RecipeNode> _collapsedNodes = new();

    public BreedingTreeView()
    {
        InitializeComponent();
    }

    public void ShowTree(RecipeNode root)
    {
        _currentRoot = root;
        _collapsedNodes.Clear();

        InitAllCollapsed(root, isRoot: true);
        RenderTree();
    }

    private void InitAllCollapsed(RecipeNode node, bool isRoot)
    {
        if (node == null || node.IsRecipeGroup) return;

        if (!isRoot && HasValidChildren(node))
        {
            _collapsedNodes.Add(node);
        }

        foreach (var child in node.Children)
        {
            InitAllCollapsed(child, isRoot: false);
        }
    }

    private bool HasValidChildren(RecipeNode node)
    {
        foreach (var child in node.Children)
        {
            if (!child.IsRecipeGroup) return true;
            if (HasValidChildren(child)) return true;
        }
        return false;
    }

    private void RenderTree()
    {
        if (_currentRoot == null) return;

        RootCanvas.Children.Clear();

        var layout = BreedingTreeLayout.Compute(_currentRoot, _collapsedNodes);

        RootCanvas.Width = layout.Width;
        RootCanvas.Height = layout.Height;

        foreach (var edge in layout.Edges)
            RootCanvas.Children.Add(BuildConnector(edge));

        foreach (var box in layout.Boxes)
        {
            RootCanvas.Children.Add(BuildBoxVisual(box));

            if (HasValidChildren(box.Node))
            {
                RootCanvas.Children.Add(BuildToggleButton(box));
            }
        }
    }

    static Polyline BuildConnector(LayoutEdge edge)
    {
        double fromX = edge.From.X + BreedingTreeLayout.BoxWidth;
        double fromY = edge.From.Y + BreedingTreeLayout.BoxHeight / 2;
        double toX = edge.To.X;
        double toY = edge.To.Y + BreedingTreeLayout.BoxHeight / 2;
        double midX = (fromX + toX) / 2;

        var line = new Polyline
        {
            Stroke = Brushes.OrangeRed,
            StrokeThickness = 2,
            Points = new PointCollection
            {
                new Point(fromX, fromY),
                new Point(midX, fromY),
                new Point(midX, toY),
                new Point(toX, toY)
            }
        };
        return line;
    }

    static Border BuildBoxVisual(LayoutBox box)
    {
        var node = box.Node;

        UIElement? imageContent = null;
        if (node.Icon != null)
        {
            imageContent = new Image
            {
                Source = node.Icon,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(4)
            };
        }

        var border = new Border
        {
            Width = BreedingTreeLayout.BoxWidth,
            Height = BreedingTreeLayout.BoxHeight,
            Background = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38)),
            BorderBrush = Brushes.SandyBrown,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Child = imageContent
        };

        string toolTipText = $"{node.DisplayName} ({node.Family} {node.Rank})";
        if (node.ScoutAreas.Count > 0)
        {
            var dungeonNames = node.ScoutAreas.Select(a => a.DungeonName).Distinct();
            toolTipText += "\n" + string.Join(", ", dungeonNames);
        }
        border.ToolTip = toolTipText;

        Canvas.SetLeft(border, box.X);
        Canvas.SetTop(border, box.Y);
        return border;
    }

    private FrameworkElement BuildToggleButton(LayoutBox box)
    {
        bool isCollapsed = _collapsedNodes.Contains(box.Node);
        var button = new Button
        {
            Content = isCollapsed ? "▶" : "▼",
            Width = 16,
            Height = 16,
            FontSize = 8,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(0x3E, 0x3E, 0x48)),
            Foreground = Brushes.White,
            BorderBrush = Brushes.SandyBrown,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        button.Click += (s, e) =>
        {
            if (isCollapsed)
            {
                _collapsedNodes.Remove(box.Node);
            }
            else
            {
                _collapsedNodes.Add(box.Node);
            }

            RenderTree();
        };

        Canvas.SetLeft(button, box.X + BreedingTreeLayout.BoxWidth + 2);
        Canvas.SetTop(button, box.Y + BreedingTreeLayout.BoxHeight / 2 - 8);

        return button;
    }
}