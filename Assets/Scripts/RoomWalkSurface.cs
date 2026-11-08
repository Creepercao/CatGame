using UnityEngine;

namespace CatGame
{
    /// <summary>Floor geometry in background source pixels (origin at top left).</summary>
    public sealed class RoomWalkSurface : MonoBehaviour
    {
        public SpriteRenderer Background;
        // Furniture ground footprints, not their upright silhouettes.
        public Rect[] Obstacles = {
            new Rect(118, 110, 52, 15), new Rect(204, 56, 25, 13),
            new Rect(257, 57, 54, 15), new Rect(458, 115, 52, 11)
        };

        public Vector2 SourcePoint(Vector3 world)
        {
            var local = Background.transform.InverseTransformPoint(world);
            var sprite = Background.sprite;
            return new Vector2(local.x * sprite.pixelsPerUnit + sprite.pivot.x,
                sprite.rect.height - (local.y * sprite.pixelsPerUnit + sprite.pivot.y));
        }

        public bool Contains(Vector3 foot)
        {
            if (Background == null || Background.sprite == null) return false;
            var p = SourcePoint(foot);
            if (p.x < 2 || p.x >= 510 || p.y >= 286 ||
                p.y < Mathf.Round(40 + Mathf.Abs(p.x - 256) * .36f) + 6) return false;
            foreach (var rect in Obstacles)
                if (rect.Contains(p)) return false;
            return true;
        }

        public bool AllowsSegment(Vector3 from, Vector3 to)
        {
            // Sample at <= one source pixel: do not cross a thin footprint at large dt.
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(SourcePoint(from), SourcePoint(to))));
            for (int i = 0; i <= steps; i++)
                if (!Contains(Vector3.Lerp(from, to, (float)i / steps))) return false;
            return true;
        }
    }
}
