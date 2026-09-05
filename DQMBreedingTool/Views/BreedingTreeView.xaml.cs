using DQMBreedingTool.Models;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
        if (node == null)
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
        if (HasValidChildren(node))
            _collapsedNodes.Add(node);

        foreach (var child in node.Children)
        {
            if (HasValidChildren(child))
                _collapsedNodes.Add(child);

            CollapseAllDescendants(child);
        }
    }

    private void ExpandAllDescendants(RecipeNode node)
    {
        _collapsedNodes.Remove(node);

        foreach (var child in node.Children)
        {
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
        UIElement content;
        Brush backgroundColor;

        if (node.IsRecipeGroup)
        {
            string groupText = node.Children.Count == 4 ? "4체" : "2체";

            var grid = new Grid();
            grid.Children.Add(new TextBlock
            {
                Text = groupText,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });

            content = grid;
            backgroundColor = new SolidColorBrush(Color.FromRgb(0x45, 0x35, 0x25));
        }
        else
        {
            var grid = new Grid();

            grid.Children.Add(new Image
            {
                Source = node.Icon,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2)
            });

            var familyConverter = new Converters.FamilyToIconConverter();
            if (familyConverter.Convert(node.Family, typeof(ImageSource), 0, CultureInfo.CurrentCulture) is BitmapImage familyIconImg)
            {
                grid.Children.Add(new Image
                {
                    Source = familyIconImg,
                    Width = 12,
                    Height = 12,
                    Stretch = Stretch.Uniform,
                    Opacity = 1,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Bottom
                });
            }

            if (!string.IsNullOrEmpty(node.Rank))
            {
                var rankGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 1, 3, 0),
                    Opacity = 0.8
                };

                int[] offsets = [-1, 1];

                foreach (int x in offsets)
                {
                    foreach (int y in offsets)
                    {
                        rankGrid.Children.Add(new TextBlock
                        {
                            Text = node.Rank,
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            Foreground = Brushes.Black,
                            Margin = new Thickness(x, y, -x, -y)
                        });
                    }
                }

                rankGrid.Children.Add(new TextBlock
                {
                    Text = node.Rank,
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Yellow
                });

                grid.Children.Add(rankGrid);
            }

            content = grid;
            backgroundColor = new SolidColorBrush(Color.FromRgb(0x2E, 0x2E, 0x38));
        }

        var border = new Border
        {
            Width = BreedingTreeLayout.BoxWidth,
            Height = BreedingTreeLayout.BoxHeight,
            Background = backgroundColor,
            BorderBrush = Brushes.SandyBrown,
            BorderThickness = new Thickness(2),
            CornerRadius = new CornerRadius(8),
            Child = content,
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

        if (!node.IsRecipeGroup && node.Icon != null)
        {
            panel.Children.Add(new Image
            {
                Source = node.Icon,
                Width = 32,
                Height = 32,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            });
        }

        panel.Children.Add(new TextBlock
        {
            Text = node.DisplayName,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.Yellow,
            FontSize = 13,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 2)
        });

        if (!node.IsRecipeGroup)
        {
            var familyRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };

            var familyConverter = new Converters.FamilyToIconConverter();
            if (familyConverter.Convert(node.Family, typeof(ImageSource), 0, CultureInfo.CurrentCulture) is BitmapImage familyIconImg)
            {
                familyRow.Children.Add(new Image
                {
                    Source = familyIconImg,
                    Width = 14,
                    Height = 14,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0)
                });
            }

            familyRow.Children.Add(new TextBlock
            {
                Text = $"{node.Family} {node.Rank}",
                FontSize = 11,
                Foreground = Brushes.LightGray,
                VerticalAlignment = VerticalAlignment.Center
            });

            panel.Children.Add(familyRow);

            if (node.ScoutAreas.Count > 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "서식지",
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 11,
                    Foreground = Brushes.LightGreen,
                    Margin = new Thickness(0, 4, 0, 2)
                });

                var dungeonGroups = node.ScoutAreas.GroupBy(a => a.DungeonName);

                foreach (var group in dungeonGroups)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"• {group.Key}",
                        FontWeight = FontWeights.Medium,
                        FontSize = 10.5,
                        Foreground = Brushes.LightGreen,
                        Margin = new Thickness(0, 2, 0, 1)
                    });

                    foreach (var area in group)
                    {
                        if (area.NormalFloors != null && area.NormalFloors.Count > 0)
                        {
                            var normalStr = string.Join(",", area.NormalFloors);
                            panel.Children.Add(new TextBlock
                            {
                                Text = $"   - {normalStr} (맑음)",
                                FontSize = 10,
                                Foreground = Brushes.LightGray,
                                Margin = new Thickness(0, 0, 0, 1)
                            });
                        }

                        if (area.BadWeatherFloors != null && area.BadWeatherFloors.Count > 0)
                        {
                            if (area.NormalFloors == null || !area.NormalFloors.SequenceEqual(area.BadWeatherFloors))
                            {
                                var badStr = string.Join(",", area.BadWeatherFloors);

                                panel.Children.Add(new TextBlock
                                {
                                    Text = $"   - {badStr} (악천후)",
                                    FontSize = 10,
                                    Foreground = Brushes.LightSkyBlue,
                                    Margin = new Thickness(0, 0, 0, 1)
                                });
                            }
                        }
                    }
                }
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
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/Expand.png", UriKind.Absolute)),
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
                Source = new BitmapImage(new Uri("pack://application:,,,/Assets/Images/Collapse.png", UriKind.Absolute)),
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