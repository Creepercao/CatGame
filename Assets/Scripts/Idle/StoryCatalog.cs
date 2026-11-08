namespace CatGame.Idle
{
    public sealed class StoryDefinition
    {
        public readonly string id, title, caption, resource;
        public StoryDefinition(string id,string title,string caption,string resource)
        { this.id=id; this.title=title; this.caption=caption; this.resource="Art/Story/"+resource; }
    }

    /// <summary>One distinct memory for every achievement, ordered by AchievementCatalog.</summary>
    public static class StoryCatalog
    {
        public static readonly StoryDefinition[] All = {
            new StoryDefinition("first_tuft","第一撮毛","小小一撮柔软的毛，是这个家最初的收藏。","AchievementFirstTuft_v1"),
            new StoryDefinition("ten_thousand","猫毛过万","不知不觉，小小的收藏已经装满了一篮。","AchievementTenThousand_v1"),
            new StoryDefinition("hundred_thousand","十万个为什么","猫毛越攒越多，好奇心也跟着长大。","AchievementHundredThousand_v1"),
            new StoryDefinition("millionaire","百万富翁","最珍贵的财富，是这些毛茸茸的日常。","AchievementMillionaire_v1"),
            new StoryDefinition("lots_of_points","亿点点","只是亿点点喜欢，就让猫毛堆成了小山。","AchievementLotsOfPoints_v1"),
            new StoryDefinition("unstoppable","人类已经阻止不了","从一间小屋开始，温柔正在占领整座城市。","AchievementUnstoppable_v1"),
            new StoryDefinition("family_of_ten","一家十口","十份呼噜声，刚好填满一个家。","AchievementFamilyOfTen_v1"),
            new StoryDefinition("thirty_cats","三十而立","猫窝更热闹了，每一个角落都有回应。","AchievementThirtyCats_v1"),
            new StoryDefinition("hundred_cats","百猫夜行","夜色降临，百只小小的脚步仍走在一起。","AchievementHundredCats_v1"),
            new StoryDefinition("crowded_nest","猫满为患","再挤一点也没关系，这里永远留着你的位置。","AchievementCrowdedNest_v1"),
            new StoryDefinition("three_breeds","集齐三色","第一批伙伴到来，猫窝有了新的颜色。","MilestoneThree_v1"),
            new StoryDefinition("six_breeds","六种颜色","多一点热闹，也多一点陪伴。","MilestoneSix_v1"),
            new StoryDefinition("all_breeds","十一色猫","每一种颜色，都是这个家不可缺少的一部分。","MilestoneEleven_v1"),
            new StoryDefinition("first_click","第一次撸猫","第一次摸摸，换来一声轻轻的呼噜。","AchievementFirstClick_v1"),
            new StoryDefinition("thousand_clicks","撸到手酸","第一千次摸摸，依然是最喜欢的那一下。","AchievementThousandClicks_v1"),
            new StoryDefinition("max_scratcher","满级抓板","这块抓板，已经见证了最认真的磨爪时光。","AchievementMaxScratcher_v1"),
            new StoryDefinition("stage_two","街区新邻居","门铃响起，穿着黑白礼服的新朋友来报到了。","AchievementNewNeighbor_v1"),
            new StoryDefinition("stage_three","城市之光","在城市的灯火里，我们遇见了那双明亮的眼睛。","AchievementCityLights_v1"),
            new StoryDefinition("stage_four","云端猫窝","圣诞猫带来一个梦：把温暖的小窝搬到云上。","AchievementCloudNest_v1"),
            new StoryDefinition("planet_cats","猫猫覆盖星球","把猫毛攒成梦想，下一站会是哪里呢？","MilestonePlanet_v1")
        };
        public static StoryDefinition Find(string id)
        { foreach(var story in All) if(story.id==id) return story; return null; }

        public static bool EnqueueUnlocked(GameState state)
        {
            bool changed=false;
            foreach(var story in All)
                if(state.achievements.Contains(story.id) && !state.seenStoryEvents.Contains(story.id) && !state.pendingStoryEvents.Contains(story.id))
                { state.pendingStoryEvents.Add(story.id); changed=true; }
            return changed;
        }
        public static void Complete(GameState state,string id)
        {
            state.pendingStoryEvents.Remove(id);
            if(!state.seenStoryEvents.Contains(id)) state.seenStoryEvents.Add(id);
        }
    }
}
