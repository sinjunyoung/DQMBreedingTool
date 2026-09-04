namespace DQMBreedingTool.Models;

public class RecipeEntry
{
    public int ChildId { get; set; }

    public List<int> ParentIds { get; set; } = [];

    public RecipeSource Source { get; set; }

    public int Sequence { get; set; }

    public string SourceLabel => Source switch
    {
        RecipeSource.Fixed => "고정배합",
        RecipeSource.Gold2Parent => "골드(2부모)",
        RecipeSource.Gold4Material => "골드(4재료)",
        RecipeSource.FourGeneration => "4체배합",
        RecipeSource.MonsLibFixed => "기본배합",
        _ => "",
    };
}