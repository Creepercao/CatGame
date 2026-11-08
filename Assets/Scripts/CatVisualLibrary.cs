using System;
using System.Collections.Generic;
using CatGame.Idle;
using UnityEngine;

namespace CatGame
{
    /// <summary>Loads the art-backed clips available for each of the eleven economic cat types.</summary>
    public static class CatVisualLibrary
    {
        static readonly Dictionary<string, CatBreed> Cache = new Dictionary<string, CatBreed>();
        public static bool UsesPixel64Art(string id)
        { return id == "Egypt" || id == "Milk" || id == "Tabby" || id == "Snow"
            || id == "Tuxedo" || id == "Siamese" || id == "Tiger"
            || id == "Calico" || id == "Batcat" || id == "Demon" || id == "Xmas"; }

        public static CatBreed Load(CatDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            CatBreed cached;
            if (Cache.TryGetValue(definition.id, out cached)) return cached;

            bool pixel64 = UsesPixel64Art(definition.id);
            string primaryRoot = pixel64 ? "Art/Cats/Pixel64/" + definition.id : definition.path;
            var breed = new CatBreed
            {
                Id = definition.id,
                Name = definition.name,
                Idle = LoadFrames(primaryRoot, "Idle"),
                Walk = LoadFrames(primaryRoot, "Walk"),
                Run = LoadFrames(definition.path, "Run"),
                Jump = LoadFrames(primaryRoot, "Jump"),
                Sleep = LoadFrames(pixel64 ? primaryRoot : definition.path, "Sleep"),
                Eat = LoadFrames(definition.path, "Eat"),
                Attack = LoadFrames(definition.path, "Attack"),
                UseWalkWhileMoving = pixel64,
                UseSleepWhileResting = pixel64,
            };
            if (breed.Idle == null || breed.Idle.Length == 0)
                throw new InvalidOperationException("Missing Idle sprites for cat " + definition.id + " at " + definition.path);
            Cache.Add(definition.id, breed);
            return breed;
        }

        static Sprite[] LoadFrames(string root, string clip)
        {
            var sprites = Resources.LoadAll<Sprite>(root + "/" + clip);
            if (sprites == null || sprites.Length == 0) return null;
            Array.Sort(sprites, (a, b) => FrameIndex(a.name).CompareTo(FrameIndex(b.name)));
            return sprites;
        }

        static int FrameIndex(string name)
        {
            int separator = name.LastIndexOf('_');
            int frame;
            return separator >= 0 && int.TryParse(name.Substring(separator + 1), out frame) ? frame : 0;
        }
    }
}
