using DQMBreedingTool.Data;
using DQMBreedingTool.Models;

namespace DQMBreedingTool.Services;

public static class GeneralBreedingCalculator
{
    static readonly Dictionary<string, Dictionary<string, string>> CrossTable = new()
    {
        ["슬라임계"] = new() { ["드래곤계"] = "물질계", ["자연계"] = "드래곤계", ["마수계"] = "좀비계", ["물질계"] = "악마계", ["악마계"] = "좀비계", ["좀비계"] = "마수계" },
        ["드래곤계"] = new() { ["슬라임계"] = "물질계", ["자연계"] = "마수계", ["마수계"] = "물질계", ["물질계"] = "악마계", ["악마계"] = "좀비계", ["좀비계"] = "슬라임계" },
        ["자연계"] = new() { ["슬라임계"] = "드래곤계", ["드래곤계"] = "마수계", ["마수계"] = "드래곤계", ["물질계"] = "마수계", ["악마계"] = "슬라임계", ["좀비계"] = "악마계" },
        ["마수계"] = new() { ["슬라임계"] = "좀비계", ["드래곤계"] = "물질계", ["자연계"] = "드래곤계", ["물질계"] = "자연계", ["악마계"] = "드래곤계", ["좀비계"] = "물질계" },
        ["물질계"] = new() { ["슬라임계"] = "악마계", ["드래곤계"] = "악마계", ["자연계"] = "마수계", ["마수계"] = "자연계", ["악마계"] = "자연계", ["좀비계"] = "슬라임계" },
        ["악마계"] = new() { ["슬라임계"] = "좀비계", ["드래곤계"] = "좀비계", ["자연계"] = "슬라임계", ["마수계"] = "드래곤계", ["물질계"] = "자연계", ["좀비계"] = "자연계" },
        ["좀비계"] = new() { ["슬라임계"] = "마수계", ["드래곤계"] = "슬라임계", ["자연계"] = "악마계", ["마수계"] = "물질계", ["물질계"] = "슬라임계", ["악마계"] = "자연계" },
    };

    public static string? GetCrossFamily(string momFamily, string dadFamily)
    {
        if (momFamily == dadFamily) 
            return null;

        if (CrossTable.TryGetValue(momFamily, out var row) && row.TryGetValue(dadFamily, out var result))
            return result;

        return null;
    }

    public static List<GeneralBreedResult> Calculate(MonsterData dad, MonsterData mom, DataRepository repo)
    {
        var results = new List<GeneralBreedResult>();
        var seen = new HashSet<int>();

        void AddCandidate(MonsterData? m, string label)
        {
            if (m == null)
                return;

            if (seen.Contains(m.Id))
            {
                var existing = results.Find(r => r.Monster.Id == m.Id);

                if (existing != null)
                    existing.RuleLabel += " / " + label;

                return;
            }
            seen.Add(m.Id);
            results.Add(new GeneralBreedResult { Monster = m, RuleLabel = label });
        }

        var higher = dad.Wigye >= mom.Wigye ? dad : mom;
        var lower = dad.Wigye >= mom.Wigye ? mom : dad;

        if (dad.Family == mom.Family)
        {
            var candidate = repo.NextInFamily(dad.Family, higher.Wigye);

            AddCandidate(candidate, "① 동일 종족 (①=②)");

            return results;
        }

        var c1 = repo.NextInFamily(dad.Family, higher.Wigye);

        AddCandidate(c1, "① 아빠 종족");

        var c2 = repo.NextInFamily(mom.Family, higher.Wigye);

        AddCandidate(c2, "② 엄마 종족");

        var crossFamily = GetCrossFamily(mom.Family, dad.Family);

        if (crossFamily != null)
        {
            var c3 = repo.NextInFamily(crossFamily, lower.Wigye);

            AddCandidate(c3, $"③ 교배 종족 ({crossFamily})");
        }

        return results;
    }

    public static List<(MonsterData Dad, MonsterData Mom, string RuleLabel)> SuggestParents(MonsterData target, DataRepository repo)
    {
        var results = new List<(MonsterData, MonsterData, string)>();
        var famList = repo.GetFamilySorted(target.Family);
        int idx = famList.FindIndex(m => m.Id == target.Id);

        if (idx <= 0) 
            return results;

        int prevWigye = famList[idx - 1].Wigye;
        var same = famList[idx - 1];

        if (VerifyProduces(same, same, target, repo))
            results.Add((same, same, "① 동일 종족(위계배합)"));

        foreach (var momFam in repo.Families)
        {
            foreach (var dadFam in repo.Families)
            {
                if (momFam == dadFam) 
                    continue;

                if (GetCrossFamily(momFam, dadFam) != target.Family) 
                    continue;

                var momList = repo.GetFamilySorted(momFam);
                var dadList = repo.GetFamilySorted(dadFam);

                if (momList.Count == 0 || dadList.Count == 0) 
                    continue;

                var momLow = momList.FirstOrDefault(m => m.Wigye <= prevWigye) ?? momList.First();
                var dadHigh = dadList.LastOrDefault(m => m.Wigye >= momLow.Wigye) ?? dadList.Last();

                if (VerifyProduces(dadHigh, momLow, target, repo))
                {
                    results.Add((dadHigh, momLow, $"③ 교배 종족 ({momFam}×{dadFam})"));
                    continue;
                }

                var dadLow = dadList.FirstOrDefault(m => m.Wigye <= prevWigye) ?? dadList.First();
                var momHigh = momList.LastOrDefault(m => m.Wigye >= dadLow.Wigye) ?? momList.Last();

                if (VerifyProduces(dadLow, momHigh, target, repo))
                    results.Add((dadLow, momHigh, $"③ 교배 종족 ({momFam}×{dadFam})"));
            }
        }

        return results;
    }

    static bool VerifyProduces(MonsterData dad, MonsterData mom, MonsterData target, DataRepository repo)
    {
        var candidates = Calculate(dad, mom, repo);

        return candidates.Exists(c => c.Monster.Id == target.Id);
    }
}