using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatGame.EditorTools
{
    /// <summary>
    /// 美术资源的导入规则：猫咪横条按帧高切帧（Point 过滤，像素清晰），
    /// 房间/UI 图按整图 Sprite 导入（Bilinear）。
    ///
    /// 只在"第一次导入"（或 meta 被删掉之后）生效：规则跑完会在 meta 的 userData 里写下标记，
    /// 之后无论你在 Inspector 里怎么手调、点多少次 Apply，都不会再被覆盖。
    /// 想重新套用规则：删掉该图的 .png.meta（或跑 CatGameSetup.ForceReimportArt）。
    /// 想彻底让某张图不受管：把它的 userData 手填成 manual。
    /// </summary>
    public class ArtImportRules : AssetPostprocessor
    {
        public const int CatCell = 32;
        const string ArtRoot = "Assets/Resources/Art/";

        /// <summary>认领标记：写在 TextureImporter.userData 里，表示"这张图已经按规则导过了"。</summary>
        const string RuleMarker = "catgame-import-rules-v1";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            if (!assetPath.ToLowerInvariant().EndsWith(".png")) return;

            var imp = (TextureImporter)assetImporter;

            // 已经认领过（或用户标了 manual）→ 尊重现有 meta，不再插手
            if (imp.userData == RuleMarker || imp.userData == "manual") return;

            bool isCat = assetPath.Replace('\\', '/').Contains("/Cats/");

            // 像素图（猫咪，或放在 Pixel 目录 / 名字带 Pixel 的背景）用点采样；
            // 平滑矢量风的图用双线性。
            bool isPixel = isCat
                || assetPath.Replace('\\', '/').Contains("/Pixel/")
                || Path.GetFileNameWithoutExtension(assetPath).IndexOf("pixel", System.StringComparison.OrdinalIgnoreCase) >= 0;

            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteExtrude = 0;
            s.alphaIsTransparency = true;
            s.mipmapEnabled = false;
            s.filterMode = isPixel ? FilterMode.Point : FilterMode.Bilinear;

            imp.textureType = TextureImporterType.Sprite;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;

            if (isCat)
            {
                // 宽条必须保持原尺寸：吃饭 10 帧 = 640px、挥爪 11 帧 = 704px，
                // 一旦上限设成 512 就会被 Unity 降采样，像素猫直接糊掉（踩过一次）。
                imp.maxTextureSize = 2048;
                s.spriteMode = (int)SpriteImportMode.Multiple;
                s.spritePixelsPerUnit = CatCell;
                imp.SetTextureSettings(s);
                imp.spritesheet = BuildCatFrames(assetPath);
            }
            else
            {
                imp.maxTextureSize = 2048;
                s.spriteMode = (int)SpriteImportMode.Single;
                s.spritePixelsPerUnit = 100;
                if (assetPath.Contains("/UI/Pixel/"))
                {
                    string name = Path.GetFileNameWithoutExtension(assetPath);
                    int border = name == "panel_soft" ? 8 : name == "panel_pill" ? 16 : 31;
                    s.spriteBorder = new Vector4(border, border, border, border);
                }
                imp.SetTextureSettings(s);
            }

            // SetTextureSettings 会带回旧值，最后再压一遍过滤方式
            imp.filterMode = s.filterMode;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;

            // 像素图在导出平台上也不许压缩、不许缩尺寸（平台覆盖的旧值会盖掉上面的默认值）
            if (isPixel)
            {
                SetPixelPlatform(imp, "Standalone");
                SetPixelPlatform(imp, "WebGL");
            }

            // 认领：写下标记，以后再导入就跳过，手调设置不会被规则覆盖
            imp.userData = RuleMarker;
        }

        static void SetPixelPlatform(TextureImporter imp, string platform)
        {
            var ps = imp.GetPlatformTextureSettings(platform);
            ps.overridden = true;
            ps.maxTextureSize = 2048;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ps.format = TextureImporterFormat.Automatic;
            ps.crunchedCompression = false;
            imp.SetPlatformTextureSettings(ps);
        }

        static SpriteMetaData[] BuildCatFrames(string assetPath)
        {
            var size = ReadPngSize(assetPath);

            // 单行横条：一格的高度就是帧高。32（原素材）和 64（AI 生成的高清帧）都支持，
            // 帧尺寸不同不影响显示，因为界面里是拉伸到固定尺寸。
            int cell = Mathf.Max(16, size.y);
            int cols = Mathf.Max(1, size.x / cell);
            int rows = Mathf.Max(1, size.y / cell);
            var clip = Path.GetFileNameWithoutExtension(assetPath);
            var list = new List<SpriteMetaData>(cols * rows);

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    list.Add(new SpriteMetaData
                    {
                        name = clip + "_" + (y * cols + x),
                        rect = new Rect(x * cell, size.y - (y + 1) * cell, cell, cell),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        border = Vector4.zero,
                    });
                }
            }
            return list.ToArray();
        }

        /// <summary>从 PNG 的 IHDR 读宽高，不依赖图库。</summary>
        public static Vector2Int ReadPngSize(string assetPath)
        {
            string abs = Path.IsPathRooted(assetPath)
                ? assetPath
                : Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            if (!File.Exists(abs))
                return new Vector2Int(CatCell * 7, CatCell);

            var buf = new byte[24];
            using (var fs = File.OpenRead(abs))
            {
                int read = 0;
                while (read < 24)
                {
                    int n = fs.Read(buf, read, 24 - read);
                    if (n <= 0) break;
                    read += n;
                }
                if (read < 24) return new Vector2Int(CatCell * 7, CatCell);
            }
            int w = (buf[16] << 24) | (buf[17] << 16) | (buf[18] << 8) | buf[19];
            int h = (buf[20] << 24) | (buf[21] << 16) | (buf[22] << 8) | buf[23];
            return (w <= 0 || h <= 0) ? new Vector2Int(CatCell * 7, CatCell) : new Vector2Int(w, h);
        }
    }
}
