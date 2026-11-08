namespace CatGame.Idle
{
    /// <summary>Pure creation of a fresh player state, shared by runtime and tests.</summary>
    public static class GameStateFactory
    {
        public static GameState CreateNewGame()
        {
            var state = new GameState { version = GameState.CurrentVersion };
            foreach (var cat in CatCatalog.All)
                state.cats.Add(new CatCount { id = cat.id, count = cat.id == "Milk" ? 1 : 0 });
            foreach (var item in ItemCatalog.All)
                state.items.Add(new ItemLevel { id = item.id, level = 0 });
            return state;
        }
    }
}
