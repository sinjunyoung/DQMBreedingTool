using DQMBreedingTool.Data;
using DQMBreedingTool.Models;
using DQMBreedingTool.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DQMBreedingTool;

public partial class MainWindow : Window
{
    readonly DataRepository _repo = new();

    public MainWindow()
    {
        InitializeComponent();
        _repo.Load();
        InitFilters();
        RunSearch();
        InitGeneralCalculator();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        IntPtr hWnd = new WindowInteropHelper(this).Handle;
        int value = 1;

        _ = Win32API.DwmSetWindowAttribute(hWnd, 20, ref value, sizeof(int));
    }

    void InitFilters()
    {
        List<FilterItem> familyList = [];
        string[] fixedFamilies = ["전체", "슬라임계", "드래곤계", "자연계", "마수계", "물질계", "악마계", "좀비계", "???계"];

        foreach (var f in fixedFamilies)
        {
            familyList.Add(new FilterItem
            {
                Name = f,
                Icon = GetFamilyIcon(f)
            });
        }

        FamilyFilterCombo.ItemsSource = familyList;
        FamilyFilterCombo.SelectedIndex = 0;

        RankFilterCombo.Items.Add("전체");

        foreach (var r in _repo.Ranks)
            RankFilterCombo.Items.Add(r);

        RankFilterCombo.SelectedIndex = 0;
    }

    private static BitmapImage? GetFamilyIcon(string? familyName)
    {
        string? iconPath = familyName switch
        {
            "???계" => "pack://application:,,,/Assets/Images/Unknown.png",
            "드래곤계" => "pack://application:,,,/Assets/Images/Dragon.png",
            "마수계" => "pack://application:,,,/Assets/Images/Beast.png",
            "물질계" => "pack://application:,,,/Assets/Images/Material.png",
            "슬라임계" => "pack://application:,,,/Assets/Images/Slime.png",
            "악마계" => "pack://application:,,,/Assets/Images/Demon.png",
            "자연계" => "pack://application:,,,/Assets/Images/Nature.png",
            "좀비계" => "pack://application:,,,/Assets/Images/Zombie.png",
            _ => null
        };

        if (string.IsNullOrEmpty(iconPath)) 
            return null;

        try
        {
            return new BitmapImage(new Uri(iconPath));
        }
        catch
        {
            return null;
        }
    }

    void SearchButton_Click(object sender, RoutedEventArgs e) => RunSearch();

    void NameFilterBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            RunSearch();
    }

    void RunSearch()
    {
        string name = NameFilterBox.Text;
        string? family = null;

        if (FamilyFilterCombo.SelectedItem is FilterItem selectedFamilyItem)
            family = selectedFamilyItem.Name;

        string? rank = RankFilterCombo.SelectedItem as string;
        var results = _repo.Search(name, family, rank).ToList();

        ResultListView.ItemsSource = results;
        ResultCountText.Text = $"{results.Count}건";
    }

    void TreeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int monsterId) 
            return;

        var root = _repo.BuildTree(monsterId);
        var treeWindow = new TreeWindow(root) { Owner = this };

        treeWindow.Show();
    }

    void InitGeneralCalculator()
    {
        var monsterList = _repo.Monsters.Values
            .Where(m => !string.IsNullOrEmpty(m.Family))
            .OrderBy(m => m.Name)
            .ToList();
    }
}