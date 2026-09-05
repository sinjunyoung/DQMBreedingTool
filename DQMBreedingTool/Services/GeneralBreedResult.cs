using DQMBreedingTool.Models;

namespace DQMBreedingTool.Services;

public class GeneralBreedResult
{
    public MonsterData Monster { get; set; } = null!;
    public string RuleLabel { get; set; } = "";
}
