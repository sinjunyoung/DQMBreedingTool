using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using DQMBreedingTool.Models;
using DQMBreedingTool.Services;

namespace DQMBreedingTool.Data;

public class DataRepository
{
    public Dictionary<int, MonsterData> Monsters { get; } = new();
    public List<RecipeEntry> Recipes { get; } = new();
    public Dictionary<int, List<RecipeEntry>> RecipesByChild { get; } = new();
    public List<string> Families { get; } = new();
    public List<string> Ranks { get; } = new() { "F", "E", "D", "C", "B", "A", "S", "SS" };
    Dictionary<int, System.Windows.Media.ImageSource> _icons = new();
    Dictionary<string, List<MonsterData>> _byFamily = new();
    HashSet<int> _wildOnlyConfirmed = new();

    public void Load()
    {
        var csvs = EmbeddedDataLoader.LoadCsvSet("dqm_data.zip");
        _icons = IconLoader.LoadIcons("icon.zip");

        LoadMonsters(csvs["enmy_kind_full.csv"]);
        // CombinationKindTbl.bin(고정배합371)은 MonsLibTbl의 파생/체인 데이터로 밝혀져 사용 안 함(2024 재검증)
        LoadRecipes(csvs["combination_gold_2parent.csv"], RecipeSource.Gold2Parent, 0, 2, 4, 6);
        LoadRecipes(csvs["combination_gold.csv"], RecipeSource.Gold4Material, 0, 2, 4, 10, 6, 8);
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
            if (_wildOnlyConfirmed.Contains(r.ChildId)) continue; // MonsLibTbl에서 부모없음이 확정된 몬스터는 다른 테이블 기록을 무시함

            if (!RecipesByChild.TryGetValue(r.ChildId, out var list))
            {
                list = new List<RecipeEntry>();
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
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var f = CsvLine.Split(lines[i]);
            if (f.Length < 1) continue;
            if (int.TryParse(f[0], out int id))
                _wildOnlyConfirmed.Add(id);
        }
    }

    void BuildFamilyIndex()
    {
        foreach (var m in Monsters.Values)
        {
            if (string.IsNullOrEmpty(m.Family)) continue;
            if (!_byFamily.TryGetValue(m.Family, out var list))
            {
                list = new List<MonsterData>();
                _byFamily[m.Family] = list;
            }
            list.Add(m);
        }

        foreach (var list in _byFamily.Values)
            list.Sort((a, b) => a.Wigye.CompareTo(b.Wigye));
    }

    public MonsterData? NextInFamily(string family, int wigyeThreshold)
    {
        if (!_byFamily.TryGetValue(family, out var list)) return null;
        foreach (var m in list)
        {
            if (m.Wigye > wigyeThreshold) return m;
        }
        return null;
    }

    public List<MonsterData> GetFamilySorted(string family)
    {
        return _byFamily.TryGetValue(family, out var list) ? list : new List<MonsterData>();
    }

    void LoadMonsters(string[] lines)
    {
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var f = CsvLine.Split(lines[i]);
            if (f.Length < 19) continue;
            if (!int.TryParse(f[0], out int id)) continue;
            if (string.IsNullOrWhiteSpace(f[1])) continue;

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
                SkillName = f.Length > 18 ? f[18] : "",
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
        return BuildTreeInternal(monsterId, maxDepth, 0, visited);
    }

    RecipeNode BuildTreeInternal(int monsterId, int depth, int currentDepth, HashSet<int> visited)
    {
        Monsters.TryGetValue(monsterId, out var m);

        var node = new RecipeNode
        {
            MonsterId = monsterId,
            DisplayName = m?.Name ?? $"ID{monsterId}",
            Family = m?.Family ?? "",
            Rank = m?.Rank ?? "",
            Icon = m?.Icon,

            IsExpanded = currentDepth < 1
        };

        if (depth <= 0 || visited.Contains(monsterId))
            return node;

        visited.Add(monsterId);

        if (_wildOnlyConfirmed.Contains(monsterId))
        {
            //node.Children.Add(new RecipeNode
            //{
            //    MonsterId = -1,
            //    IsRecipeGroup = true,
            //    SourceLabel = "야생포획 전용 (배합 불가 확정)",
            //    IsExpanded = false
            //});
        }
        else if (RecipesByChild.TryGetValue(monsterId, out var recipes))
        {
            foreach (var recipe in recipes)
            {
                foreach (var parentId in recipe.ParentIds)
                {
                    var childVisited = new HashSet<int>(visited);
                    node.Children.Add(BuildTreeInternal(parentId, depth - 1, currentDepth + 1, childVisited));
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