using System;
using UnityEditor;
using UnityEngine;

namespace CatGame.EditorTools
{
    /// <summary>Stamp an explicit source-controlled version before release builds.</summary>
    public static class BuildVersion
    {
        public static void ApplyFromEnvironment()
        {
            string version=Environment.GetEnvironmentVariable("CATGAME_RELEASE_VERSION");
            int code;
            if(string.IsNullOrEmpty(version) ||
                !System.Text.RegularExpressions.Regex.IsMatch(version,@"^\d+\.\d+\.\d+$") ||
                !int.TryParse(Environment.GetEnvironmentVariable("CATGAME_ANDROID_VERSION_CODE"),out code) || code<1)
                throw new ArgumentException("Explicit semantic version and positive Android versionCode are required");
            PlayerSettings.bundleVersion=version;
            PlayerSettings.Android.bundleVersionCode=code;
            AssetDatabase.SaveAssets();
            Debug.Log("[Version] "+version+" / Android "+code);
        }
    }
}
