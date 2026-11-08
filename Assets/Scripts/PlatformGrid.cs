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

        readonly Dictionary<string, ItemView> _itemViews = new Dictionary<string, ItemView>();
        IdleGameController _economy;
        Tilemap _floorTilemap;
        Tile _floorTile;

        public int CellCount { get; private set; }
        public int FloorRendererCount { get { return _floorTilemap != null ? 1 : 0; } }
        public int ItemMarkerCount
        {
            get { int n = 0; foreach (var view in _itemViews.Values) if (view.root.activeSelf) n++; return n; }
        }

        public void Initialize(Vector2 center, Vector2 halfSize, IdleGameController economy)
        {
            _economy = economy;
            float cellWidth = halfSize.x * 2f / Columns;
            float cellHeight = halfSize.y * 2f / Rows;
            var cellSprite = UiKit.RoundSoft;
            var gridObject = new GameObject("PlatformGrid", typeof(Grid));
            gridObject.transform.SetParent(transform, false);
            gridObject.transform.position = new Vector3(center.x - halfSize.x, center.y - halfSize.y, 0f);
            var grid = gridObject.GetComponent<Grid>();
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
                _itemViews.Add(ItemCatalog.All[i].id, CreateItemView(ItemCatalog.All[i], i, center, halfSize));

            if (_economy != null) _economy.StateChanged += Refresh;
            Refresh();
        }

        ItemView CreateItemView(ItemDefinition definition, int index, Vector2 center, Vector2 halfSize)
        {
            var root = new GameObject("PlatformItem_" + definition.id);
            root.transform.SetParent(transform, false);
            root.SetActive(false);

            int col = index % Columns;
            int row = index / Columns;
            float cellWidth = halfSize.x * 2f / Columns;
            float cellHeight = halfSize.y * 2f / Rows;
            root.transform.position = new Vector3(center.x - halfSize.x + cellWidth * (col + 0.5f),
                center.y - halfSize.y + cellHeight * (row + 0.5f), -0.1f);

            var icon = new GameObject("Icon");
            icon.transform.SetParent(root.transform, false);
            var badge = icon.AddComponent<SpriteRenderer>();
            badge.sprite = ItemVisualLibrary.Load(definition.id);
            badge.color = Color.white;
            badge.sortingOrder = 20;
            var profile = WorldItemArt.Get(definition.id);
            float scale = WorldItemArt.Scale(definition.id, badge.sprite.pixelsPerUnit);
            icon.transform.localScale = new Vector3(scale, scale, 1f);
            icon.transform.localPosition = new Vector3((32 - profile.centerX) / badge.sprite.pixelsPerUnit * scale,
                (32 - profile.bottom) / badge.sprite.pixelsPerUnit * scale, 0);
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
            if (_economy != null) _economy.StateChanged -= Refresh;
            if (_floorTilemap != null) _floorTilemap.ClearAllTiles();
            if (_floorTile != null) Destroy(_floorTile);
        }
    }
}
