using System;
using System.Collections.Generic;

namespace CatGame.Idle
{
    public static class IdleMath
    {
        public static double BasePerSecond(GameState s) { double n=0; foreach(var c in CatCatalog.All) n+=c.income*Owned(s,c.id); return n; }
        public static double ItemMultiplier(GameState s) { double sum=0; foreach(var i in ItemCatalog.All) if(!i.click) { var level=Level(s,i.id); if(level>0) sum+=i.effect*Math.Pow(2,level-1); } return 1+sum; }
        public static double ApplyProduction(GameState state, double ratePerSecond, double elapsedSeconds)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (double.IsNaN(ratePerSecond) || double.IsInfinity(ratePerSecond) || ratePerSecond < 0)
                throw new ArgumentOutOfRangeException(nameof(ratePerSecond), "Production rate must be finite and non-negative.");
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed time must be finite and non-negative.");
            if (elapsedSeconds == 0 || ratePerSecond == 0) return 0;

            if (double.IsNaN(state.catHair) || double.IsInfinity(state.catHair) || state.catHair < 0
                || double.IsNaN(state.totalEarned) || double.IsInfinity(state.totalEarned) || state.totalEarned < 0)
                throw new InvalidOperationException("Game-state balances must be finite and non-negative before production.");

            double remainingBalance = double.MaxValue - state.catHair;
            double remainingTotal = double.MaxValue - state.totalEarned;
            double maxIncrease = Math.Min(remainingBalance, remainingTotal);
            if (maxIncrease <= 0) return 0;

            double requested = ratePerSecond * elapsedSeconds;
            if (double.IsNaN(requested)) throw new OverflowException("Passive production could not be represented.");
            double earned = double.IsInfinity(requested) ? maxIncrease : Math.Min(requested, maxIncrease);
            double newBalance = state.catHair + earned;
            double newTotal = state.totalEarned + earned;
            if (double.IsNaN(newBalance) || double.IsInfinity(newBalance)
                || double.IsNaN(newTotal) || double.IsInfinity(newTotal))
                throw new OverflowException("Passive production exceeded the finite game-state range.");

            // Spending makes the balances differ, so normal floating-point rounding
            // can produce slightly different deltas. Never compare them for equality.
            // Only reject a tick if either balance cannot advance at all.
            double balanceDelta = newBalance - state.catHair;
            double totalDelta = newTotal - state.totalEarned;
            if (balanceDelta <= 0 || totalDelta <= 0) return 0;

