using System.Collections.Generic;
using CatGame.Idle;
using UnityEngine;
using System.Collections;

namespace CatGame
{
    /// <summary>Owns rendered cat actors while keeping the full owned count in GameState.</summary>
    public sealed class CatRoster : MonoBehaviour
    {
        public const int MaxVisibleCats = IdleBalance.MaxVisibleCats;

        sealed class ActorRecord
        {
            public CatActor actor;
            public string breedId;
            public long sequence;
        }

        readonly List<ActorRecord> _records = new List<ActorRecord>();
        readonly Dictionary<string, int> _lastCounts = new Dictionary<string, int>();
        readonly Dictionary<string, int> _ownedCounts = new Dictionary<string, int>();
        GameState _state;
        Vector3 _floorCenter;
        Vector2 _floorHalf;
        long _sequence;
        bool _initialized;
        RoomWalkSurface _walkSurface;

        public int TotalOwned { get; private set; }
        public int StackedCount { get; private set; }
        public int VisibleCount { get { return _records.Count; } }
        public GameState State { get { return _state; } }

        public void Initialize(GameState state, Vector3 floorCenter, Vector2 floorHalf, string newestBreedId = null, RoomWalkSurface walkSurface = null)
        {
            _walkSurface = walkSurface;
            _floorCenter = floorCenter;
            _floorHalf = floorHalf;
            Rebuild(state, newestBreedId);
        }

        /// <summary>Reconcile rendered actors with state; optional newest id gives new purchases priority.</summary>
        public void Rebuild(GameState state, string newestBreedId = null)
        {
            if (state == null) state = SaveSystem.CreateNewGame();
            _state = state;
            ReadOwnedCounts(state);

            if (!_initialized)
                BuildInitialActors(newestBreedId);
            else
                ApplyCountChanges(newestBreedId);

            SelectVisibleActors();
            UpdateCounts();
            _initialized = true;
        }

        public CatActor FindActor(string breedId)
        {
            foreach (var record in _records)
                if (record.breedId == breedId) return record.actor;
            return null;
        }

        public CatActor GetFirstActor()
        {
            return _records.Count == 0 ? null : _records[0].actor;
        }

        public int VisibleCountFor(string breedId)
        {
            int count = 0;
            foreach (var record in _records) if (record.breedId == breedId) count++;
            return count;
        }

        public int StackedCountFor(string breedId)
        {
            return Mathf.Max(0, OwnedCountFor(breedId) - VisibleCountFor(breedId));
        }

        public int OwnedCountFor(string breedId)
        {
            int count;
            return _ownedCounts.TryGetValue(breedId, out count) ? count : 0;
        }

        void ReadOwnedCounts(GameState state)
        {
            _ownedCounts.Clear();
            foreach (var definition in CatCatalog.All) _ownedCounts[definition.id] = 0;
            if (state.cats != null)
            {
                foreach (var entry in state.cats)
                {
                    if (entry == null || !_ownedCounts.ContainsKey(entry.id)) continue;
                    _ownedCounts[entry.id] = Mathf.Clamp(entry.count, 0, IdleBalance.CatMaxOwned);
                }
            }
        }

        void BuildInitialActors(string newestBreedId)
        {
            var order = BuildBreedOrder(newestBreedId);
            foreach (var id in order)
                if (_ownedCounts[id] > 0) AddActor(id);

            bool added;
            do
            {
                added = false;
                foreach (var id in order)
                {
                    if (_records.Count >= MaxVisibleCats) return;
                    if (CountRecords(id) >= _ownedCounts[id]) continue;
                    AddActor(id);
                    added = true;
                }
            } while (added && _records.Count < MaxVisibleCats);
        }

        void ApplyCountChanges(string newestBreedId)
        {
            // If ownership decreases (e.g. a repaired save), remove excess actors before adding new ones.
            foreach (var definition in CatCatalog.All)
            {
                int owned = _ownedCounts[definition.id];
                while (CountRecords(definition.id) > owned)
                {
                    var remove = FindOldestExtra(definition.id);
                    if (remove == null) break;
                    RemoveActor(remove);
                }
            }

            var order = BuildBreedOrder(newestBreedId);
            foreach (var id in order)
            {
                int previous;
                _lastCounts.TryGetValue(id, out previous);
                int increase = Mathf.Max(0, _ownedCounts[id] - previous);
                // Bound temporary candidates; selection below keeps one per owned type and newest extras.
                increase = Mathf.Min(increase, MaxVisibleCats);
                for (int i = 0; i < increase; i++) AddActor(id, id == newestBreedId);
            }
        }

