using System.Globalization;
using DQMBreedingTool.Models;

namespace DQMBreedingTool.Data;

public class DataRepository
{
    public Dictionary<int, MonsterData> Monsters { get; } = [];
    public List<RecipeEntry> Recipes { get; } = [];
    public Dictionary<int, List<RecipeEntry>> RecipesByChild { get; } = [];
    public List<string> Families { get; } = [];
    public List<string> Ranks { get; } = ["F", "E", "D", "C", "B", "A", "S", "SS"];
    Dictionary<int, System.Windows.Media.ImageSource> _icons = [];
    readonly Dictionary<string, List<MonsterData>> _byFamily = [];
    readonly HashSet<int> _wildOnlyConfirmed = [];

    public void Load(string dataResource, string iconResource)
    {
        var csvs = EmbeddedDataLoader.LoadCsvSet(dataResource);
        _icons = IconLoader.LoadIcons(iconResource);

        LoadMonsters(csvs["enmy_kind_full.csv"]);
        LoadScoutLocations(csvs["dungeon_names.csv"], csvs["area_monster.csv"]);
        LoadRecipes(csvs["combination_4g.csv"], RecipeSource.FourGeneration, 0, 2, 4, -1, 6, 8);
        LoadRecipes(csvs["mons_lib_recipe.csv"], RecipeSource.MonsLibFixed, 0, 2, 4, -1);
        LoadWildOnlyConfirmed(csvs["wild_only_confirmed.csv"]);

        Families.AddRange(Monsters.Values
            .Select(m => m.Family)
            .Where(f => !string.IsNullOrEmpty(f))
            .Distinct()
            .OrderBy(f => f));

        foreach (var r in Recipes)
        {
            if (_wildOnlyConfirmed.Contains(r.ChildId))
                continue;

            if (!RecipesByChild.TryGetValue(r.ChildId, out var list))
            {
                list = [];
                RecipesByChild[r.ChildId] = list;
            }

            list.Add(r);
        }

        BuildFamilyIndex();
    }

