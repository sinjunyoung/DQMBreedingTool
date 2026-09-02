namespace DQMBreedingTool.Models;

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
}