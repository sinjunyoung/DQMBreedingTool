using DQMBreedingTool.Models;
using DQMBreedingTool.Services;
using System.Windows;
using System.Windows.Interop;

namespace DQMBreedingTool;

public partial class TreeWindow : Window
{
    public TreeWindow(RecipeNode root)
    {
        InitializeComponent();
        Title = $"배합 트리 - {root.DisplayName}";
        Loaded += (_, _) => GraphView.ShowTree(root);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        IntPtr hWnd = new WindowInteropHelper(this).Handle;
        int value = 1;

        _ = Win32API.DwmSetWindowAttribute(hWnd, 20, ref value, sizeof(int));
    }
}