    void LoadWildOnlyConfirmed(string[] lines)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) 
                continue;

            var f = CsvLine.Split(lines[i]);

            if (f.Length < 1)
                continue;

            if (int.TryParse(f[0], out int id))
                _wildOnlyConfirmed.Add(id);
        }
    }

    void BuildFamilyIndex()
    {
        foreach (var m in Monsters.Values)
        {
            if (string.IsNullOrEmpty(m.Family)) 
                continue;

            if (!_byFamily.TryGetValue(m.Family, out var list))
            {
                list = [];
                _byFamily[m.Family] = list;
            }

            list.Add(m);
        }

        foreach (var list in _byFamily.Values)
            list.Sort((a, b) => a.Wigye.CompareTo(b.Wigye));
    }

    public MonsterData? NextInFamily(string family, int wigyeThreshold)
    {
        if (!_byFamily.TryGetValue(family, out var list)) 
            return null;

        foreach (var m in list)
            if (m.Wigye > wigyeThreshold) 
                return m;

        return null;
    }

    public List<MonsterData> GetFamilySorted(string family)
    {
        return _byFamily.TryGetValue(family, out var list) ? list : [];
    }

    void LoadMonsters(string[] lines)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) 
                continue;

            var f = CsvLine.Split(lines[i]);

            if (f.Length < 19) 
                continue;

            if (!int.TryParse(f[0], out int id))
                continue;

            if (string.IsNullOrWhiteSpace(f[1])) 
                continue;

            var m = new MonsterData
            {
                Id = id,
                Name = f[1],
                Scale = ParseD(f[2]),
                Family = f[3],
                Rank = f[4],
                Wigye = ParseI(f[5]),
                Hp = ParseI(f[6]),
                Mp = ParseI(f[7]),
                Atk = ParseI(f[8]),
                Def = ParseI(f[9]),
                Agi = ParseI(f[10]),
                Wis = ParseI(f[11]),
                Res1 = ParseI(f[12]),
                Res2 = ParseI(f[13]),
                Res3 = ParseI(f[14]),
                Res4 = ParseI(f[15]),
                Res5 = ParseI(f[16]),
                Breedable = f[17].Contains("가능"),
                SkillName = f.Length > 18 ? f[18] : string.Empty,
                Icon = _icons.TryGetValue(id, out var icon) ? icon : null
            };

            Monsters[id] = m;
        }
    }

    void LoadScoutLocations(string[] dungeonLines, string[] areaMonsterLines)
    {
        var dungeonNames = new Dictionary<int, string>();

        for (int i = 1; i < dungeonLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(dungeonLines[i])) 
                continue;

            var f = CsvLine.Split(dungeonLines[i]);

            if (f.Length < 2)
                continue;

            if (!int.TryParse(f[0], out int did)) 
                continue;

            dungeonNames[did] = f[1];
        }

        var areasByMonsterAndDungeon = new Dictionary<(int monsterId, int dungeonId), ScoutArea>();

        for (int i = 1; i < areaMonsterLines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(areaMonsterLines[i])) 
                continue;

            var f = CsvLine.Split(areaMonsterLines[i]);

            if (f.Length < 5) 
                continue;

            if (!int.TryParse(f[0], out int monsterId)) 
                continue;

            if (!int.TryParse(f[1], out int dungeonId)) 
                continue;

            if (!int.TryParse(f[2], out int floor))
                continue;

            if (!Monsters.TryGetValue(monsterId, out var m))
                continue;

            if (!dungeonNames.TryGetValue(dungeonId, out var dname))
                continue;

            var key = (monsterId, dungeonId);

            if (!areasByMonsterAndDungeon.TryGetValue(key, out var area))
            {
                area = new ScoutArea { DungeonName = dname };
                areasByMonsterAndDungeon[key] = area;
                m.ScoutAreas.Add(area);
            }

            if (f[3] == "1") 
                area.NormalFloors.Add(floor);

            if (f[4] == "1") 
                area.BadWeatherFloors.Add(floor);
        }
    }

    void LoadRecipes(string[] lines, RecipeSource source, int childCol, int p1Col, int p2Col, int seqCol, int p3Col = -1, int p4Col = -1)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            var f = CsvLine.Split(lines[i]);

            if (f.Length <= p2Col) 
                continue;

            if (!int.TryParse(f[childCol], out int child))
                continue;

            if (!int.TryParse(f[p1Col], out int p1))
                continue;

            if (!int.TryParse(f[p2Col], out int p2)) 
                continue;

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

        return BuildTreeInternal(monsterId, maxDepth, 0, visited);
    }

    RecipeNode BuildTreeInternal(int monsterId, int depth, int currentDepth, HashSet<int> visited)
    {
        Monsters.TryGetValue(monsterId, out var m);

        var node = new RecipeNode
        {
            MonsterId = monsterId,
            DisplayName = m?.Name ?? $"ID{monsterId}",
            Family = m?.Family ?? string.Empty,
            Rank = m?.Rank ?? string.Empty,
            ScoutAreas = m?.ScoutAreas ?? [],
            Icon = m?.Icon,

            IsExpanded = currentDepth < 1
        };

        if (depth <= 0 || visited.Contains(monsterId))
            return node;

        visited.Add(monsterId);

        if (_wildOnlyConfirmed.Contains(monsterId)) { }
        else if (RecipesByChild.TryGetValue(monsterId, out var recipes))
        {
            if (recipes.Count == 1)
            {
                foreach (var parentId in recipes[0].ParentIds)
                {
                    var childVisited = new HashSet<int>(visited);

                    node.Children.Add(BuildTreeInternal(parentId, depth - 1, currentDepth + 1, childVisited));
                }
            }
            else
            {
                foreach (var recipe in recipes)
                {
                    var groupNode = new RecipeNode
                    {
                        MonsterId = -1,
                        IsRecipeGroup = true,
                        SourceLabel = recipe.SourceLabel,
                        IsExpanded = currentDepth < 1
                    };

                    foreach (var parentId in recipe.ParentIds)
                    {
                        var childVisited = new HashSet<int>(visited);

                        groupNode.Children.Add(BuildTreeInternal(parentId, depth - 1, currentDepth + 1, childVisited));
                    }

                    node.Children.Add(groupNode);
                }
            }
        }
        else if (m != null && m.Rank != "F")
        {
            node.Children.Add(new RecipeNode
            {
                MonsterId = -1,
                IsRecipeGroup = true,
                SourceLabel = "고정 레시피 없음 — 야생포획 전용이거나 배합법 미확인",
                IsExpanded = false
            });
        }

        return node;
    }

    static int ParseI(string s) => int.TryParse(s, out var v) ? v : 0;

    static double ParseD(string s) => double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0;
}