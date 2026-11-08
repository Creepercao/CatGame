using System.Collections.Generic;
using UnityEngine;

namespace CatGame
{
    /// <summary>Purchase cards and owned item markers share the same pixel artwork.</summary>
    public static class ItemVisualLibrary
    {
        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        public static Sprite Load(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            Sprite sprite;
            if (!Sprites.TryGetValue(id, out sprite))
            {
                sprite = Resources.Load<Sprite>("Art/Items/Pixel/" + id);
                if (sprite != null) Sprites[id] = sprite;
            }
            return sprite;
        }
    }
}
