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
        bool hasChildren = HasValidChildren(node);
        bool isCollapsed = _collapsedNodes.Contains(node);

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
                Margin = new Thickness(2, 2, 2, 2)
            });

            var familyConverter = new Converters.FamilyToIconConverter();
            if (familyConverter.Convert(node.Family, typeof(ImageSource), 0, CultureInfo.CurrentCulture) is BitmapImage familyIconImg)
            {
                grid.Children.Add(new Rectangle
                {
                    Width = 12,
                    Height = 12,
                    RadiusX = 1,
                    RadiusY = 1,
                    Margin = new Thickness(2, 2, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    Fill = new ImageBrush(familyIconImg)
                    {
                        Stretch = Stretch.Uniform
                    }
                });
            }

            if (!string.IsNullOrEmpty(node.Rank))
            {
                var rankGrid = new Grid
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 1, 3, 0)
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

        if (hasChildren && content is Grid boxGrid)
        {
            string indicatorText = isCollapsed ? "+" : "-";

            var badgeGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 2, 2),
                Background = new SolidColorBrush(Color.FromArgb(0x90, 0x00, 0x00, 0x00)),
                Width = 8,
                Height = 8
            };

            badgeGrid.Children.Add(new TextBlock
            {
                Text = indicatorText,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -1, 0, 0)
            });

            boxGrid.Children.Add(badgeGrid);
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

        if (hasChildren)
        {
            border.Cursor = System.Windows.Input.Cursors.Hand;
            border.MouseLeftButtonUp += (s, e) =>
            {
                if (_collapsedNodes.Contains(node))
                    _collapsedNodes.Remove(node);
                else
                    _collapsedNodes.Add(node);

                RenderTree();
                e.Handled = true;
            };
        }

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
                                Text = $"   - {normalStr}층 (맑음)",
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
                                    Text = $"   - {badStr}층 (악천후)",
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

        var toolTip = new ToolTip
        {
            Content = panel,
            HasDropShadow = true
        };

        var template = new ControlTemplate(typeof(ToolTip));
        var borderFactory = new FrameworkElementFactory(typeof(Border));

        borderFactory.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x2D)));
        borderFactory.SetValue(Border.BorderBrushProperty, Brushes.SandyBrown);
        borderFactory.SetValue(Border.BorderThicknessProperty, new Thickness(1.5));
        borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(6));

        var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));

        contentPresenterFactory.SetValue(ContentPresenter.MarginProperty, new Thickness(6));
        borderFactory.AppendChild(contentPresenterFactory);

        template.VisualTree = borderFactory;
        toolTip.Template = template;

        return toolTip;
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
}