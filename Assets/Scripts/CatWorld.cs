using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using CatGame.Idle;

namespace CatGame
{
    /// <summary>
    /// 世界场景的根：管相机（含 2D Pixel Perfect）、生成猫、生成 HUD。
    /// 猫窝 / 公园各自一个场景，场景里的地面和摆件是烘好的 SpriteRenderer，
    /// 这里只负责"相机 + 猫 + 界面"这些每个场景都要有的东西。
    /// </summary>
    public class CatWorld : MonoBehaviour
    {
        public static CatWorld I { get; private set; }

        [Header("场景标题（显示在 HUD 左上角）")]
        public string Title = "猫窝";

        [Header("猫的活动范围：中心与半宽半高（世界单位，1 单位 = 100 像素）")]
        public Vector2 FloorCenter = Vector2.zero;
        public Vector2 FloorHalf = new Vector2(8f, 2.4f);

        [Header("是否生成猫")]
        public bool SpawnCat = true;
        public bool EnableStoryIntro = true;
        public bool EnableStoryEvents = true;

        [Header("是否在世界中显示 7x6 猫窝平台")]
        public bool ShowPlatform = true;
        public RoomWalkSurface WalkSurface;

        public CatActor Cat { get; private set; }
        public CatRoster Roster { get; private set; }
        public IdleGameController Economy { get; private set; }
        public PlatformGrid Platform { get; private set; }
        public Hud Hud { get; private set; }
        int _viewWidth, _viewHeight;

        void LateUpdate()
        {
            if (_viewWidth != Screen.width || _viewHeight != Screen.height) FitWindow(Screen.width, Screen.height);
        }

        void FitWindow(int width, int height)
        {
            _viewWidth = width;
            _viewHeight = height;
            var camera = Camera.main;
            if (camera == null || _viewHeight <= 0) return;
            float aspect = (float)_viewWidth / _viewHeight;
            var pixelPerfect = camera.GetComponent<PixelPerfectCamera>();
            // Integer pixel projection crops below the reference size. Small/narrow
            // windows instead fit the complete world, with point-filtered source art.
            if (pixelPerfect != null)
                pixelPerfect.enabled = _viewWidth >= 1920 && _viewHeight >= 1080 && aspect >= 16f / 9f;
            if (pixelPerfect == null || !pixelPerfect.enabled)
                camera.orthographicSize = Mathf.Max(5.4f, 9.6f / aspect);
            var room = GameObject.Find("Room");
            var background = room != null ? room.GetComponent<SpriteRenderer>() : null;
            if (background != null && background.sprite != null)
            {
                float halfHeight = camera.orthographicSize;
                if (pixelPerfect != null && pixelPerfect.enabled)
                {
                    // Pixel Perfect fits full-screen pixels using an integer zoom, so
                    // non-integer reference sizes expose more than the 5.4-unit half-height.
                    int zoom = Mathf.Max(1, Mathf.Min(_viewWidth / pixelPerfect.refResolutionX,
                        _viewHeight / pixelPerfect.refResolutionY));
                    halfHeight = _viewHeight / (2f * pixelPerfect.assetsPPU * zoom);
                }
                var size = background.sprite.bounds.size;
                float cover = Mathf.Max(halfHeight * 2 * aspect / size.x, halfHeight * 2 / size.y);
                background.transform.localScale = new Vector3(cover, cover, 1);
            }
        }

        void Awake()
        {
            I = this;
            if (CatGame.I == null) gameObject.AddComponent<CatGame>();
            EnsureCamera();
            EnsureEventSystem();
        }

        void Start()
        {
            if (SpawnCat)
            {
                var state = SaveSystem.Instance != null ? SaveSystem.Instance.State : SaveSystem.CreateNewGame();
                Roster = gameObject.AddComponent<CatRoster>();
                Roster.Initialize(state, new Vector3(FloorCenter.x, FloorCenter.y, 0f), FloorHalf, walkSurface: WalkSurface);
                Economy = gameObject.AddComponent<IdleGameController>();
                Economy.Initialize(state, Roster);
                if (ShowPlatform)
                {
                    Platform = gameObject.AddComponent<PlatformGrid>();
                    Platform.Initialize(FloorCenter, FloorHalf, Economy);
                }
                var currentId = CatGame.I != null && CatGame.I.Current != null ? CatGame.I.Current.Id : "Milk";
                Cat = Roster.FindActor(currentId);
                if (Cat == null && Roster.VisibleCount > 0) Cat = Roster.GetFirstActor();
            }

            var hudGo = new GameObject("Hud", typeof(RectTransform));
            hudGo.transform.SetParent(transform, false);
            Hud = hudGo.AddComponent<Hud>();
            Hud.Build(Title);
        }

        void OnDestroy()
        {
            if (I == this) I = null;
        }

        void EnsureCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }

            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x8F, 0xB8, 0x9E, 0xFF);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (cam.GetComponent<Physics2DRaycaster>() == null)
                cam.gameObject.AddComponent<Physics2DRaycaster>();
#endif

            // 2D Pixel Perfect：素材 ppu = 100，参考分辨率 1920x1080
            var pp = cam.GetComponent<PixelPerfectCamera>();
            if (pp == null) pp = cam.gameObject.AddComponent<PixelPerfectCamera>();
            pp.assetsPPU = 100;
            pp.refResolutionX = 1920;
            pp.refResolutionY = 1080;
            pp.upscaleRT = false;
            pp.pixelSnapping = true;
            pp.cropFrameX = false;
            pp.cropFrameY = false;

            cam.orthographicSize = 1080f / 2f / 100f;   // 5.4：1 世界单位 = 100 像素
            FitWindow(Screen.width, Screen.height);
        }

        void EnsureEventSystem()
        {
            var es = FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }
            // 只有 EventSystem 没有输入模块，按钮一样点不动
            if (es.GetComponent<BaseInputModule>() == null)
                es.gameObject.AddComponent<StandaloneInputModule>();
        }
    }
}
