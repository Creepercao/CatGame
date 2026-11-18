using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatGame.EditorTools
{
    public static class RoomThemeSetup
    {
        [MenuItem("CatGame/Art/Select room growth themes")]
        public static void SelectSettings()
        {
            var settings=Resources.Load<RoomThemeSettings>(RoomThemeSettings.ResourcePath);
            if(settings==null)throw new System.InvalidOperationException("Apply room growth assets first");
            Selection.activeObject=settings;EditorGUIUtility.PingObject(settings);
        }

        [MenuItem("CatGame/Art/Apply room growth assets")]
        public static void Apply()
        {
            const string root="Assets/Resources/Art/Room/Pixel/Growth";
            AssetDatabase.Refresh();
            foreach(var path in Directory.GetFiles(root,"*.png",SearchOption.AllDirectories))
            {
                string normalized=path.Replace('\\','/');
                var importer=AssetImporter.GetAtPath(normalized) as TextureImporter;
                if(importer==null)throw new System.InvalidOperationException("Missing room texture importer "+path);
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=WorldArtSettings.Current.WorldPixelsPerUnit;
                importer.spritePivot=new Vector2(.5f,.5f);importer.filterMode=FilterMode.Point;
                importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048;
                ArtImportRules.SetPixelPlatform(importer,"Standalone");ArtImportRules.SetPixelPlatform(importer,"Android");
                importer.SaveAndReimport();
            }
            const string asset="Assets/Resources/Settings/RoomThemeSettings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<RoomThemeSettings>(asset);
            if(settings==null)
            {
                settings=ScriptableObject.CreateInstance<RoomThemeSettings>();
                AssetDatabase.CreateAsset(settings,asset);
            }
            settings.Validate();AssetDatabase.SaveAssets();
            Debug.Log("[RoomThemes] Imported pixel assets; existing config thresholds preserved");
        }
    }
}
