using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using DQMBreedingTool.Models;

namespace DQMBreedingTool.Data;

public class DataRepository
{
    public Dictionary<int, MonsterData> Monsters { get; } = new();
    public List<RecipeEntry> Recipes { get; } = new();
    public Dictionary<int, List<RecipeEntry>> RecipesByChild { get; } = new();
    public List<string> Families { get; } = new();
    public List<string> Ranks { get; } = new() { "F", "E", "D", "C", "B", "A", "S", "SS" };
    Dictionary<int, System.Windows.Media.ImageSource> _icons = new();

    public void Load()
    {
        var csvs = EmbeddedDataLoader.LoadCsvSet("dqm_data.zip");
        _icons = IconLoader.LoadIcons("icon.zip");

        LoadMonsters(csvs["enmy_kind_full.csv"]);
        LoadRecipes(csvs["combination_kind.csv"], RecipeSource.Fixed, 4, 0, 2, -1);
        LoadRecipes(csvs["combination_gold_2parent.csv"], RecipeSource.Gold2Parent, 0, 2, 4, 6);
        LoadRecipes(csvs["combination_gold.csv"], RecipeSource.Gold4Material, 0, 2, 4, 10, 6, 8);
        LoadRecipes(csvs["combination_4g.csv"], RecipeSource.FourGeneration, 0, 2, 4, -1, 6, 8);

        Families.AddRange(Monsters.Values
            .Select(m => m.Family)
            .Where(f => !string.IsNullOrEmpty(f))
            .Distinct()
            .OrderBy(f => f));

        foreach (var r in Recipes)
        {
            if (!RecipesByChild.TryGetValue(r.ChildId, out var list))
            {
                list = new List<RecipeEntry>();
                RecipesByChild[r.ChildId] = list;
            }
            list.Add(r);
        }
    }

    void LoadMonsters(string[] lines)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var f = CsvLine.Split(lines[i]);
            if (f.Length < 18) continue;
            if (!int.TryParse(f[0], out int id)) continue;
            if (string.IsNullOrWhiteSpace(f[1])) continue;

            var m = new MonsterData
            {
                Id = id,
                Name = f[1],
                Scale = ParseD(f[2]),
                Family = f[3],
                Rank = f[4],
                Hp = ParseI(f[5]),
                Mp = ParseI(f[6]),
                Atk = ParseI(f[7]),
                Def = ParseI(f[8]),
                Agi = ParseI(f[9]),
                Wis = ParseI(f[10]),
                Res1 = ParseI(f[11]),
                Res2 = ParseI(f[12]),
                Res3 = ParseI(f[13]),
                Res4 = ParseI(f[14]),
                Res5 = ParseI(f[15]),
                Breedable = f[16].Contains("가능"),
                SkillName = f.Length > 17 ? f[17] : "",
            };
            m.Icon = _icons.TryGetValue(id, out var icon) ? icon : null;
            Monsters[id] = m;
        }
    }

    void LoadRecipes(string[] lines, RecipeSource source, int childCol, int p1Col, int p2Col, int seqCol, int p3Col = -1, int p4Col = -1)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var f = CsvLine.Split(lines[i]);
            if (f.Length <= p2Col) continue;

            if (!int.TryParse(f[childCol], out int child)) continue;
            if (!int.TryParse(f[p1Col], out int p1)) continue;
            if (!int.TryParse(f[p2Col], out int p2)) continue;

            var entry = new RecipeEntry
            {
                ChildId = child,
                Source = source,
                Sequence = (seqCol >= 0 && seqCol < f.Length && int.TryParse(f[seqCol], out int seq)) ? seq : 0,
            };
            entry.ParentIds.Add(p1);
            entry.ParentIds.Add(p2);

            if (p3Col >= 0 && p3Col < f.Length && int.TryParse(f[p3Col], out int p3))
                entry.ParentIds.Add(p3);
            if (p4Col >= 0 && p4Col < f.Length && int.TryParse(f[p4Col], out int p4))
                entry.ParentIds.Add(p4);

            Recipes.Add(entry);
        }
    }

    public IEnumerable<MonsterData> Search(string? nameFilter, string? familyFilter, string? rankFilter)
    {
        var query = Monsters.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(m => m.Name.Contains(nameFilter));

        if (!string.IsNullOrWhiteSpace(familyFilter) && familyFilter != "전체")
            query = query.Where(m => m.Family == familyFilter);

        if (!string.IsNullOrWhiteSpace(rankFilter) && rankFilter != "전체")
            query = query.Where(m => m.Rank == rankFilter);

        return query.OrderBy(m => m.Id);
    }

    public RecipeNode BuildTree(int monsterId, int maxDepth = 6)
    {
        var visited = new HashSet<int>();
        return BuildTreeInternal(monsterId, maxDepth, visited);
    }

    RecipeNode BuildTreeInternal(int monsterId, int depth, HashSet<int> visited)
    {
        Monsters.TryGetValue(monsterId, out var m);

        var node = new RecipeNode
        {
            MonsterId = monsterId,
            DisplayName = m?.Name ?? $"ID{monsterId}",
            Family = m?.Family ?? "",
            Rank = m?.Rank ?? "",
            Icon = m?.Icon,
        };

        if (depth <= 0 || visited.Contains(monsterId))
            return node;

        visited.Add(monsterId);

        if (RecipesByChild.TryGetValue(monsterId, out var recipes))
        {
            foreach (var recipe in recipes)
            {
                var groupNode = new RecipeNode
                {
                    MonsterId = -1,
                    IsRecipeGroup = true,
                    SourceLabel = recipe.Sequence > 0 ? $"{recipe.SourceLabel} #{recipe.Sequence}" : recipe.SourceLabel,
                };

                foreach (var parentId in recipe.ParentIds)
                {
                    var childVisited = new HashSet<int>(visited);
                    groupNode.Children.Add(BuildTreeInternal(parentId, depth - 1, childVisited));
                }

                node.Children.Add(groupNode);
            }
        }

        return node;
    }

    static int ParseI(string s) => int.TryParse(s, out var v) ? v : 0;
    static double ParseD(string s) => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
}