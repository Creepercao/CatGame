using System;
using CatGame.Idle;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    /// <summary>Pause overlay with settings, cat catalog, achievements, resume and main-menu actions.</summary>
    public sealed class PausePanel : MonoBehaviour
    {
        const string NumberFormatKey = "CatGame.NumberFormat";

        RectTransform _root;
        GameState _state;
        Action _returnToMenu;
        GameObject _overlay;
        ModalGate _gate;
        Text _numberFormatValue;
        Slider _volumeSlider;
        bool _volumeDirty;

        public bool IsOpen { get { return _overlay != null; } }
        public int CatalogEntryCount { get; private set; }
        public int AchievementEntryCount { get; private set; }

        public void Initialize(RectTransform root, GameState state, Action returnToMenu, ModalGate gate = null)
        {
            _gate = gate != null ? gate : root.GetComponent<ModalGate>();
            if (_gate == null) _gate = root.gameObject.AddComponent<ModalGate>();
            _root = root;
            _state = state;
            _returnToMenu = returnToMenu;
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("CatGame.Volume", AudioListener.volume));
            var button = UiKit.Btn(root, "Btn_pause", "设置", new Vector2(64f, 56f), UiKit.PanelC, UiKit.Ink, 18, () => Open());
            button.GetComponent<RectTransform>().At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(364f, -24f), new Vector2(64f, 56f));
        }

        void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (IsOpen) Close();
            else if (Application.isMobilePlatform) Open();
        }

        public bool Open()
        {
            if (IsOpen || _root == null || Time.timeScale <= 0f) return false;
            if (!_gate.TryAcquire(this)) return false;

            var dim = UiKit.Panel(_root, "PauseOverlay", new Color(0.16f, 0.12f, 0.10f, 0.58f), false);
            _overlay = dim.gameObject;
            dim.rectTransform.Stretch();
            BuildWindow(dim.transform);
            ShowPage("Settings");
            return true;
        }

        void BuildWindow(Transform parent)
        {
            var window = UiKit.Panel(parent, "Window", UiKit.Cream);
            window.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 650f));
            var heading = UiKit.Label(window.transform, "Heading", "猫窝暂停中", 32, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            heading.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(510f, 52f));
            var close = UiKit.Btn(window.transform, "Btn_resume", "继续游戏", new Vector2(142f, 52f), UiKit.Mint, UiKit.Cream, 20, Close);
            close.GetComponent<RectTransform>().At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -18f), new Vector2(142f, 52f));

            var nav = UiKit.Node("Navigation", window.transform).At(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(190f, 540f));
            AddNav(nav, "Btn_page_Settings", "设置", "Settings", -22f);
            AddNav(nav, "Btn_page_Catalog", "猫咪图鉴", "Catalog", -90f);
            AddNav(nav, "Btn_page_Achievements", "成就", "Achievements", -158f);
            var menu = UiKit.Btn(nav, "Btn_main_menu", "回主菜单", new Vector2(164f, 54f), UiKit.Coral, UiKit.Cream, 20, ReturnToMenu);
            menu.GetComponent<RectTransform>().At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -420f), new Vector2(164f, 54f));

            var content = UiKit.Node("PageContent", window.transform).At(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(226f, 26f), new Vector2(660f, 540f));
            BuildSettings(content);
            BuildCatalog(content);
            BuildAchievements(content);
        }

        void AddNav(RectTransform nav, string name, string label, string page, float y)
        {
            var button = UiKit.Btn(nav, name, label, new Vector2(164f, 54f), UiKit.PanelC, UiKit.Ink, 20,
                () => ShowPage(page));
            button.GetComponent<RectTransform>().At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, y), new Vector2(164f, 54f));
        }

        void BuildSettings(Transform parent)
        {
            var page = CreatePage(parent, "Settings");
            AddHeading(page.transform, "设置", "SettingsHeading");
            var formatLabel = UiKit.Label(page.transform, "NumberFormatLabel", "数字显示格式", 22, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            formatLabel.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -106f), new Vector2(260f, 42f));
            var formatButton = UiKit.Btn(page.transform, "Btn_number_format", "", new Vector2(420f, 54f), UiKit.PanelC, UiKit.Ink, 19, CycleNumberFormat);
            formatButton.GetComponent<RectTransform>().At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -166f), new Vector2(420f, 54f));
            _numberFormatValue = formatButton.GetComponentInChildren<Text>();
            RefreshNumberFormatLabel();

            var volumeLabel = UiKit.Label(page.transform, "VolumeLabel", "音量", 22, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            volumeLabel.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -252f), new Vector2(260f, 42f));
            var sliderRoot = UiKit.Node("VolumeSlider", page.transform).At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -310f), new Vector2(420f, 36f));
            var background = UiKit.Panel(sliderRoot, "Background", UiKit.PanelDark);
            background.rectTransform.Stretch();
            var fillArea = UiKit.Node("Fill Area", sliderRoot).Stretch(6f);
            var fill = UiKit.Panel(fillArea, "Fill", UiKit.Mint);
            fill.rectTransform.Stretch();
            var handle = UiKit.Panel(sliderRoot, "Handle", UiKit.Coral);
            handle.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(28f, 0f);
            _volumeSlider = sliderRoot.gameObject.AddComponent<Slider>();
            _volumeSlider.fillRect = fill.rectTransform;
            _volumeSlider.handleRect = handle.rectTransform;
            _volumeSlider.targetGraphic = handle;
            _volumeSlider.minValue = 0f;
            _volumeSlider.maxValue = 1f;
            _volumeSlider.value = Mathf.Clamp01(PlayerPrefs.GetFloat("CatGame.Volume", AudioListener.volume));
            AudioListener.volume = _volumeSlider.value;
            _volumeSlider.onValueChanged.AddListener(value =>
            {
                GlobalSettings.SetVolume(value);
                _volumeDirty = true;
            });
            var hint = UiKit.Label(page.transform, "VolumeHint", "当前版本暂无音效", 16, UiKit.InkSoft, TextAnchor.MiddleLeft);
            hint.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -348f), new Vector2(300f, 30f));

            GlobalSettingsControls.Attach(page.transform,_numberFormatValue,_volumeSlider,new Vector2(0,1),new Vector2(20,-390));
            var exit = UiKit.Btn(page.transform, "Btn_exit", "退出游戏", new Vector2(180f, 54f), UiKit.PanelDark, UiKit.Ink, 19, Application.Quit);
            exit.GetComponent<RectTransform>().At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -480f), new Vector2(180f, 54f));
        }

        void BuildCatalog(Transform parent)
        {
            var page = CreatePage(parent, "Catalog", Resources.Load<Sprite>("Art/Backgrounds/Pixel/CatalogPark"));
            AddHeading(page.transform, "猫咪图鉴", "CatalogHeading");
            var viewport = UiKit.Node("Entries", page.transform).At(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 10f), new Vector2(610f, 468f));
            var maskImage = viewport.gameObject.AddComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = UiKit.Node("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            CatalogEntryCount = 0;
            foreach (var cat in CatCatalog.All)
            {
                int owned = CountCats(cat.id);
                bool unlocked = owned > 0;
                string status = unlocked ? "已解锁" : "可购买 · 首价 " + Hud.FormatNumber(cat.firstPrice);
                var row = UiKit.Panel(content, "CatEntry_" + cat.id, unlocked ? UiKit.PanelC : UiKit.PanelDark);
                row.rectTransform.sizeDelta = new Vector2(600f, 70f);
                var portrait = UiKit.Node("Portrait", row.transform).At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(58f, 58f)).gameObject.AddComponent<Image>();
                portrait.sprite = CatVisualLibrary.Load(cat).Portrait;
                portrait.preserveAspect = true;
                if (!unlocked) portrait.color = new Color(0.35f, 0.35f, 0.35f, 0.75f);
                var text = UiKit.Label(row.transform, "Details", cat.name + "\n" + status + " · 拥有 " + owned + "/" + cat.maxOwned + " · " + Hud.FormatNumber(cat.income) + "/秒", 15, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                text.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(500f, 60f));
                CatalogEntryCount++;
            }
        }

        void BuildAchievements(Transform parent)
        {
            var page = CreatePage(parent, "Achievements");
            AddHeading(page.transform, "成就", "AchievementsHeading");
            var viewport = UiKit.Node("Entries", page.transform).At(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(14f, 10f), new Vector2(610f, 468f));
            var maskImage = viewport.gameObject.AddComponent<Image>();
            maskImage.color = new Color(1f, 1f, 1f, 0.02f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = UiKit.Node("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.padding = new RectOffset(4, 4, 4, 4);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            AchievementEntryCount = 0;
            foreach (var achievement in AchievementCatalog.All)
            {
                bool unlocked = _state != null && _state.achievements != null && _state.achievements.Contains(achievement.id);
                var row = UiKit.Panel(content, "Achievement_" + achievement.id, unlocked ? UiKit.PanelC : UiKit.PanelDark);
                row.rectTransform.sizeDelta = new Vector2(600f, 84f);
                var label = UiKit.Label(row.transform, "Details", (unlocked ? "★  " : "◇  ") + achievement.name + "\n" + achievement.condition, 15, unlocked ? UiKit.Ink : UiKit.InkSoft, TextAnchor.MiddleLeft, unlocked ? FontStyle.Bold : FontStyle.Normal);
                label.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(570f, 78f));
                AchievementEntryCount++;
            }
        }

        static RectTransform CreatePage(Transform parent, string name, Sprite pageBackground = null)
        {
            var page = UiKit.Node(name, parent).At(Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
            page.Stretch();
            var background = UiKit.Panel(page, "Background", pageBackground != null ? Color.white : new Color(1f, 1f, 1f, 0.52f));
            background.rectTransform.Stretch();
            if (pageBackground != null)
            {
                background.sprite = pageBackground;
                background.type = Image.Type.Simple;
                background.raycastTarget = false;
            }
            return page;
        }

        static void AddHeading(Transform parent, string title, string name)
        {
            var heading = UiKit.Label(parent, name, title, 26, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            heading.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -16f), new Vector2(500f, 48f));
        }

        void ShowPage(string name)
        {
            if (_overlay == null) return;
            var pages = _overlay.transform.Find("Window/PageContent");
            if (pages == null) return;
            for (int i = 0; i < pages.childCount; i++)
            {
                var child = pages.GetChild(i);
                if (child.name == name) child.gameObject.SetActive(true);
                else if (child.name == "Settings" || child.name == "Catalog" || child.name == "Achievements") child.gameObject.SetActive(false);
            }
        }

        void CycleNumberFormat()
        {
            int next = (PlayerPrefs.GetInt(NumberFormatKey, 0) + 1) % 3;
            GlobalSettings.CycleFormat();
            PlayerPrefs.Save();
            RefreshNumberFormatLabel();
            if (CatWorld.I != null && CatWorld.I.Hud != null) CatWorld.I.Hud.RefreshDisplay();
        }

        void RefreshNumberFormatLabel()
        {
            if (_numberFormatValue == null) return;
            int mode = PlayerPrefs.GetInt(NumberFormatKey, 0);
            _numberFormatValue.text = GlobalSettings.FormatLabel;
        }

        void Close()
        {
            if (_overlay == null) return;
            Destroy(_overlay);
            _overlay = null;
            if (_volumeDirty)
            {
                PlayerPrefs.Save();
                _volumeDirty = false;
            }
            _gate.Release(this);
        }

        void ReturnToMenu()
        {
            var callback = _returnToMenu;
            Close();
            if (callback != null) callback();
        }

        int CountCats(string id)
        {
            if (_state == null || _state.cats == null) return 0;
            foreach (var row in _state.cats) if (row != null && row.id == id) return row.count;
            return 0;
        }

        static string Roman(int stage) { return stage == 1 ? "I" : stage == 2 ? "II" : stage == 3 ? "III" : stage == 4 ? "IV" : "V"; }

        void OnDestroy()
        {
            if (_volumeDirty) PlayerPrefs.Save();
            if (_gate != null) _gate.Release(this);
        }
    }
}

