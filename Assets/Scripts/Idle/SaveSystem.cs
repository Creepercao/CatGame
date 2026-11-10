using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGame.Idle
{
    /// <summary>Local JSON save owner. Game systems call MarkDirty after changing persisted state.</summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        public const int CurrentVersion = GameState.CurrentVersion;
        public const float AutosaveIntervalSeconds = 10f;

        public static SaveSystem Instance { get; private set; }

        public GameState State { get; private set; }
        public OfflineEarningsResult OfflineEarnings { get; private set; }
        public string SavePath
        {
            get
            {
                return GetSavePath(Application.persistentDataPath);
            }
        }
        public bool HasExistingSave { get; private set; }

        bool _dirty;
        bool _resetBackup;
        float _lastSaveTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (FindObjectOfType<SaveSystem>() != null) return;
            new GameObject("SaveSystem").AddComponent<SaveSystem>();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            State = LoadOrCreate(SavePath, out bool loadedSave);
            HasExistingSave = loadedSave;
            OfflineEarnings = global::CatGame.Idle.OfflineEarnings.Apply(State, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            // Merely opening the menu must not create a resumable game.
            // BeginNewGame or gameplay changes mark a fresh state dirty explicitly.
            _dirty = loadedSave;
            _lastSaveTime = Time.unscaledTime;
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Instance = null;
        }

        void Update()
        {
            if (_dirty && Time.unscaledTime - _lastSaveTime >= AutosaveIntervalSeconds)
                SaveNow();
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void BeginNewGame()
        {
            State = CreateNewGame();
            OfflineEarnings = null;
            _resetBackup = true;
            _dirty = true;
            SaveNow();
        }

        /// <summary>Call immediately after an achievement is unlocked.</summary>
        public void OnAchievementUnlocked()
        {
            _dirty = true;
            SaveNow();
        }

        public void SaveNow()
        {
            if (State == null) return;
            try
            {
                State.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var path = SavePath;
                WriteSave(path, State, keepPrevious: HasExistingSave && !_resetBackup);
                HasExistingSave = true;
                // A deliberately reset game must not recover an older playthrough.
                if (_resetBackup) File.Delete(path + ".bak");
                _resetBackup = false;
                _dirty = false;
                _lastSaveTime = Time.unscaledTime;
            }
            catch (Exception exception)
            {
                _dirty = true;
                Debug.LogError("Could not save CatGame progress; it will be retried. " + exception.Message);
                _lastSaveTime = Time.unscaledTime;
            }
        }

        void OnActiveSceneChanged(Scene previous, Scene next)
        {
            if (_dirty || HasExistingSave) SaveNow();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && (_dirty || HasExistingSave)) SaveNow();
        }

        void OnApplicationQuit()
        {
            if (_dirty || HasExistingSave) SaveNow();
        }

        public static string GetSavePath(string persistentDataPath)
        {
            if (string.IsNullOrEmpty(persistentDataPath))
                throw new ArgumentException("Persistent data path must not be empty.", nameof(persistentDataPath));
            return Path.Combine(persistentDataPath, "CatGame", "save.json");
        }

        public static GameState LoadOrCreate(string path)
        {
            return LoadOrCreate(path, out _);
        }

        static GameState LoadOrCreate(string path, out bool loadedSave)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Save path must not be empty.", nameof(path));
            loadedSave = false;
            if (!File.Exists(path))
            {
                loadedSave = TryRestoreBackup(path, out var restored);
                return loadedSave ? restored : CreateNewGame();
            }

            string json;
            try
            {
                json = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Save data could not be read; starting a new game without moving the file. " + exception.Message);
                return CreateNewGame();
            }

            try
            {
                var state = ParseSave(json);
                loadedSave = true;
                return state;
            }
            catch (Exception exception)
            {
                if (Quarantine(path) && TryRestoreBackup(path, out var restored))
                {
                    loadedSave = true;
                    return restored;
                }
                Debug.LogWarning("Save data could not be loaded; starting a new game. " + exception.Message);
                return CreateNewGame();
            }
        }

        static bool TryRestoreBackup(string path, out GameState state)
        {
            state = null;
            var backupPath = path + ".bak";
            if (!File.Exists(backupPath)) return false;

            try
            {
                var restored = ParseSave(File.ReadAllText(backupPath, Encoding.UTF8));
                WriteSave(path, restored, keepPrevious: false);
                state = restored;
                Debug.LogWarning("Recovered CatGame progress from backup.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Save backup could not be restored. " + exception.Message);
                return false;
            }
        }

        static GameState ParseSave(string json)
        {
            RequireVersionOneFields(json);
            var state = JsonUtility.FromJson<GameState>(json);
            if (state == null) throw new InvalidDataException("JSON did not contain a GameState object.");
            if (state.version >= 2)
                foreach (var field in new[]{"introCompleted","seenStoryEvents","pendingStoryEvents","playSeconds","hasFullPlaytimeHistory","rocketPurchased","endingCompleted","hasEndingSnapshot","firstEnding"})
                    if (!Regex.IsMatch(json, "\\\"" + field + "\\\"\\s*:")) throw new InvalidDataException("Story save field missing: " + field);
            Upgrade(state);
            Validate(state);
            return state;
        }

        public static void SaveToPath(string path, GameState state)
        {
            WriteSave(path, state, keepPrevious: true);
        }

        static void WriteSave(string path, GameState state, bool keepPrevious)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException("Save path must not be empty.", nameof(path));
            Validate(state);

            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Save path must include a directory.", nameof(path));
            Directory.CreateDirectory(directory);

            var temporaryPath = path + ".tmp";
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(state, true), new UTF8Encoding(false));

            if (File.Exists(path))
                File.Replace(temporaryPath, path, keepPrevious ? path + ".bak" : null);
            else
                File.Move(temporaryPath, path);
        }

        public static GameState CreateNewGame()
        {
            return GameStateFactory.CreateNewGame();
        }

        static void RequireVersionOneFields(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new InvalidDataException("Save file is empty.");
            string[] required = { "version", "catHair", "totalEarned", "clicks", "cats", "items", "achievements", "lastSeenUnix", "prestigeLevel", "prestigeMultiplier" };
            foreach (var field in required)
                if (!Regex.IsMatch(json, "\\\"" + Regex.Escape(field) + "\\\"\\s*:"))
                    throw new InvalidDataException("Save file is missing required field '" + field + "'.");
        }

        static void Upgrade(GameState state)
        {
            if (state.version > CurrentVersion)
                throw new InvalidDataException("Save version " + state.version + " is newer than supported version " + CurrentVersion + ".");

            // Keep version-to-version migration steps here as the save schema evolves.
            while (state.version < CurrentVersion)
            {
                switch (state.version)
                {
                    case 1:
                        state.introCompleted = true;
                        state.seenStoryEvents = new List<string>();
                        state.pendingStoryEvents = new List<string>();
                        foreach (var id in new[]{"three_breeds","six_breeds","all_breeds","planet_cats"})
                            if (state.achievements != null && state.achievements.Contains(id)) state.seenStoryEvents.Add(id);
                        state.playSeconds = 0;
                        state.hasFullPlaytimeHistory = false;
                        state.firstEnding = new EndingStatistics();
                        state.rocketPurchased = state.endingCompleted = state.hasEndingSnapshot = false;
                        state.version = 2;
                        break;
                    default:
                        throw new InvalidDataException("No migration exists from save version " + state.version + ".");
                }
            }
            // The B4 delivery already wrote the v2 schema before CG playback existed.
            // Its achieved milestones have neither marker. Current gameplay persists
            // achievement + pending together, so genuine interrupted memories retain
            // pending and must never be consumed by this compatibility normalization.
            if(state.achievements!=null && state.seenStoryEvents!=null && state.pendingStoryEvents!=null)
                foreach(var story in StoryCatalog.All)
                    if(state.achievements.Contains(story.id) && !state.seenStoryEvents.Contains(story.id) && !state.pendingStoryEvents.Contains(story.id))
                        state.seenStoryEvents.Add(story.id);
        }

        static void Validate(GameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.version != CurrentVersion) throw new InvalidDataException("Unsupported save version " + state.version + ".");
            if (!IsFiniteNonNegative(state.catHair) || !IsFiniteNonNegative(state.totalEarned))
                throw new InvalidDataException("Save currencies must be finite and non-negative.");
            if (state.clicks < 0 || state.lastSeenUnix < 0 || state.prestigeLevel < 0 || !IsFiniteNonNegative(state.prestigeMultiplier))
                throw new InvalidDataException("Save counters are outside their valid range.");
            if (state.cats == null || state.items == null || state.achievements == null)
                throw new InvalidDataException("Save collections must be present.");
            if (!IsFiniteNonNegative(state.playSeconds) || state.seenStoryEvents == null || state.pendingStoryEvents == null || state.firstEnding == null)
                throw new InvalidDataException("Story state must contain valid progress and playtime.");
            var events = new HashSet<string>();
            foreach (var id in state.seenStoryEvents)
                if (string.IsNullOrEmpty(id) || !events.Add(id)) throw new InvalidDataException("Invalid or duplicate seen story event.");
            foreach (var id in state.pendingStoryEvents)
                if (string.IsNullOrEmpty(id) || !events.Add(id)) throw new InvalidDataException("Story event repeated or both pending and seen.");
            if ((state.endingCompleted || state.hasEndingSnapshot) && !state.rocketPurchased)
                throw new InvalidDataException("Ending state requires the rocket purchase.");
            if (state.rocketPurchased && !state.hasEndingSnapshot)
                throw new InvalidDataException("Rocket purchase requires its first-ending snapshot.");
            var ending=state.firstEnding;
            if (!IsFiniteNonNegative(ending.playSeconds) || !IsFiniteNonNegative(ending.totalEarned)
                || ending.clicks<0 || ending.totalCats<0 || ending.totalCats>CatCatalog.All.Length*IdleBalance.CatMaxOwned
                || ending.breeds<0 || ending.breeds>CatCatalog.All.Length || ending.itemLevels<0
                || ending.itemLevels>ItemCatalog.All.Length*ItemCatalog.LevelsPerItem || ending.achievementCount<0
                || ending.achievementCount>AchievementCatalog.All.Length || ending.completedUnix<0)
                throw new InvalidDataException("First-ending statistics are outside their valid range.");

            ValidateCats(state.cats);
            ValidateItems(state.items);
            var achievements = new HashSet<string>();
            foreach (var id in state.achievements)
                if (string.IsNullOrEmpty(id) || !achievements.Add(id))
                    throw new InvalidDataException("Achievement ids must be non-empty and unique.");
        }

        static void ValidateCats(List<CatCount> counts)
        {
            var seen = new HashSet<string>();
            foreach (var entry in counts)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || !seen.Add(entry.id) || entry.count < 0 || entry.count > IdleBalance.CatMaxOwned)
                    throw new InvalidDataException("Cat counts contain a duplicate, invalid id, or out-of-range value.");
                if (FindCat(entry.id) == null) throw new InvalidDataException("Unknown cat id '" + entry.id + "'.");
            }
            if (seen.Count != CatCatalog.All.Length) throw new InvalidDataException("Save must contain one entry for every cat type.");
        }

        static void ValidateItems(List<ItemLevel> levels)
        {
            var seen = new HashSet<string>();
            foreach (var entry in levels)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id) || !seen.Add(entry.id) || entry.level < 0 || entry.level > ItemCatalog.LevelsPerItem)
                    throw new InvalidDataException("Item levels contain a duplicate, invalid id, or out-of-range value.");
                if (FindItem(entry.id) == null) throw new InvalidDataException("Unknown item id '" + entry.id + "'.");
            }
            if (seen.Count != ItemCatalog.All.Length) throw new InvalidDataException("Save must contain one entry for every item type.");
        }

        static CatDefinition FindCat(string id)
        {
            foreach (var cat in CatCatalog.All) if (cat.id == id) return cat;
            return null;
        }

        static ItemDefinition FindItem(string id)
        {
            foreach (var item in ItemCatalog.All) if (item.id == id) return item;
            return null;
        }

        static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
        }

        static bool Quarantine(string path)
        {
            try
            {
                var directory = Path.GetDirectoryName(path);
                var badPath = Path.Combine(directory, "save.bad.json");
                if (File.Exists(badPath))
                {
                    var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff");
                    File.Move(badPath, Path.Combine(directory, "save.bad." + timestamp + ".json"));
                }
                File.Move(path, badPath);
                return true;
            }
            catch (Exception quarantineException)
            {
                Debug.LogWarning("Could not quarantine invalid save file: " + quarantineException.Message);
                return false;
            }
        }
    }
}
