using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DQMBreedingTool.Models;

public class RecipeNode
{
    public int MonsterId { get; set; }
    public string DisplayName { get; set; } = "";
    public string Family { get; set; } = "";
    public string Rank { get; set; } = "";
    public string SourceLabel { get; set; } = "";
    public ImageSource? Icon { get; set; }
    public bool IsExpanded { get; set; } = true;

    public ObservableCollection<RecipeNode> Children { get; set; } = new();

    public bool IsRecipeGroup { get; set; }

    public string Header =>
        IsRecipeGroup
            ? $"◆ {SourceLabel}"
            : $"{DisplayName} ({Family} {Rank})";
}