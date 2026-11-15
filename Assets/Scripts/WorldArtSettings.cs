using System;
using UnityEngine;

namespace CatGame
{
    /// <summary>Source texels, world units and screen pixels are separate quantities.</summary>
    [CreateAssetMenu(fileName = "WorldArtSettings", menuName = "CatGame/World art settings")]
    public sealed class WorldArtSettings : ScriptableObject
    {
        public const string ResourcePath = "Settings/WorldArtSettings";
        static WorldArtSettings _current;
        public static WorldArtSettings Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<WorldArtSettings>(ResourcePath);
                    if (_current == null)
                    {
                        _current = CreateInstance<WorldArtSettings>();
                        _current.hideFlags = HideFlags.HideAndDontSave;
                    }
                }
                return _current;
            }
        }

        [Header("World source grid (apply imports/scenes after editing)")]
        [Min(1)] public int WorldPixelsPerUnit = 32;
        [Min(1)] public int CatCanvasPixels = 64;
        [Min(0)] public float CatFootPixels = 6;
        public Vector2Int ReferencePixels = new Vector2Int(640, 360);
        [Tooltip("UI nine-slice borders retain their independent Canvas density.")]
        [Min(1)] public int UiPixelsPerUnit = 100;

        [Header("Home layout, measured on the reference world grid")]
        public Vector2 HomeFootCenterPixels = new Vector2(74, -34);
        public Vector2 HomeHalfPixels = new Vector2(217, 120);
        public Vector2 ItemGridOffsetPixels = new Vector2(0, 27);

        [Header("Opaque item extent on the world grid")]
        [Min(1)] public float BowlWidthPixels = 40;
        [Min(1)] public float BedWidthPixels = 61;
        [Min(1)] public float FeederHeightPixels = 37;
        [Min(1)] public float DecorationHeightPixels = 22;

        public float UnitsPerPixel { get { return 1f / WorldPixelsPerUnit; } }
        public float CatFootFraction { get { return CatFootPixels / CatCanvasPixels; } }
        public Vector2 HomeFootCenter { get { return HomeFootCenterPixels * UnitsPerPixel; } }
        public Vector2 HomeFloorCenter
        {
            get { return HomeFootCenter + Vector2.up * (CatCanvasPixels * .5f - CatFootPixels) * UnitsPerPixel; }
        }
        public Vector2 HomeHalf { get { return HomeHalfPixels * UnitsPerPixel; } }
        public Vector2 ItemGridOffset
        {
            get { return (ItemGridOffsetPixels - Vector2.up * (CatCanvasPixels * .5f - CatFootPixels)) * UnitsPerPixel; }
        }

        public void Validate()
        {
            if (WorldPixelsPerUnit <= 0 || UiPixelsPerUnit <= 0 || CatCanvasPixels <= 0 ||
                CatFootPixels < 0 || CatFootPixels >= CatCanvasPixels || ReferencePixels.x < 2 || ReferencePixels.y < 2 ||
                HomeHalfPixels.x <= 0 || HomeHalfPixels.y <= 0 || BowlWidthPixels <= 0 || BedWidthPixels <= 0 ||
                FeederHeightPixels <= 0 || DecorationHeightPixels <= 0)
                throw new InvalidOperationException("Invalid world art grid, foot, viewport or item extent");
        }

        public Vector3 Snap(Vector3 position)
        {
            return new Vector3(Mathf.Round(position.x * WorldPixelsPerUnit) * UnitsPerPixel,
                Mathf.Round(position.y * WorldPixelsPerUnit) * UnitsPerPixel, position.z);
        }

        public float CatScale(Sprite sprite)
        {
            return sprite == null || sprite.rect.height <= 0 ? 1 :
                CatCanvasPixels * UnitsPerPixel * sprite.pixelsPerUnit / sprite.rect.height;
        }

        public float HalfHeight(int width, int height)
        {
            if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException("viewport");
            return Mathf.Max(ReferencePixels.y, ReferencePixels.x * (float)height / width) * UnitsPerPixel * .5f;
        }

        public Vector2Int RenderSize(int width, int height)
        {
            float logicalHeight = HalfHeight(width, height) * 2 * WorldPixelsPerUnit;
            // Even dimensions keep a centred camera aligned to the logical grid.
            return new Vector2Int(Mathf.Max(2, Mathf.RoundToInt(logicalHeight * width / height * .5f) * 2),
                Mathf.Max(2, Mathf.RoundToInt(logicalHeight * .5f) * 2));
        }
    }
}
