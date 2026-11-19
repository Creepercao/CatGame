using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CatGame.EditorTools
{
    public static class LauncherArtSetup
    {
        [MenuItem("CatGame/Art/Apply desktop launcher icon")]
        public static void Apply()
        {
            const string path = "Assets/Art/AndroidIcons/MomoLegacy.png";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon == null) throw new InvalidOperationException("Missing launcher texture " + path);
            var group = BuildTargetGroup.Standalone;
            var sizes = PlayerSettings.GetIconSizesForTargetGroup(group);
            if (sizes.Length == 0) throw new InvalidOperationException("The desktop icon sizes are unavailable");
            PlayerSettings.SetIconsForTargetGroup(group, sizes.Select(_ => icon).ToArray());
            AssetDatabase.SaveAssets();
            Debug.Log("[LauncherArt] Applied the Milk desktop icon at " + sizes.Length + " sizes");
        }
    }
}