        void SelectVisibleActors()
        {
            var selected = new HashSet<ActorRecord>();
            var order = BuildBreedOrder(null);

            // Reserve one visible actor for every owned cat type before filling the remaining slots.
            foreach (var id in order)
            {
                if (_ownedCounts[id] <= 0) continue;
                ActorRecord best = null;
                foreach (var record in _records)
                    if (record.breedId == id && (best == null || record.sequence < best.sequence)) best = record;
                if (best == null) best = AddActor(id);
                selected.Add(best);
            }

            var extras = new List<ActorRecord>();
            foreach (var record in _records)
                if (!selected.Contains(record) && CountSelected(selected, record.breedId) < _ownedCounts[record.breedId]) extras.Add(record);
            extras.Sort((a, b) => b.sequence.CompareTo(a.sequence));

            int remaining = Mathf.Max(0, MaxVisibleCats - selected.Count);
            for (int i = 0; i < extras.Count && i < remaining; i++) selected.Add(extras[i]);

            var removed = new List<ActorRecord>();
            foreach (var record in _records) if (!selected.Contains(record)) removed.Add(record);
            foreach (var record in removed) RemoveActor(record);
        }

        ActorRecord AddActor(string breedId, bool purchased = false)
        {
            var definition = FindDefinition(breedId);
            if (definition == null) return null;
            var go = new GameObject("Cat_" + breedId);
            go.transform.SetParent(transform, false);
            var actor = go.AddComponent<CatActor>();
            actor.Setup(CatVisualLibrary.Load(definition), _floorCenter, _floorHalf);
            actor.SetWalkSurface(_walkSurface);
            if (purchased) actor.PlayOneShot(ActionNames.Jump);
            actor.Licked += OnCatLicked;
            var record = new ActorRecord { actor = actor, breedId = breedId, sequence = ++_sequence };
            _records.Add(record);
            return record;
        }

        void RemoveActor(ActorRecord record)
        {
            _records.Remove(record);
            if (record.actor != null)
            {
                record.actor.Licked -= OnCatLicked;
                if (Application.isPlaying) Destroy(record.actor.gameObject);
                else DestroyImmediate(record.actor.gameObject);
            }
        }

        void OnCatLicked(CatActor actor)
        {
            if (_state == null) return;
            double clickValue = IdleMath.ApplyClick(_state);
            if (SaveSystem.Instance != null && SaveSystem.Instance.State == _state)
                SaveSystem.Instance.MarkDirty();
            ShowReward(actor, clickValue);
        }

        void ShowReward(CatActor actor, double value)
        {
            if (actor == null) return;
            var go = new GameObject("ClickReward");
            go.transform.position = actor.Position + new Vector3(0f, 0.55f, 0f);
            var label = go.AddComponent<TextMesh>();
            label.text = "+" + FormatReward(value);
            label.fontSize = 48;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(0.28f, 0.18f, 0.12f, 1f);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sortingOrder = actor.Renderer != null ? actor.Renderer.sortingOrder + 2 : 500;
            StartCoroutine(AnimateReward(go.transform, label));
        }

        static string FormatReward(double value)
        {
            return Hud.FormatNumber(value);
        }

        static IEnumerator AnimateReward(Transform target, TextMesh label)
        {
            const float duration = 0.8f;
            float elapsed = 0f;
            var start = target.position;
            var color = label.color;
            while (elapsed < duration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                target.position = start + Vector3.up * (0.65f * t);
                color.a = 1f - t;
                label.color = color;
                yield return null;
            }
            if (target != null) Destroy(target.gameObject);
        }

        ActorRecord FindOldestExtra(string breedId)
        {
            int keep = _ownedCounts[breedId] > 0 ? 1 : 0;
            if (CountRecords(breedId) <= keep) return null;
            ActorRecord oldest = null;
            foreach (var record in _records)
            {
                if (record.breedId != breedId) continue;
                if (oldest == null || record.sequence < oldest.sequence) oldest = record;
            }
            return oldest;
        }

        void UpdateCounts()
        {
            TotalOwned = 0;
            foreach (var definition in CatCatalog.All)
            {
                int count = _ownedCounts[definition.id];
                TotalOwned += count;
                _lastCounts[definition.id] = count;
            }
            StackedCount = Mathf.Max(0, TotalOwned - _records.Count);
        }

        int CountRecords(string breedId)
        {
            int count = 0;
            foreach (var record in _records) if (record.breedId == breedId) count++;
            return count;
        }

        static int CountSelected(HashSet<ActorRecord> selected, string breedId)
        {
            int count = 0;
            foreach (var record in selected) if (record.breedId == breedId) count++;
            return count;
        }

        static CatDefinition FindDefinition(string id)
        {
            foreach (var definition in CatCatalog.All) if (definition.id == id) return definition;
            return null;
        }

        static List<string> BuildBreedOrder(string newestBreedId)
        {
            var result = new List<string>(CatCatalog.All.Length);
            if (!string.IsNullOrEmpty(newestBreedId) && FindDefinition(newestBreedId) != null)
                result.Add(newestBreedId);
            foreach (var definition in CatCatalog.All)
                if (definition.id != newestBreedId) result.Add(definition.id);
            return result;
        }
    }
}