            state.catHair = newBalance;
            state.totalEarned = newTotal;
            return earned;
        }
        public static double ApplyClick(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            double earned = ApplyProduction(state, ClickValue(state), 1.0);
            if (state.clicks < int.MaxValue) state.clicks++;
            return earned;
        }
        public static double ClickValue(GameState s) { int levels=0; foreach(var i in ItemCatalog.All) if(i.click) levels+=Level(s,i.id); return IdleBalance.ClickBaseValue*(1+IdleBalance.ClickLevelBonus*levels); }
        public static double CatPrice(string id,int owned) { var c=FindCat(id); return c==null||owned<0||owned>=c.maxOwned?double.PositiveInfinity:c.firstPrice*Math.Pow(c.priceGrowth,owned); }
        public static double ItemPrice(string id,int level) { var i=FindItem(id); return i==null||level<0||level>=ItemCatalog.LevelsPerItem?double.PositiveInfinity:i.firstPrice*Math.Pow(ItemCatalog.PriceGrowthPerLevel,level); }
        public static int Stage(GameState s) => s.totalEarned>=IdleBalance.Stage5Threshold?5:s.totalEarned>=IdleBalance.Stage4Threshold?4:s.totalEarned>=IdleBalance.Stage3Threshold?3:s.totalEarned>=IdleBalance.Stage2Threshold?2:1;
        public static double OfflineRatePerSecond(GameState s) => BasePerSecond(s) * ItemMultiplier(s) * IdleBalance.OfflineFactor;
        public static double OfflineEarnings(GameState s,double seconds) => OfflineRatePerSecond(s)*Math.Max(0,Math.Min(seconds,IdleBalance.OfflineCapSeconds));
        static int Owned(GameState s,string id) { foreach(var c in s.cats) if(c.id==id)return c.count; return 0; }
        static int Level(GameState s,string id) { foreach(var i in s.items) if(i.id==id)return i.level; return 0; }
        static CatDefinition FindCat(string id) { foreach(var c in CatCatalog.All) if(c.id==id)return c; return null; }
        static ItemDefinition FindItem(string id) { foreach(var i in ItemCatalog.All) if(i.id==id)return i; return null; }
    }

    public sealed class CurveResult
    {
        public double totalSeconds;
        public double[] stageSeconds = new double[IdleBalance.StageCount];
        public double[] stageEndRates = new double[IdleBalance.StageCount];
        public int purchasedLevels, totalLevels;
        public bool reachedGoal;
        public int iterations;
        public double completion => totalLevels == 0 ? 0 : (double)purchasedLevels / totalLevels;
        public double completionSeconds;
        public double secondCatSeconds = double.PositiveInfinity;
        public double firstFourBreedsSeconds = double.PositiveInfinity;
        public double firstItemSeconds = double.PositiveInfinity;
        public double nextCatBatchSeconds = double.PositiveInfinity;
        public readonly List<string> purchases = new List<string>();
    }

    /// <summary>Deterministic economy simulation; time advances only to the next affordable purchase.</summary>
    public static class CurveSimulator
    {
        const int MaxIterations = 2000000;

        public static CurveResult Run(double clickRate = IdleBalance.CurveClickRate)
        {
            if (double.IsNaN(clickRate) || double.IsInfinity(clickRate) || clickRate < 0)
                throw new ArgumentOutOfRangeException(nameof(clickRate), "Click rate must be finite and non-negative.");

            var state = new GameState();
            foreach (var c in CatCatalog.All) state.cats.Add(new CatCount { id = c.id, count = c.id == "Milk" ? 1 : 0 });
            foreach (var i in ItemCatalog.All) state.items.Add(new ItemLevel { id = i.id, level = 0 });
            var result = new CurveResult { totalLevels = ItemCatalog.All.Length * ItemCatalog.LevelsPerItem };
            double[] threshold = { IdleBalance.Stage2Threshold, IdleBalance.Stage3Threshold, IdleBalance.Stage4Threshold, IdleBalance.Stage5Threshold, IdleBalance.WinThreshold };
            int currentStage = 1;
            while (state.totalEarned < IdleBalance.WinThreshold && result.iterations < MaxIterations)
            {
                result.iterations++;
                var candidates = new List<Tuple<double, Action>>();
                foreach (var c in CatCatalog.All)
                {
                    int owned = 0; foreach (var x in state.cats) if (x.id == c.id) owned = x.count;
                    var price = IdleMath.CatPrice(c.id, owned);
                    if (owned < c.maxOwned && price <= state.catHair)
                        candidates.Add(Tuple.Create(price / (c.income * IdleMath.ItemMultiplier(state)), (Action)(() => { state.catHair -= price; foreach (var x in state.cats) if (x.id == c.id) x.count++; })));
                }
                foreach (var item in ItemCatalog.All)
                {

                    int level = 0; foreach (var x in state.items) if (x.id == item.id) level = x.level;
                    var price = IdleMath.ItemPrice(item.id, level);
                    if (level < ItemCatalog.LevelsPerItem && price <= state.catHair)
                    {
                        int nextLevelEffectExponent = Math.Max(0, level - 1);
                        double nextEffect = item.click ? clickRate * IdleBalance.ClickBaseValue * Math.Pow(IdleBalance.ClickStageGrowth, IdleMath.Stage(state)-1) * IdleBalance.ClickLevelBonus : IdleMath.BasePerSecond(state) * item.effect * Math.Pow(2,nextLevelEffectExponent);
                        candidates.Add(Tuple.Create(price / Math.Max(1e-9, nextEffect), (Action)(() => { state.catHair -= price; foreach (var x in state.items) if (x.id == item.id) x.level++; result.purchasedLevels++; if(result.purchasedLevels==result.totalLevels)result.completionSeconds=result.totalSeconds; })));
                    }
                }
                if (candidates.Count > 0)
                {
                    candidates.Sort((a,b) => a.Item1.CompareTo(b.Item1));
                    var beforeCats = state.cats.ConvertAll(c => c.count);
                    var beforeItems = state.items.ConvertAll(i => i.level);
                    candidates[0].Item2();
                    int totalCats = 0;
                    for (int k = 0; k < state.cats.Count; k++)
                    {
                        totalCats += state.cats[k].count;
                        if (state.cats[k].count != beforeCats[k])
                            result.purchases.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0:F3}s cat {1} count={2}", result.totalSeconds, state.cats[k].id, state.cats[k].count));
                    }
                    for (int k = 0; k < state.items.Count; k++)
                        if (state.items[k].level != beforeItems[k])
                            result.purchases.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                "{0:F3}s item {1} level={2}", result.totalSeconds, state.items[k].id, state.items[k].level));
                    if (totalCats > 1 && double.IsPositiveInfinity(result.secondCatSeconds)) result.secondCatSeconds = result.totalSeconds;
                    if (state.cats[0].count > 0 && state.cats[1].count > 0 && state.cats[2].count > 0 && state.cats[3].count > 0
                        && double.IsPositiveInfinity(result.firstFourBreedsSeconds)) result.firstFourBreedsSeconds = result.totalSeconds;
                    if (result.purchasedLevels > 0 && double.IsPositiveInfinity(result.firstItemSeconds)) result.firstItemSeconds = result.totalSeconds;
                    if (state.cats[4].count > 0 && double.IsPositiveInfinity(result.nextCatBatchSeconds)) result.nextCatBatchSeconds = result.totalSeconds;
                    continue;
                }
                double rate = IdleMath.BasePerSecond(state) * IdleMath.ItemMultiplier(state) + clickRate * IdleMath.ClickValue(state);
                if (rate <= 0) break;
                double nextPurchase = double.PositiveInfinity;
                foreach (var c in CatCatalog.All) { int owned=0; foreach(var x in state.cats)if(x.id==c.id)owned=x.count; if(owned<c.maxOwned)nextPurchase=Math.Min(nextPurchase,IdleMath.CatPrice(c.id,owned)); }
                foreach (var i in ItemCatalog.All) { int l=0;foreach(var x in state.items)if(x.id==i.id)l=x.level;if(l<ItemCatalog.LevelsPerItem)nextPurchase=Math.Min(nextPurchase,IdleMath.ItemPrice(i.id,l));}
                double toStage = threshold[currentStage-1] - state.totalEarned;
                double toPurchase = nextPurchase - state.catHair;
                double earned = Math.Min(toStage, Math.Max(0, toPurchase));
                if (double.IsPositiveInfinity(toPurchase) || toPurchase <= 0) earned = toStage;
                if (earned <= 0) { state.totalEarned = threshold[currentStage-1]; continue; }
                double seconds = earned / rate;
                result.stageSeconds[currentStage-1] += seconds;
                result.totalSeconds += seconds; state.catHair += earned; state.totalEarned += earned;
                if (state.totalEarned + 1e-7 >= threshold[currentStage-1])
                {
                    state.totalEarned = threshold[currentStage-1];
                    result.stageEndRates[currentStage-1] = rate;
                    currentStage++;
                }
            }
            result.reachedGoal = state.totalEarned >= IdleBalance.WinThreshold;
            if (result.purchasedLevels < result.totalLevels) result.completionSeconds = double.PositiveInfinity;
            return result;
        }
    }
}
