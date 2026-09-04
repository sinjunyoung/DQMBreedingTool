using DQMBreedingTool.Models;
using DQMBreedingTool.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace DQMBreedingTool;

public partial class TreeWindow : Window
{
    public TreeWindow(RecipeNode root)
    {
        InitializeComponent();

        Title = $"배합 트리 - {root.DisplayName}";
        RecipeTreeView.ItemsSource = new List<RecipeNode> { root };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        IntPtr hWnd = new WindowInteropHelper(this).Handle;
        int value = 1;

        _ = Win32API.DwmSetWindowAttribute(hWnd, 20, ref value, sizeof(int));
    }

    void ExpandAll_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is RecipeNode node)
            SetNodeExpansion(node, true);
    }

    void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.DataContext is RecipeNode node)
            SetNodeExpansion(node, false);
    }

    static void SetNodeExpansion(RecipeNode node, bool isExpanded)
    {
        node.IsExpanded = isExpanded;

        foreach (var child in node.Children)
            SetNodeExpansion(child, isExpanded);
    }
}