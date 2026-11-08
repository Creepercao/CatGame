namespace CatGame.Idle
{
    /// <summary>Central tuning values. Keep structural rules (levels, caps, additive stacking) in code.</summary>
    public static class IdleBalance
    {
        // Balance coefficients shared by catalogs, runtime math, and curve simulation.
        public const int CatMaxOwned = 40;
        public const int MaxVisibleCats = 100;
        public const double CatPriceGrowth = 1.30;
        public const int ItemLevelCap = 5;
        public const double ItemEffectGrowthPerLevel = 2.0;
        public const double ItemPriceGrowthPerLevel = 4.0;
        public const double CurveClickRate = 0.5;
        public const int StageCount = 5;
        public const double ClickBaseValue = 0.5;
        public const double ClickStageGrowth = 1.0;
        public const double ClickLevelBonus = 0.5;
        public const double OfflineCapSeconds = 8 * 60 * 60;
        public const double OfflineFactor = 0.5;
        public const double Stage2Threshold = 1e5;
        public const double Stage3Threshold = 1e7;
        public const double Stage4Threshold = 1e9;
        public const double Stage5Threshold = 1e11;
        public const double WinThreshold = 1e12;

        // Cat price and income move together so each breed keeps its designed 4–5 second payback.
        public static readonly double[] CatStageScales = { 0.04, 0.10, 0.035, 0.15 };
        public static readonly double[] CatPriceScales = { 0.4, 10, 100, 10000 };

        public const double ItemBasePriceScale = 0.5;
        // Per-generation first-price tuning, applied on top of the nominal price growth.
        public static readonly double[] ItemGenerationPriceScales = { 1.0, 0.5, 0.7, 1.2, 1.2 };
        public const double ProductionEffectScale = 1.0;
        public const double ItemEffectGrowthPerGeneration = 6.0;
        public const double ItemPriceGrowthPerGeneration = 12.0;
    }
}
