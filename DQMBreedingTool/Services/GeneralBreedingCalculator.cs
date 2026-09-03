using System.Collections.Generic;
using System.Linq;
using DQMBreedingTool.Data;
using DQMBreedingTool.Models;

namespace DQMBreedingTool.Services;

public static class GeneralBreedingCalculator
{
    // Key: 엄마종족, Value: (아빠종족 -> 결과종족)
    // ttywiki.com/dqt/haigou.shtml 의 위계배합표 기준
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
        if (momFamily == dadFamily) return null;
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
            if (m == null) return;
            if (seen.Contains(m.Id))
            {
                var existing = results.Find(r => r.Monster.Id == m.Id);
                if (existing != null) existing.RuleLabel += " / " + label;
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

    /// <summary>
    /// 특정 몬스터(target)를 만들 수 있는 구체적인 부모 조합 예시를 찾는다.
    /// 실제로 Calculate()를 돌려서 target이 후보에 나오는지 검증까지 한 값만 반환한다.
    /// </summary>
    public static List<(MonsterData Dad, MonsterData Mom, string RuleLabel)> SuggestParents(MonsterData target, DataRepository repo)
    {
        var results = new List<(MonsterData, MonsterData, string)>();

        var famList = repo.GetFamilySorted(target.Family);
        int idx = famList.FindIndex(m => m.Id == target.Id);
        if (idx <= 0) return results; // 그 종족의 가장 낮은 위계면 일반배합으로 못 만듦(기초종)

        int prevWigye = famList[idx - 1].Wigye;

        // ①②: 동일 종족(바로 아래 위계 개체끼리)
        var same = famList[idx - 1];
        if (VerifyProduces(same, same, target, repo))
            results.Add((same, same, "① 동일 종족(위계배합)"));

        // ③: 교배 종족 - 7x7표에서 target.Family로 매핑되는 (엄마종족,아빠종족) 조합 전부 탐색
        foreach (var momFam in repo.Families)
        {
            foreach (var dadFam in repo.Families)
            {
                if (momFam == dadFam) continue;
                if (GetCrossFamily(momFam, dadFam) != target.Family) continue;

                var momList = repo.GetFamilySorted(momFam);
                var dadList = repo.GetFamilySorted(dadFam);
                if (momList.Count == 0 || dadList.Count == 0) continue;

                // 낮은위계 후보를 엄마쪽에서 찾고, 아빠쪽에서 그보다 위계 높은(또는 같은) 상대를 찾는 경우
                var momLow = momList.FirstOrDefault(m => m.Wigye <= prevWigye) ?? momList.First();
                var dadHigh = dadList.LastOrDefault(m => m.Wigye >= momLow.Wigye) ?? dadList.Last();
                if (VerifyProduces(dadHigh, momLow, target, repo))
                {
                    results.Add((dadHigh, momLow, $"③ 교배 종족 ({momFam}×{dadFam})"));
                    continue;
                }

                // 반대로 낮은위계 후보가 아빠쪽인 경우
                var dadLow = dadList.FirstOrDefault(m => m.Wigye <= prevWigye) ?? dadList.First();
                var momHigh = momList.LastOrDefault(m => m.Wigye >= dadLow.Wigye) ?? momList.Last();
                if (VerifyProduces(dadLow, momHigh, target, repo))
                {
                    results.Add((dadLow, momHigh, $"③ 교배 종족 ({momFam}×{dadFam})"));
                }
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
