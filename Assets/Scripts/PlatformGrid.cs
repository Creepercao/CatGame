using System.Collections.Generic;
using CatGame.Idle;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CatGame
{
    /// <summary>Seven-by-six world platform. Purchased items occupy stable catalog-index slots.</summary>
    public sealed class PlatformGrid : MonoBehaviour
    {
        public const int Columns = 6;
        public const int Rows = 7;
        public const int Capacity = Columns * Rows;

        sealed class ItemView
        {
            public GameObject root;
            public SpriteRenderer badge;
            public TextMesh label;
        }

        internal sealed class FurnitureSpot
        {
            readonly Transform _root;
            readonly Vector2 _frontOffset, _useOffset;

            internal FurnitureSpot(Transform root, string action, Vector2 frontOffset, Vector2 useOffset, float facing = 0f)
            {
                _root = root;
                Action = action;
                _frontOffset = frontOffset;
                _useOffset = useOffset;
                Facing = facing;
            }

            internal Vector3 Front => (Vector2)_root.position + _frontOffset;
            internal Vector3 UsePoint => (Vector2)_root.position + _useOffset;
            internal bool IsPlaced => _root != null && _root.gameObject.activeInHierarchy;
            internal string Action { get; }
            internal float Facing { get; }
            internal CatActor Occupant { get; set; }
        }

        readonly Dictionary<string, ItemView> _itemViews = new Dictionary<string, ItemView>();
        readonly List<FurnitureSpot> _furniture = new List<FurnitureSpot>();
        IdleGameController _economy;
        RoomWalkSurface _surface;
        Grid _grid;
        Tilemap _floorTilemap;
        Tile _floorTile;
        Vector2 _center, _halfSize;
        float _left, _widestItemHalfWidth;

        public int CellCount { get; private set; }
        public int FloorRendererCount { get { return _floorTilemap != null ? 1 : 0; } }
        public int ItemMarkerCount
        {
            get { int n = 0; foreach (var view in _itemViews.Values) if (view.root.activeSelf) n++; return n; }
        }

        public void Initialize(Vector2 center, Vector2 halfSize, IdleGameController economy, RoomWalkSurface surface = null)
        {
            _economy = economy;
            _surface = surface;
            if (_surface != null) _surface.Platform = this;
            _center = center;
            _halfSize = halfSize;
            _left = center.x - halfSize.x;
            float cellWidth = halfSize.x * 2f / Columns;
            float cellHeight = halfSize.y * 2f / Rows;
            var cellSprite = UiKit.RoundSoft;
            var gridObject = new GameObject("PlatformGrid", typeof(Grid));
            gridObject.transform.SetParent(transform, false);
            gridObject.transform.position = new Vector3(center.x - halfSize.x, center.y - halfSize.y, 0f);
            var grid = _grid = gridObject.GetComponent<Grid>();
            grid.cellSize = new Vector3(cellSprite.bounds.size.x / 0.94f, cellSprite.bounds.size.y / 0.88f, 1f);
            gridObject.transform.localScale = new Vector3(cellWidth / grid.cellSize.x, cellHeight / grid.cellSize.y, 1f);

            var tilemapObject = new GameObject("FloorTiles", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(gridObject.transform, false);
            _floorTilemap = tilemapObject.GetComponent<Tilemap>();
            _floorTilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            var tileRenderer = tilemapObject.GetComponent<TilemapRenderer>();
            tileRenderer.sortingOrder = -80;
            tileRenderer.enabled = false; // Slots remain logical; empty grid is invisible.
            _floorTile = ScriptableObject.CreateInstance<Tile>();
            _floorTile.name = "CatGamePlatformCell";
            _floorTile.sprite = cellSprite;
            _floorTile.color = new Color32(0xFF, 0xF8, 0xEB, 0x42);
            for (int row = 0; row < Rows; row++)
            {
                for (int col = 0; col < Columns; col++)
                {
                    _floorTilemap.SetTile(new Vector3Int(col, row, 0), _floorTile);
                    CellCount++;
                }
            }

            for (int i = 0; i < ItemCatalog.All.Length; i++)
                _itemViews.Add(ItemCatalog.All[i].id, CreateItemView(ItemCatalog.All[i], i, cellHeight));

            if (_economy != null) _economy.StateChanged += Refresh;
            Refresh();
        }

        Vector3 SlotPosition(int index)
        {
            float cellWidth = (_center.x + _halfSize.x - _left) / Columns;
            float cellHeight = _halfSize.y * 2f / Rows;
            return new Vector3(_left + cellWidth * (index % Columns + .5f),
                _center.y - _halfSize.y + cellHeight * (index / Columns + .5f), -.1f);
        }

        // Move the layout as a unit so purchases keep the same relative slot across window sizes.
        public bool KeepItemsVisible(float sidebarWorldRight)
        {
            const float margin = .125f;
            float firstCenter = sidebarWorldRight + margin + _widestItemHalfWidth;
            float right = _center.x + _halfSize.x;
            float fraction = .5f / Columns;
            float left = (firstCenter - right * fraction) / (1f - fraction);
            float density = WorldArtSettings.Current.WorldPixelsPerUnit;
            left = Mathf.Ceil(left * density) / density;
            left = Mathf.Clamp(left, _center.x - _halfSize.x, right - .25f * Columns);
            if (Mathf.Approximately(left, _left)) return false;

            _left = left;
            var location = _grid.transform.position;
            _grid.transform.position = new Vector3(left, location.y, location.z);
            var scale = _grid.transform.localScale;
            _grid.transform.localScale = new Vector3((right - left) / Columns / _grid.cellSize.x, scale.y, scale.z);
            for (int i = 0; i < ItemCatalog.All.Length; i++)
                _itemViews[ItemCatalog.All[i].id].root.transform.position = SlotPosition(i);
            return true;
        }

        ItemView CreateItemView(ItemDefinition definition, int index, float cellHeight)
        {
            var root = new GameObject("PlatformItem_" + definition.id);
            root.transform.SetParent(transform, false);
            root.SetActive(false);

            root.transform.position = SlotPosition(index);

            var icon = new GameObject("Icon");
            icon.transform.SetParent(root.transform, false);
            var badge = icon.AddComponent<SpriteRenderer>();
            badge.sprite = ItemVisualLibrary.Load(definition.id);
            badge.color = Color.white;
            badge.sortingOrder = 20;
            var profile = WorldItemArt.Get(definition.id);
            float ppu = badge.sprite.pixelsPerUnit;
            float scale = WorldItemArt.Scale(definition.id, ppu);
            float halfWidth = profile.width / ppu * scale * .5f;
            float height = profile.height / ppu * scale;
            _widestItemHalfWidth = Mathf.Max(_widestItemHalfWidth, halfWidth);
            icon.transform.localScale = new Vector3(scale, scale, 1f);
            icon.transform.localPosition = new Vector3((badge.sprite.pivot.x - profile.centerX) / ppu * scale,
                (badge.sprite.pivot.y - profile.bottom) / ppu * scale, 0);
            RegisterFurniture(root.transform, index, halfWidth, height);
            if (definition.id.EndsWith("_3"))
            {
                var depth = root.AddComponent<RoomFurnitureDepth>();
                depth.LocalGround = Vector3.zero;
                depth.Pieces = new[] { badge };
            }

            var labelObject = new GameObject("Level");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, -cellHeight * 0.24f, -0.01f);
            var label = labelObject.AddComponent<TextMesh>();
            label.font = UiKit.UIFont;
            label.fontSize = 24;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = UiKit.Ink;
            label.GetComponent<MeshRenderer>().sharedMaterial = label.font.material;
            label.GetComponent<MeshRenderer>().sortingOrder = 21;
            return new ItemView { root = root, badge = badge, label = label };
        }

        void RegisterFurniture(Transform root, int index, float halfWidth, float height)
        {
            float facing = index % Columns == 0 ? -1f : 1f;
            Vector2 point;
            switch (index % ItemCatalog.ItemsPerGeneration)
            {
                case 0: // Approach the first column from the room side of the sidebar.
                    point = new Vector2(-facing * (halfWidth + .14f), .21f);
                    _furniture.Add(new FurnitureSpot(root, ActionNames.Scratch, point + Vector2.down * .14f, point, facing));
                    break;
                case 1:
                    _furniture.Add(new FurnitureSpot(root, ActionNames.Sleep, Vector2.down * .12f, Vector2.up * (height * .57f)));
                    break;
                case 2: // Leave the upright feeder behind the bowl and lower the head from the side.
                    point = new Vector2(-facing * (halfWidth + .24f), -.1f);
                    _furniture.Add(new FurnitureSpot(root, ActionNames.Eat, point + Vector2.down * .14f, point, facing));
                    break;
            }
        }

        internal FurnitureSpot ReserveNearbyFurniture(CatActor actor, float maxDistance)
        {
            FurnitureSpot nearest = null;
            float nearestDistance = maxDistance * maxDistance;
            var foot = actor.Position + Vector3.up * actor.GroundOffset;
            foreach (var spot in _furniture)
            {
                if (!spot.IsPlaced || spot.Occupant != null) continue;
                float distance = (foot - spot.Front).sqrMagnitude;
                if (distance >= nearestDistance || !actor.CanVisitFurniture(spot)) continue;
                nearest = spot;
                nearestDistance = distance;
            }

            if (nearest != null) nearest.Occupant = actor;
            return nearest;
        }

        public void Refresh()
        {
            if (_economy == null || _economy.State == null) return;
            foreach (var definition in ItemCatalog.All)
            {
                ItemLevel level = null;
                foreach (var row in _economy.State.items) if (row.id == definition.id) { level = row; break; }
                ItemView view;
                if (!_itemViews.TryGetValue(definition.id, out view)) continue;
                int currentLevel = level != null ? level.level : 0;
                view.root.SetActive(currentLevel > 0);
                if (currentLevel > 0) view.label.text = "Lv." + currentLevel;
            }
        }

        void OnDestroy()
        {
            if (_surface != null && _surface.Platform == this) _surface.Platform = null;
            if (_economy != null) _economy.StateChanged -= Refresh;
            if (_floorTilemap != null) _floorTilemap.ClearAllTiles();
            if (_floorTile != null) Destroy(_floorTile);
        }
    }
}
