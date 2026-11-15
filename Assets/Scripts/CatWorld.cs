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

        [Header("猫的完整画布活动范围：中心与半宽半高（世界单位，密度见WorldArtSettings）")]
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
            if (_viewWidth <= 0) return;
            float aspect = (float)_viewWidth / _viewHeight;
            var legacy = camera.GetComponent<PixelPerfectCamera>();
            if (legacy != null) legacy.enabled = false;
            var pixelCamera = camera.GetComponent<WorldPixelCamera>();
            if (pixelCamera == null) pixelCamera = camera.gameObject.AddComponent<WorldPixelCamera>();
            pixelCamera.Fit(width, height);
            var room = GameObject.Find("Room");
            var background = room != null ? room.GetComponent<SpriteRenderer>() : null;
            if (background != null && background.sprite != null)
            {
                float halfHeight = camera.orthographicSize;
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
                    Platform.Initialize(FloorCenter + WorldArtSettings.Current.ItemGridOffset, FloorHalf, Economy);
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

            // The world raster has its own logical grid; overlay UI stays native resolution.
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
