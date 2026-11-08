using System;
using UnityEngine;

namespace CatGame
{
    public static class WorldItemArt
    {
        [Serializable] public class Profile { public string id; public float width, height, centerX, bottom; }
        [Serializable] class Profiles { public Profile[] profiles; }
        static Profiles _profiles;
        public static Profile Get(string id)
        {
            if (_profiles == null) _profiles = JsonUtility.FromJson<Profiles>(Resources.Load<TextAsset>("Art/Items/Pixel/world_profiles").text);
            foreach (var profile in _profiles.profiles) if (profile.id == id) return profile;
            throw new ArgumentException("Missing item art profile: " + id);
        }
        public static float Scale(string id, float ppu)
        {
            var profile = Get(id);
            if (id.EndsWith("_2")) return 1.9f * ppu / profile.width; // beds fit a cat
            if (id.EndsWith("_3")) return 1.15f * ppu / profile.height; // upright feeder
            if (id.EndsWith("_4")) return .7f * ppu / profile.height;
            return 1.25f * ppu / profile.width;
        }
    }
}
