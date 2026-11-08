using System.Collections.Generic;

namespace CatGame.Idle
{
    public sealed class AchievementDefinition
    {
        public string id;
        public string name;
        public string condition;
    }

    /// <summary>Achievement names and unlock rules from the gameplay design.</summary>
    public static class AchievementCatalog
    {
        public static readonly AchievementDefinition[] All =
        {
            A("first_tuft", "第一撮毛", "累计猫毛达到 10"),
            A("ten_thousand", "猫毛过万", "累计猫毛达到 10,000"),
            A("hundred_thousand", "十万个为什么", "累计猫毛达到 100,000"),
            A("millionaire", "百万富翁", "累计猫毛达到 1,000,000"),
            A("lots_of_points", "亿点点", "累计猫毛达到 1e8"),
            A("unstoppable", "人类已经阻止不了", "累计猫毛达到 1e10"),
            A("family_of_ten", "一家十口", "拥有 10 只猫"),
            A("thirty_cats", "三十而立", "拥有 30 只猫"),
            A("hundred_cats", "百猫夜行", "拥有 100 只猫"),
            A("crowded_nest", "猫满为患", "拥有 300 只猫"),
            A("three_breeds", "集齐三色", "拥有 3 个品种"),
            A("six_breeds", "六种颜色", "拥有 6 个品种"),
            A("all_breeds", "十一色猫", "集齐全部 11 种猫"),
            A("first_click", "第一次撸猫", "点击猫 1 次"),
            A("thousand_clicks", "撸到手酸", "点击猫 1,000 次"),
            A("max_scratcher", "满级抓板", "任一代猫抓板升到 5 级"),
            A("stage_two", "街区新邻居", "拥有绅士猫"),
            A("stage_three", "城市之光", "拥有埃及猫"),
            A("stage_four", "云端猫窝", "拥有圣诞猫"),
            A("planet_cats", "猫猫覆盖星球", "累计猫毛达到 1e12"),
        };

        public static List<AchievementDefinition> EvaluateNew(GameState state)
        {
            var result = new List<AchievementDefinition>();
            if (state == null) return result;
            if (state.achievements == null) state.achievements = new List<string>();

            int totalCats = 0;
            int breeds = 0;
            if (state.cats != null)
                foreach (var row in state.cats)
                    if (row != null && row.count > 0) { totalCats += row.count; breeds++; }

            int stage = IdleMath.Stage(state);
            int maxScratcher = 0;
            if (state.items != null)
                foreach (var row in state.items)
                    if (row != null && row.id != null && row.id.StartsWith("item_") && row.id.EndsWith("_1"))
                        maxScratcher = System.Math.Max(maxScratcher, row.level);

            for (int i = 0; i < All.Length; i++)
            {
                var achievement = All[i];
                if (state.achievements.Contains(achievement.id) || !IsUnlocked(achievement.id, state, totalCats, breeds, stage, maxScratcher)) continue;
                state.achievements.Add(achievement.id);
                result.Add(achievement);
            }
            return result;
        }

        static bool Owns(GameState state, string id) { return state.cats != null && state.cats.Exists(c => c != null && c.id == id && c.count > 0); }

        static bool IsUnlocked(string id, GameState state, int totalCats, int breeds, int stage, int maxScratcher)
        {
            switch (id)
            {
                case "first_tuft": return state.totalEarned >= 10;
                case "ten_thousand": return state.totalEarned >= 1e4;
                case "hundred_thousand": return state.totalEarned >= 1e5;
                case "millionaire": return state.totalEarned >= 1e6;
                case "lots_of_points": return state.totalEarned >= 1e8;
                case "unstoppable": return state.totalEarned >= 1e10;
                case "family_of_ten": return totalCats >= 10;
                case "thirty_cats": return totalCats >= 30;
                case "hundred_cats": return totalCats >= 100;
                case "crowded_nest": return totalCats >= 300;
                case "three_breeds": return breeds >= 3;
                case "six_breeds": return breeds >= 6;
                case "all_breeds": return breeds >= CatCatalog.All.Length;
                case "first_click": return state.clicks >= 1;
                case "thousand_clicks": return state.clicks >= 1000;
                case "max_scratcher": return maxScratcher >= ItemCatalog.LevelsPerItem;
                case "stage_two": return Owns(state, "Tuxedo");
                case "stage_three": return Owns(state, "Egypt");
                case "stage_four": return Owns(state, "Xmas");
                case "planet_cats": return state.totalEarned >= IdleBalance.WinThreshold;
                default: return false;
            }
        }

        static AchievementDefinition A(string id, string name, string condition)
        {
            return new AchievementDefinition { id = id, name = name, condition = condition };
        }
    }
}


