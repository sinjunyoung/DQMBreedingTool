namespace DQMBreedingTool.Models;

using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

public class MonsterData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Scale { get; set; }
    public string Family { get; set; } = "";
    public string Rank { get; set; } = "";
    public int Wigye { get; set; }

    public ImageSource? Icon { get; set; }

    public int Hp { get; set; }
    public int Mp { get; set; }
    public int Atk { get; set; }
    public int Def { get; set; }
    public int Agi { get; set; }
    public int Wis { get; set; }

    public int Res1 { get; set; }
    public int Res2 { get; set; }
    public int Res3 { get; set; }
    public int Res4 { get; set; }
    public int Res5 { get; set; }

    public bool Breedable { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = "";
    public List<ScoutArea> ScoutAreas { get; set; } = [];
}

public class ScoutArea
{
    public string DungeonName { get; set; } = "";
    public List<int> NormalFloors { get; set; } = [];
    public List<int> BadWeatherFloors { get; set; } = [];

    public override string ToString()
    {
        var normal = string.Join(",", NormalFloors);
        var bad = string.Join(",", BadWeatherFloors);

        if (NormalFloors.SequenceEqual(BadWeatherFloors))
            return $"{DungeonName} {normal}층";

        var parts = new List<string>();

        if (NormalFloors.Count > 0) 
            parts.Add($"보통 {normal}층");

        if (BadWeatherFloors.Count > 0)
            parts.Add($"악천후 {bad}층");

        return $"{DungeonName} ({string.Join(" / ", parts)})";
    }
}