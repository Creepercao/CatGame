using System;

namespace CatGame.Idle
{
    public sealed class ItemDefinition
    {
        public string id, name;
        public int generation;
        public bool click;
        public double effect, firstPrice;
    }

    public static class ItemCatalog
    {
        public const int GenerationCount = IdleBalance.StageCount;
        public const int ItemsPerGeneration = 4;
        public const int LevelsPerItem = IdleBalance.ItemLevelCap;
        public const double EffectGrowthPerLevel = IdleBalance.ItemEffectGrowthPerLevel;
        public const double PriceGrowthPerLevel = IdleBalance.ItemPriceGrowthPerLevel;
        public const double EffectGrowthPerGeneration = IdleBalance.ItemEffectGrowthPerGeneration;
        public const double PriceGrowthPerGeneration = IdleBalance.ItemPriceGrowthPerGeneration;
        public static readonly ItemDefinition[] All = Build();

        static ItemDefinition[] Build()
        {
            var items = new ItemDefinition[GenerationCount * ItemsPerGeneration];
            string[] names = { "猫抓板", "小窝垫", "自动喂食器", "毛球" };
            double[] effects = { .10, .25, .50, .50 };
            double[] prices = { 200, 5000, 120000, 1000 };
            for (int generation = 1; generation <= GenerationCount; generation++)
                for (int kind = 0; kind < ItemsPerGeneration; kind++)
                    items[(generation - 1) * ItemsPerGeneration + kind] = new ItemDefinition
                    {
                        id = $"item_{generation}_{kind + 1}",
                        name = generation == 1 ? names[kind] : $"{names[kind]} {generation}",
                        generation = generation,
                        click = kind == 3,
                        effect = kind == 3
                            ? effects[kind]
                            : effects[kind] * IdleBalance.ProductionEffectScale * Math.Pow(EffectGrowthPerGeneration, generation - 1),
                        firstPrice = prices[kind] * IdleBalance.ItemBasePriceScale * Math.Pow(PriceGrowthPerGeneration, generation - 1) * IdleBalance.ItemGenerationPriceScales[generation - 1]
                    };
            return items;
        }
    }
}
