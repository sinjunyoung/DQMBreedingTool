using DQMBreedingTool.Data;
using DQMBreedingTool.Models;
using DQMBreedingTool.Services;
using DQMBreedingTool.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace DQMBreedingTool;

public partial class MainWindow : Window
{
    // 2편, 3편... 추가할 땐 여기에 한 줄만 추가하면 됩니다.
    // (해당 리소스 zip을 Resources 폴더에 넣고 csproj에 EmbeddedResource로 등록해야 함)
    static readonly GameTitle[] Titles =
    [
        new GameTitle
        {
            Name = "테리의 원더랜드 3D",
            TabIconPath = "Assets/Images/DQM1.png",
            DataResource = "dqm1_data.zip",
            IconResource = "dqm1_icon.zip"
        }
    ];

    public MainWindow()
    {
        InitializeComponent();

        foreach (var title in Titles)
            TitleTabs.Items.Add(BuildTabItem(title));
    }

    static TabItem BuildTabItem(GameTitle title)
    {
        var repo = new DataRepository();
        repo.Load(title.DataResource, title.IconResource);

        var header = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

        if (!string.IsNullOrEmpty(title.TabIconPath))
        {
            header.Children.Add(new Image
            {
                Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(title.TabIconPath, UriKind.Relative)),
                Width = 32,
                Height = 32,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        header.Children.Add(new TextBlock { Text = title.Name, VerticalAlignment = VerticalAlignment.Center });

        return new TabItem
        {
            Header = header,
            Content = new GameTabView(repo)
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        IntPtr hWnd = new WindowInteropHelper(this).Handle;
        int value = 1;

        _ = Win32API.DwmSetWindowAttribute(hWnd, 20, ref value, sizeof(int));
    }
}
