using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatGame.EditorTools
{
    /// <summary>
    /// 工程级操作：重新导入美术、构建 Windows 包、命令行一键跑。
    /// 场景生成在 SceneBuilders 里（主菜单 / 猫窝 / 公园）。
    /// </summary>
    public static class CatGameSetup
    {
        const string ArtRoot = "Assets/Resources/Art";

        /// <summary>构建场景，顺序即 Build Settings 顺序（第 0 个是启动场景）；Park 仅保留为预览资产。</summary>
        public static readonly string[] ScenePaths =
        {
            SceneBuilders.MenuPath,
            SceneBuilders.HomePath,
        };

        // ── 导入 ────────────────────────────────────────────────

        [MenuItem("CatGame/1. 重新导入美术资源", priority = 1)]
        public static void ForceReimportArt()
        {
            // 删掉 png 的 .meta，让 ArtImportRules 在"首次导入"时套用正确设置。
            // （不在批处理里反复 SaveAndReimport —— 那会让资源管线崩溃）
            int removed = 0;
            foreach (var meta in Directory.GetFiles(ArtRoot, "*.png.meta", SearchOption.AllDirectories))
            {
                File.Delete(meta);
                removed++;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("[CatGame] 已重新导入美术资源（清理 meta " + removed + " 个）");
        }

        static bool HasArtMeta()
        {
            var metas = Directory.GetFiles(ArtRoot, "*.png.meta", SearchOption.AllDirectories);
            return metas.Length >= 20;
        }

        static bool ScenesMissing()
        {
            foreach (var p in ScenePaths)
                if (!File.Exists(p)) return true;
            return false;
        }

        // ── 构建 ────────────────────────────────────────────────

        [MenuItem("CatGame/4. 构建 Windows 版", priority = 4)]
        public static void BuildWindows()
        {
            if (ScenesMissing()) SceneBuilders.BuildAllScenes();

            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Build/CatGame.exe"));
            Directory.CreateDirectory(Path.GetDirectoryName(outDir));

            var opts = new BuildPlayerOptions
            {
                scenes = ScenePaths,
                locationPathName = outDir,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log("[CatGame] 构建结果: " + summary.result + " / 大小 " + (summary.totalSize / 1024 / 1024) + " MB / 错误 " + summary.totalErrors);
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }

        /// <summary>命令行一键：导入 + 生成三个场景 + 构建。</summary>
        public static void BuildAll()
        {
            if (!HasArtMeta()) ForceReimportArt();
            SceneBuilders.BuildAllScenes();
            BuildWindows();
            AssetDatabase.SaveAssets();
            Debug.Log("[CatGame] BuildAll 完成");
        }

    }
}
