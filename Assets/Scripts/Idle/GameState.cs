using System;
using System.Collections.Generic;

namespace CatGame.Idle
{
    /// <summary>Serializable player progress. Keep this model free of Unity scene/runtime state.</summary>
    [Serializable]
    public sealed class GameState
    {
        public const int CurrentVersion = 2;
        public int version = CurrentVersion;
        public double catHair;
        public double totalEarned;
        public int clicks;
        public List<CatCount> cats = new List<CatCount>();
        public List<ItemLevel> items = new List<ItemLevel>();
        public List<string> achievements = new List<string>();
        public long lastSeenUnix;
        public bool introCompleted;
        public List<string> seenStoryEvents = new List<string>();
        public List<string> pendingStoryEvents = new List<string>();
        public double playSeconds;
        public bool hasFullPlaytimeHistory = true;
        public bool rocketPurchased;
        public bool endingCompleted;
        public bool hasEndingSnapshot;
        public EndingStatistics firstEnding = new EndingStatistics();

        // Reserved for a future prestige feature; version 1 does not apply prestige bonuses.
        public int prestigeLevel;
        public double prestigeMultiplier = 1.0;
    }

    [Serializable]
    public sealed class EndingStatistics
    {
        public double playSeconds, totalEarned;
        public int clicks, totalCats, breeds, itemLevels, achievementCount;
        public long completedUnix;
        public bool hasFullPlaytimeHistory;
    }

    [Serializable]
    public sealed class CatCount
    {
        public string id;
        public int count;
    }

    [Serializable]
    public sealed class ItemLevel
    {
        public string id;
        public int level;
    }
}
