using System;

namespace CatGame.Idle
{
    public sealed class OfflineEarningsResult
    {
        public double elapsedSeconds;
        public double catHair;
    }

    /// <summary>Applies capped automatic production accumulated while the game was closed.</summary>
    public static class OfflineEarnings
    {
        public static OfflineEarningsResult Apply(GameState state, long nowUnix)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var result = new OfflineEarningsResult();
            long previous = state.lastSeenUnix;
            if (previous > 0 && nowUnix > previous)
            {
                result.elapsedSeconds = Math.Min((double)nowUnix - previous, IdleBalance.OfflineCapSeconds);
                result.catHair = IdleMath.ApplyProduction(
                    state, IdleMath.OfflineRatePerSecond(state), result.elapsedSeconds);
            }

            // A clock correction must not move the save timestamp backwards and create a second payout.
            state.lastSeenUnix = Math.Max(previous, nowUnix);
            return result;
        }
    }
}
