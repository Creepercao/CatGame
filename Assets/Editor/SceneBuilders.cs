using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame.EditorTools
{
    /// <summary>
    /// 烘场景：MainMenu（主菜单）/ Home（猫窝）/ Park（公园预览资产）。
    /// 世界里的地面与摆件烘成真的 SpriteRenderer 对象（编辑器里可见、可拖、可做 Prefab），
    /// 猫和 HUD 由 CatWorld 运行时生成。
    ///
    /// 坐标系：像素 → 世界单位 1:100（素材 ppu = 100，相机 orthographicSize = 5.4）。
    /// 像素图按整数倍放大（摆件 1×/2×/3×，地砖 2×），配合 PixelPerfectCamera 保持清晰。
    /// </summary>
    public static class SceneBuilders
    {
        public const string MenuPath = "Assets/Scenes/MainMenu.unity";
        public const string HomePath = "Assets/Scenes/Home.unity";
        public const string ParkPath = "Assets/Scenes/Park.unity";

        const string CozyRoot = "Assets/Resources/Art/CozyGardenAssets";
        const string TilesPath = "Assets/Resources/Art/Tiles/spritesheet_x4.png";
        const string RoomRoot = "Assets/Resources/Art/Room/Pixel/";
        const string RoomPath = RoomRoot + "architecture.png";
        [System.Serializable] class RoomLayout { public int width, height, cell; public RoomLayer[] layers; }
        [System.Serializable] class RoomLayer { public string name; public RoomPlacement[] placements; }
        [System.Serializable] class RoomPlacement { public string asset; public int x, y; }

        const float Px = 0.01f;         // 1 像素 = 0.01 世界单位

        // ── 图集编号（切片名 spritesheet_x4_N 里的 N）────────────
        const int G0 = 0, G1 = 1, G3 = 3;
        const int PaveLight = 9, Asphalt = 11, Stripe = 15;
        const int Sand = 37, Cobble = 39, Water = 21;
        const int Bench = 54, Fountain = 78, Statue = 77;
        const int TreeA = 56, TreeB = 57, TreeOrange = 58, Pine = 59;
        const int Bush = 60, PlanterA = 61, PlanterB = 62, PlanterC = 63;
        const int Hydrant = 64, MailboxBlue = 65, SignWood = 74, SignGreen = 75;
        const int CarRed = 80, CarBlue = 81, CarYellow = 82;
        const int Rock = 85, FenceWood = 86, Railing = 87;
        const int FlowerRed = 88, FlowerYellow = 89, FlowerBlue = 90, Lamp = 91;

        [MenuItem("CatGame/2. 生成全部场景（主菜单/猫窝/公园）", priority = 2)]
        public static void BuildAllScenes()
        {
            BuildMenu();
            BuildHome();
            BuildPark();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuPath, true),
                new EditorBuildSettingsScene(HomePath, true),
            };
            AssetDatabase.SaveAssets();
            Debug.Log("[Scenes] 三个场景资产已生成；Build Settings 仅启用主菜单和猫窝");
        }

        // ── 主菜单 ───────────────────────────────────────────────

        [MenuItem("CatGame/2a. 生成主菜单场景", priority = 21)]
        public static void BuildMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeCamera(new Color32(0xF2, 0xE3, 0xCF, 0xFF));

            var menu = new GameObject("MainMenu", typeof(RectTransform));
            menu.AddComponent<MainMenuUI>();

            // EventSystem 必须带输入模块，否则按钮点不动
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            Save(scene, MenuPath);
        }

        // ── 猫窝 ─────────────────────────────────────────────────

        [MenuItem("CatGame/2b. 生成猫窝场景", priority = 22)]
        public static void BuildHome()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeCamera(new Color32(0xEC, 0xE4, 0xD8, 0xFF));

            // Architecture and furniture remain separate semantic groups.
            var room = LoadSprite(RoomPath, null);
            RoomWalkSurface surface = null;
            if (room != null)
            {
                var go = new GameObject("Room");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = room;
                sr.sortingOrder = -100;
                float sw = room.rect.width, sh = room.rect.height;
                float cover = Mathf.Max(1920f / sw, 1080f / sh);
                go.transform.localScale = new Vector3(cover, cover, 1f);
                go.transform.position = Vector3.zero;
                surface = go.AddComponent<RoomWalkSurface>();
                surface.Background = sr;
                BuildRoomFurniture(go.transform);
            }
            else
            {
                Debug.LogWarning("[Scenes] 缺少房间背景: " + RoomPath);
            }

            var world = new GameObject("World");
            var cw = world.AddComponent<CatWorld>();
            cw.WalkSurface = surface;
            cw.Title = "猫窝";
            // Left HUD / full right world; surface further checks wall and prop footprints.
            cw.FloorCenter = new Vector2(2.2f, -0.2f);
            cw.FloorHalf = new Vector2(6.5f, 3.6f);

            Save(scene, HomePath);
        }

        static void BuildRoomFurniture(Transform room)
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(RoomRoot + "layout.json");
            if (json == null) throw new System.InvalidOperationException("Missing room layout");
            var layout = JsonUtility.FromJson<RoomLayout>(json.text);
            var parent = new GameObject("BackWallFurniture").transform;
            parent.SetParent(room, false);
            var shelf = new GameObject("Bookshelf").transform;
            shelf.SetParent(parent, false);
            var shelfPieces = new List<SpriteRenderer>();
            foreach (var layer in layout.layers)
            {
                if (layer.placements == null) continue;
                foreach (var p in layer.placements)
                {
                    var sprite = LoadSprite(RoomRoot + p.asset + ".png", null);
                    if (sprite == null) throw new System.InvalidOperationException("Missing room prop " + p.asset);
                    bool book = p.asset.StartsWith("bookshelf_");
                    var piece = new GameObject(p.asset);
                    piece.transform.SetParent(book ? shelf : parent, false);
                    piece.transform.localPosition = new Vector3(
                        (p.x + layout.cell / 2f - layout.width / 2f) / 100f,
                        (layout.height / 2f - p.y - layout.cell / 2f) / 100f, 0);
                    var renderer = piece.AddComponent<SpriteRenderer>();
                    renderer.sprite = sprite;
                    renderer.sortingOrder = -50;
                    if (book) { shelfPieces.Add(renderer); continue; }
                    if (p.asset == "window") continue;
                    float groundY = p.asset == "plant" ? 69 : p.asset == "cat_bed" ? 72 : 126;
                    var depth = piece.AddComponent<RoomFurnitureDepth>();
                    depth.LocalGround = new Vector3(0, (p.y + layout.cell / 2f - groundY) / 100f, 0);
                    depth.Pieces = new[] { renderer };
                }
            }
            var shelfDepth = shelf.gameObject.AddComponent<RoomFurnitureDepth>();
            shelfDepth.LocalGround = new Vector3((144 - layout.width / 2f) / 100f, (layout.height / 2f - 125) / 100f, 0);
            shelfDepth.Pieces = shelfPieces.ToArray();
        }

        // ── 公园 ─────────────────────────────────────────────────

        [MenuItem("CatGame/2c. 生成公园场景", priority = 23)]
        public static void BuildPark()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            MakeCamera(new Color32(0x8F, 0xB8, 0x9E, 0xFF));

            var ground = new GameObject("Ground").transform;
            var farDecor = new GameObject("FarDecor").transform;
            var vegetation = new GameObject("Vegetation").transform;
            var landmarks = new GameObject("Landmarks").transform;
            var foreground = new GameObject("Foreground").transform;

            // 地面
            TiledRect(ground, "Grass", G0, -960f, -300f, 1920f, 860f, -30);
            TiledRect(ground, "GrassPatchA", G1, -760f, 420f, 256f, 256f, -29);
            TiledRect(ground, "GrassPatchB", G3, 640f, 120f, 256f, 256f, -29);
            TiledRect(ground, "Sidewalk", PaveLight, -960f, -412f, 1920f, 128f, -20);
            TiledRect(ground, "Road", Asphalt, -960f, -540f, 1920f, 128f, -22);
            TiledRect(ground, "Crosswalk", Stripe, -128f, -540f, 256f, 128f, -21);
            TiledRect(ground, "Plaza", Sand, -384f, -284f, 768f, 544f, -18);
            TiledRect(ground, "Promenade", Cobble, -960f, 260f, 1920f, 128f, -19);
            TiledRect(ground, "Pond", Water, -800f, -260f, 256f, 256f, -17);

            // 摆件（y 从大到小生成：UI/排序一致，靠后的先画）
            var items = new List<Item>
            {
                new Item(TreeA, -860f, 400f, 3), new Item(TreeOrange, -600f, 400f, 3),
                new Item(TreeB, -340f, 400f, 3), new Item(TreeB, 340f, 400f, 3),
                new Item(TreeOrange, 600f, 400f, 3), new Item(TreeA, 860f, 400f, 3),
                new Item(Pine, -620f, 300f, 3), new Item(Pine, 620f, 300f, 3),

                new Item(Statue, -300f, 232f, 3),
                new Item(Lamp, -430f, 232f, 2), new Item(Lamp, 430f, 232f, 2),
                new Item(Lamp, -430f, -232f, 2), new Item(Lamp, 430f, -232f, 2),

                new Item(PlanterA, -318f, 206f, 2), new Item(PlanterB, 318f, 206f, 2),
                new Item(PlanterC, -318f, -196f, 2), new Item(PlanterA, 318f, -196f, 2),

                new Item(Bush, -860f, 160f, 2), new Item(Bush, -860f, -40f, 2),
                new Item(Bush, 860f, 160f, 2), new Item(Bush, 860f, -60f, 2),

                new Item(Fountain, 0f, -20f, 3),
                new Item(Bench, -230f, 96f, 2), new Item(Bench, 230f, 96f, 2),
                new Item(Bench, 0f, -226f, 2),

                // 池塘一圈石头 + 花，避免一片纯蓝方块
                new Item(Rock, -826f, -286f, 1), new Item(Rock, -518f, -286f, 1),
                new Item(Rock, -826f, -2f, 1), new Item(Rock, -518f, -2f, 1),
                new Item(FlowerYellow, -760f, -300f, 1), new Item(FlowerRed, -600f, -300f, 1),
                new Item(Bush, -856f, -170f, 2),

                new Item(FlowerBlue, 700f, -140f, 1), new Item(FlowerRed, 780f, -260f, 1),
                new Item(FlowerYellow, -700f, 60f, 1),

                // 两侧草地的零散点缀，别让大片绿地太空
                new Item(Bush, 640f, 120f, 2), new Item(FlowerRed, 660f, 236f, 1),
                new Item(FlowerYellow, 780f, 180f, 1), new Item(Rock, 760f, 60f, 1),
                new Item(FlowerBlue, -520f, 180f, 1), new Item(Rock, -640f, 236f, 1),
                new Item(FlowerRed, -800f, -80f, 1),

                new Item(SignWood, -520f, -348f, 2), new Item(SignGreen, 520f, -348f, 2),
                new Item(Hydrant, 320f, -352f, 2), new Item(MailboxBlue, 660f, -352f, 2),

                new Item(CarRed, -700f, -476f, 2), new Item(CarBlue, -540f, -476f, 2),
                new Item(CarYellow, 620f, -476f, 2),
            };

            // 左下角的木栅栏：只摆一小段当边角装饰，不要长排栏杆（看着像一堵墙）
            for (float x = -900f; x <= -660f; x += 96f) items.Add(new Item(FenceWood, x, -250f, 2));

            items.Sort((a, b) => b.Y.CompareTo(a.Y));
            foreach (var it in items) Prop(SelectPropGroup(it, farDecor, vegetation, landmarks, foreground), it);

            var world = new GameObject("World");
            var cw = world.AddComponent<CatWorld>();
            cw.Title = "公园";
            cw.ShowPlatform = false;
            cw.FloorCenter = new Vector2(0f, -0.6f);
            cw.FloorHalf = new Vector2(8.6f, 2.6f);

            Save(scene, ParkPath);
        }

        // ── 截图（自检用，不进游戏） ─────────────────────────────

        [MenuItem("CatGame/3. 场景预览截图（公园）", priority = 3)]
        public static void CapturePark() { CaptureScene(ParkPath, "park.png"); }

        /// <summary>三个场景各截一张（命令行一次调用）。</summary>
        public static void CaptureAllScenes()
        {
            CaptureScene(MenuPath, "menu.png");
            CaptureScene(HomePath, "home.png");
            CaptureScene(ParkPath, "park.png");
        }

        [MenuItem("CatGame/3. 场景预览截图（猫窝）", priority = 31)]
        public static void CaptureHome() { CaptureScene(HomePath, "home.png"); }

        [MenuItem("CatGame/3. 场景预览截图（主菜单）", priority = 32)]
        public static void CaptureMenu() { CaptureScene(MenuPath, "menu.png"); }

        public static void CaptureScene(string scenePath, string fileName)
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning("[Scenes] 没有主相机: " + scenePath); return; }

            // ScreenSpaceOverlay 画布的相机拍不到，临时切 ScreenSpaceCamera
            var canvas = Object.FindObjectOfType<Canvas>();
            var prevMode = canvas != null ? canvas.renderMode : RenderMode.ScreenSpaceOverlay;
            var prevCam = canvas != null ? canvas.worldCamera : null;
            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10f;
                Canvas.ForceUpdateCanvases();
            }

            const int W = 1920, H = 1080;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();

            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;

            var path = System.IO.Path.GetFullPath(
                System.IO.Path.Combine(Application.dataPath, "../Logs/shots/" + fileName));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            if (canvas != null)
            {
                canvas.renderMode = prevMode;
                canvas.worldCamera = prevCam;
            }
            Debug.Log("[Scenes] 截图: " + path);
        }

        // ── 工具 ────────────────────────────────────────────────

        struct Item
        {
            public int Tile;
            public float X, Y;
            public int Scale;
            public Item(int tile, float x, float y, int scale) { Tile = tile; X = x; Y = y; Scale = scale; }
        }

        static void Prop(Transform parent, Item it)
        {
            var sp = LoadTile(it.Tile);
            if (sp == null) return;

            var go = new GameObject("Tile" + it.Tile);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            go.transform.localScale = new Vector3(it.Scale, it.Scale, 1f);
            go.transform.position = new Vector3(it.X * Px, it.Y * Px, 0f);
            sr.sortingOrder = Order(it.Y);
        }

        static Transform SelectPropGroup(Item item, Transform farDecor, Transform vegetation, Transform landmarks, Transform foreground)
        {
            // Keep only a few semantic/depth groups. Individual sprites retain their own sortingOrder
            // because their vertical overlap still determines which object appears in front.
            if (item.Y <= -300f) return foreground;
            if (item.Tile == TreeA || item.Tile == TreeB || item.Tile == TreeOrange || item.Tile == Pine) return farDecor;
            if (item.Tile == Bench || item.Tile == Fountain || item.Tile == Statue || item.Tile == Lamp
                || item.Tile == SignWood || item.Tile == SignGreen || item.Tile == Hydrant || item.Tile == MailboxBlue
                || item.Tile == CarRed || item.Tile == CarBlue || item.Tile == CarYellow) return landmarks;
            return vegetation;
        }

        /// <summary>用同一种砖平铺一个矩形（像素给，内部按 2 倍放大）。</summary>
        static void TiledRect(Transform parent, string name, int tile, float x0, float y0, float w, float h, int order)
        {
            var sp = LoadTile(tile);
            if (sp == null) return;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            go.transform.localScale = new Vector3(2f, 2f, 1f);
            sr.size = new Vector2(w * Px * 0.5f, h * Px * 0.5f);
            go.transform.position = new Vector3((x0 + w * 0.5f) * Px, (y0 + h * 0.5f) * Px, 0f);
            sr.sortingOrder = order;
        }

        static int Order(float yPx) { return Mathf.RoundToInt((10f - yPx * Px) * 100f); }

        static void MakeCamera(Color bg)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = bg;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            go.transform.position = new Vector3(0f, 0f, -10f);
        }

        static void Save(UnityEngine.SceneManagement.Scene scene, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[Scenes] 已生成: " + path);
        }

        static Sprite LoadTile(int index)
        {
            return LoadSprite(TilesPath, "spritesheet_x4_" + index);
        }

        static Sprite LoadSprite(string path, string sliceName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[Scenes] 素材不存在: " + path);
                return null;
            }
            Sprite first = null;
            foreach (var a in assets)
            {
                var s = a as Sprite;
                if (s == null) continue;
                if (first == null) first = s;
                if (sliceName != null && s.name == sliceName) return s;
            }
            if (sliceName != null) Debug.LogWarning("[Scenes] " + path + " 缺少切片 " + sliceName);
            return first;
        }
    }
}
