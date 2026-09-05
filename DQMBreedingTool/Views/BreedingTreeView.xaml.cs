using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DQMBreedingTool.Models;

namespace DQMBreedingTool.Views;

public partial class BreedingTreeView : UserControl
{
    private RecipeNode? _currentRoot;
    private readonly HashSet<RecipeNode> _collapsedNodes = [];

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
        if (node == null || node.IsRecipeGroup) 
            return;

        if (!isRoot && HasValidChildren(node))
            _collapsedNodes.Add(node);

        foreach (var child in node.Children)
            InitAllCollapsed(child, isRoot: false);
    }

    private static bool HasValidChildren(RecipeNode node)
    {
        foreach (var child in node.Children)
        {
            if (!child.IsRecipeGroup) 
                return true;

            if (HasValidChildren(child)) 
                return true;
        }

        return false;
    }

    private void CollapseAllDescendants(RecipeNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.IsRecipeGroup)
            {
                CollapseAllDescendants(child);
                continue;
            }

            if (HasValidChildren(child))
                _collapsedNodes.Add(child);

            CollapseAllDescendants(child);
        }
    }

    private void ExpandAllDescendants(RecipeNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.IsRecipeGroup)
            {
                ExpandAllDescendants(child);
                continue;
            }

            _collapsedNodes.Remove(child);
            ExpandAllDescendants(child);
        }
    }

    private void RenderTree()
    {
        if (_currentRoot == null)
            return;

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
                RootCanvas.Children.Add(BuildToggleButton(box));
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
            Points =
            [
                new Point(fromX, fromY),
                new Point(midX, fromY),
                new Point(midX, toY),
                new Point(toX, toY)
            ]
        };

        return line;
    }

    Border BuildBoxVisual(LayoutBox box)
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
            Child = imageContent,
            ToolTip = BuildToolTip(node),
            ContextMenu = BuildContextMenu(node)
        };

        Canvas.SetLeft(border, box.X);
        Canvas.SetTop(border, box.Y);

        return border;
    }

    static ToolTip BuildToolTip(RecipeNode node)
    {
        var panel = new StackPanel { MaxWidth = 260 };

        panel.Children.Add(new TextBlock
        {
            Text = node.DisplayName,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 2)
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"{node.Family} {node.Rank}",
            FontSize = 11,
            Foreground = Brushes.LightGray,
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (node.ScoutAreas.Count > 0)
        {
            panel.Children.Add(new TextBlock
            {
                Text = "서식지",
                FontWeight = FontWeights.SemiBold,
                FontSize = 11,
                Foreground = Brushes.LightGreen,
                Margin = new Thickness(0, 0, 0, 2)
            });

            foreach (var area in node.ScoutAreas)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "• " + area.ToString(),
                    FontSize = 10,
                    Foreground = Brushes.LightGreen,
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }

        return new ToolTip
        {
            Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x2D)),
            BorderBrush = Brushes.SandyBrown,
            Content = panel
        };
    }

    ContextMenu BuildContextMenu(RecipeNode node)
    {
        var menu = new ContextMenu();

        var expandItem = new MenuItem
        {
            Header = "하위 전체 확장",
            Icon = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/Images/Expand.png", UriKind.Absolute)),
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
        
        expandItem.Click += (s, e) =>
        {
            ExpandAllDescendants(node);
            RenderTree();
        };

        var collapseItem = new MenuItem 
        { 
            Header = "하위 전체 축소",
            Icon = new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/Images/Collapse.png", UriKind.Absolute)),
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        collapseItem.Click += (s, e) =>
        {
            CollapseAllDescendants(node);
            RenderTree();
        };

        menu.Items.Add(expandItem);
        menu.Items.Add(collapseItem);

        return menu;
    }

    private Button BuildToggleButton(LayoutBox box)
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
                _collapsedNodes.Remove(box.Node);
            else
                _collapsedNodes.Add(box.Node);

            RenderTree();
        };

        Canvas.SetLeft(button, box.X + BreedingTreeLayout.BoxWidth + 2);
        Canvas.SetTop(button, box.Y + BreedingTreeLayout.BoxHeight / 2 - 8);

        return button;
    }
}