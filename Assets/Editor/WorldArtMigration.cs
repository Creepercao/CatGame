using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGame.Idle;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CatGame.EditorTools
{
    /// <summary>Explicit, repeatable import migration. Never deletes metadata or reslices existing sprites.</summary>
    public static class WorldArtMigration
    {
        const string SettingsPath = "Assets/Resources/Settings/WorldArtSettings.asset";
        const string ArtRoot = "Assets/Resources/Art/";
        [Serializable] sealed class Entry { public string path, beforeIdentity, afterIdentity, previousUserData, role; }
        [Serializable] sealed class Report
        {
            public string sourceCommit;
            public bool complete;
            public List<Entry> entries = new List<Entry>();
        }

        [MenuItem("CatGame/Art/Select world art settings")]
        public static void SelectSettings() { Selection.activeObject = EnsureSettings(); }

        public static WorldArtSettings EnsureSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldArtSettings>(SettingsPath);
            if (settings == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                AssetDatabase.Refresh();
                settings = ScriptableObject.CreateInstance<WorldArtSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
                AssetDatabase.SaveAssets();
            }
            settings.Validate();
            return settings;
        }

        [MenuItem("CatGame/Art/Apply world art settings and rebuild Home")]
        public static void Apply()
        {
            var report = new Report { sourceCommit = Environment.GetEnvironmentVariable("CATGAME_F1_SOURCE") };
            string output = Environment.GetEnvironmentVariable("CATGAME_F1_MIGRATION_ROOT");
            if (string.IsNullOrEmpty(output)) output = "Build/F1/import-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(output);
            try
            {
                var settings = EnsureSettings();
                var paths = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (var definition in CatCatalog.All)
                    foreach (var action in ActionNames.All)
                    {
                        var clip = CatVisualLibrary.Load(definition).Clip(action);
                        if (clip == null) continue;
                        foreach (var frame in clip) paths[AssetDatabase.GetAssetPath(frame)] = "cat";
                    }
                // Compatibility assets stay available; the deleted old Calico Walk is never recreated.
                foreach (string path in Directory.GetFiles(ArtRoot + "Cats", "*.png", SearchOption.AllDirectories))
                    paths[path.Replace('\\', '/')] = "cat";
                foreach (string path in Directory.GetFiles(ArtRoot + "Room/Pixel", "*.png")) paths[path.Replace('\\', '/')] = "room";
                foreach (string path in Directory.GetFiles(ArtRoot + "Items/Pixel", "*.png")) paths[path.Replace('\\', '/')] = "item";
                foreach (string path in Directory.GetFiles(ArtRoot + "UI/Pixel", "*.png")) paths[path.Replace('\\', '/')] = "ui";
                foreach (var pair in paths)
                {
                    var importer = AssetImporter.GetAtPath(pair.Key) as TextureImporter;
                    if (importer == null) throw new InvalidOperationException("Missing texture importer " + pair.Key);
                    var entry = new Entry { path = pair.Key, role = pair.Value,
                        beforeIdentity = Identity(pair.Key), previousUserData = importer.userData };
                    report.entries.Add(entry);
                    // Manual meant 'do not automatically reimport'. This explicit operation changes only
                    // documented density/pivot/filter/platform settings and preserves all other geometry.
                    importer.spritePixelsPerUnit = pair.Value == "ui" ? settings.UiPixelsPerUnit : settings.WorldPixelsPerUnit;
                    if (pair.Value == "cat") SetPivot(importer, new Vector2(.5f, settings.CatFootFraction));
                    else if (pair.Value == "item")
                    {
                        string id = Path.GetFileNameWithoutExtension(pair.Key);
                        if (id.StartsWith("item_", StringComparison.Ordinal))
                        {
                            var profile = WorldItemArt.Get(id);
                            var size = ArtImportRules.ReadPngSize(pair.Key);
                            SetPivot(importer, new Vector2(profile.centerX / size.x, profile.bottom / size.y));
                        }
                    }
                    importer.filterMode = FilterMode.Point;
                    importer.mipmapEnabled = false;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    foreach (string platform in new[] { "Standalone", "Android", "WebGL" })
                        ArtImportRules.SetPixelPlatform(importer, platform);
                    importer.SaveAndReimport();
                    entry.afterIdentity = Identity(pair.Key);
                    if (entry.beforeIdentity != entry.afterIdentity)
                        throw new InvalidOperationException("Sprite identity/rect changed: " + pair.Key);
                }
                SceneBuilders.BuildHome();
                AssetDatabase.SaveAssets();
                report.complete = true;
                Debug.Log("[WorldArt] Applied " + report.entries.Count + " textures with identities preserved");
            }
            finally { File.WriteAllText(Path.Combine(output, "migration.json"), JsonUtility.ToJson(report, true)); }
        }

        public static void ApplyBatch()
        {
            try { Apply(); EditorApplication.Exit(0); }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }

        static void SetPivot(TextureImporter importer, Vector2 pivot)
        {
            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects();
            foreach (var rect in rects) { rect.pivot = pivot; rect.alignment = SpriteAlignment.Custom; }
            provider.SetSpriteRects(rects); // Existing SpriteRect.spriteID and name/fileID pairs are retained.
            provider.Apply();
        }

        static string Identity(string path)
        {
            return string.Join("\n", AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name)
                .Select(sprite =>
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long id);
                    return sprite.name + "|" + guid + "|" + id + "|" + sprite.rect;
                }));
        }
    }
}
