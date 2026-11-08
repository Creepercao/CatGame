using System;

namespace CatGame.Idle
{
    /// <summary>One balance transaction and an immutable snapshot of the first launch.</summary>
    public static class EndingRules
    {
        public const double RocketPrice=1e12;
        public static bool TryPurchase(GameState state)
        {
            if(state==null || state.rocketPurchased || double.IsNaN(state.catHair) || double.IsInfinity(state.catHair) || state.catHair<RocketPrice) return false;
            AchievementCatalog.EvaluateNew(state);
            StoryCatalog.EnqueueUnlocked(state);
            var snapshot=new EndingStatistics {
                playSeconds=state.playSeconds,totalEarned=state.totalEarned,clicks=state.clicks,
                achievementCount=state.achievements.Count,hasFullPlaytimeHistory=state.hasFullPlaytimeHistory,
                completedUnix=DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            foreach(var cat in state.cats) { snapshot.totalCats+=cat.count; if(cat.count>0) snapshot.breeds++; }
            foreach(var item in state.items) snapshot.itemLevels+=item.level;
            state.catHair-=RocketPrice;
            state.rocketPurchased=true;
            state.hasEndingSnapshot=true;
            state.firstEnding=snapshot;
            return true;
        }
    }
}
