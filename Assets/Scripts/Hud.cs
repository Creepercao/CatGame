using System;
using System.Collections.Generic;
using CatGame.Idle;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    /// <summary>Player-facing cat room HUD: economy summary, stacked cats, and purchase rows.</summary>
    public sealed class Hud : MonoBehaviour
    {
        sealed class CatCard { public Button button; public Text text; public string id; }
        sealed class ItemCard { public Button button; public Text text; public string id; }
        Text _title, _metrics, _stackLabel;
        AchievementToast _toast;
        PausePanel _pausePanel;
        GameObject _offlinePanel;
        public ModalGate Modals { get; private set; }
        public IntroDialogue Intro { get; private set; }
        public StoryPlayer Stories { get; private set; }
        public RocketPurchase Rocket { get; private set; }
        Button _rocketButton;
        Text _rocketText;
        readonly List<Image> _stackPortraits = new List<Image>();
        readonly List<Text> _stackAmounts = new List<Text>();
        readonly List<CatCard> _catCards = new List<CatCard>();
        readonly List<ItemCard> _itemCards = new List<ItemCard>();
        float _refresh;

        IdleGameController Economy { get { return CatWorld.I != null ? CatWorld.I.Economy : null; } }
        CatRoster Roster { get { return CatWorld.I != null ? CatWorld.I.Roster : null; } }

        public void Build(string title)
        {
            Modals = gameObject.AddComponent<ModalGate>();
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            var root = (RectTransform)transform;
            Rocket=gameObject.AddComponent<RocketPurchase>();
            Rocket.Initialize(root,Economy,Modals);
            var sidebar = UiKit.Panel(root, "Sidebar", UiKit.Cream);
            sidebar.rectTransform.anchorMin = Vector2.zero;
            sidebar.rectTransform.anchorMax = new Vector2(0f, 1f);
            sidebar.rectTransform.pivot = Vector2.zero;
            sidebar.rectTransform.sizeDelta = new Vector2(460f, 0f);
            sidebar.rectTransform.anchoredPosition = Vector2.zero;
            var info = UiKit.Panel(root, "InfoPanel", new Color(1f, 1f, 1f, 0.90f));
            info.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(412f, 146f));
            _title = UiKit.Label(info.transform, "Title", title, 26, UiKit.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            _title.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -12f), new Vector2(380f, 36f));
            _metrics = UiKit.Label(info.transform, "Metrics", "每秒产量 0\n猫毛 0    点击 +10\n猫 1 只", 20, UiKit.Ink, TextAnchor.UpperLeft);
            _metrics.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f, -50f), new Vector2(380f, 88f));

            BuildStackPanel(root);
            BuildPurchasePanel(root);
            _toast = gameObject.AddComponent<AchievementToast>();
            _toast.Build(root);
            _pausePanel = gameObject.AddComponent<PausePanel>();
            _pausePanel.Initialize(root, Economy != null ? Economy.State : null, SceneFlow.GoMenu, Modals);
            var economy = Economy;
            if (economy != null)
            {
                economy.StateChanged += Refresh;
                economy.AchievementUnlocked += OnAchievementUnlocked;
            }
            Refresh();
            ShowOfflineEarningsIfNeeded(root);
            if (Economy != null && CatWorld.I != null && CatWorld.I.EnableStoryIntro)
            {
                Intro = gameObject.AddComponent<IntroDialogue>();
                Intro.Initialize(root,Economy.State,Modals);
            }
            if (Economy != null && CatWorld.I != null && CatWorld.I.EnableStoryEvents)
            {
                Stories=gameObject.AddComponent<StoryPlayer>();
                Stories.Initialize(root,Economy.State,Modals);
            }
        }

        void ShowOfflineEarningsIfNeeded(RectTransform root)
        {
            var save = SaveSystem.Instance;
            if (save == null || save.OfflineEarnings == null || save.OfflineEarnings.catHair <= 0) return;
            ShowOfflineSummary(save.OfflineEarnings, root);
        }

        public bool ShowOfflineSummary(OfflineEarningsResult result)
        {
            return ShowOfflineSummary(result, (RectTransform)transform);
        }

        bool ShowOfflineSummary(OfflineEarningsResult result, RectTransform root)
        {
            if (result == null || result.catHair <= 0 || _offlinePanel != null) return false;
            if (!Modals.TryAcquire(this)) return false;
            var dim = UiKit.Panel(root, "OfflineEarningsOverlay", new Color(0.16f, 0.12f, 0.10f, 0.54f), false);
            _offlinePanel = dim.gameObject;
            dim.rectTransform.Stretch();
            var card = UiKit.Panel(dim.transform, "Summary", UiKit.Cream);
            card.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 280f));
            var heading = UiKit.Label(card.transform, "Heading", "欢迎回来", 34, UiKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            heading.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(500f, 52f));
            var details = UiKit.Label(card.transform, "Details", "你离开的 " + FormatDuration(result.elapsedSeconds)
                + " 里，猫猫攒了 " + FormatNumber(result.catHair) + " 猫毛", 23, UiKit.Ink, TextAnchor.MiddleCenter);
            details.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 8f), new Vector2(500f, 78f));
            var close = UiKit.Btn(card.transform, "Btn_Collect", "收下猫毛", new Vector2(240f, 62f), UiKit.Coral, UiKit.Cream, 23, CloseOfflineSummary);
            close.GetComponent<RectTransform>().At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(240f, 62f));
            return true;
        }

        void CloseOfflineSummary()
        {
            if (_offlinePanel != null) Destroy(_offlinePanel);
            _offlinePanel = null;
            Modals.Release(this);
        }

        public AchievementToast Toast { get { return _toast; } }

        void OnAchievementUnlocked(AchievementDefinition achievement)
        {
            if (_toast != null) _toast.Enqueue(achievement);
        }

        void AddTopButton(RectTransform root, string name, string label, Vector2 pos, Action click, Color bg, Color fg)
        {
            var button = UiKit.Btn(root, name, label, new Vector2(140f, 56f), bg, fg, 22, click);
            button.GetComponent<RectTransform>().At(new Vector2(1f, 1f), new Vector2(1f, 1f), pos, new Vector2(140f, 56f));
        }

        void BuildStackPanel(RectTransform root)
        {
            var panel = UiKit.Panel(root, "CatStackPanel", new Color(1f, 1f, 1f, 0.88f));
            panel.rectTransform.At(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 16f), new Vector2(412f, 150f));
            _stackLabel = UiKit.Label(panel.transform, "StackCount", "", 19, UiKit.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            _stackLabel.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -10f), new Vector2(270f, 30f));
            for (int i = 0; i < 3; i++)
            {
                var avatar = UiKit.Node("StackBreed_" + i, panel.transform).At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f + i * 84f, -50f), new Vector2(68f, 68f));
                var image = avatar.gameObject.AddComponent<Image>();
                image.preserveAspect = true;
                _stackPortraits.Add(image);
                var amount = UiKit.Label(panel.transform, "StackAmount_" + i, "", 15, UiKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                amount.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(18f + i * 84f, -122f), new Vector2(68f, 26f));
                _stackAmounts.Add(amount);
            }
        }

        void BuildPurchasePanel(RectTransform root)
        {
            var panel = UiKit.Panel(root, "PurchasePanel", new Color(1f, 1f, 1f, .94f));
            panel.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -184f), new Vector2(412f, 710f));
            var viewport = UiKit.Node("ShopViewport", panel.transform).At(new Vector2(0,1),new Vector2(0,1),new Vector2(18,-72),new Vector2(376,624));
            viewport.gameObject.AddComponent<Image>().color = new Color(1,1,1,.01f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;
            var content = UiKit.Node("Content", viewport);
            content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8; layout.padding = new RectOffset(2,6,12,12);
            layout.childControlWidth = false; layout.childControlHeight = false;
            layout.childForceExpandWidth = false; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ShopScrollRect>();
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40;
            var catHeading = ShopHeading(content, "CatHeading", "猫咪伙伴");
            foreach (var definition in CatCatalog.All) _catCards.Add(CreateCatCard(content, definition));
            var divider = UiKit.Panel(content, "Divider", UiKit.PanelDark, false);
            divider.rectTransform.sizeDelta = new Vector2(360,2);
            var itemHeading = ShopHeading(content, "ItemHeading", "猫窝用品");
            foreach (var definition in ItemCatalog.All) _itemCards.Add(CreateItemCard(content, definition));
            var rocketHeading=ShopHeading(content,"RocketHeading","星星之约");
            _rocketButton=UiKit.Btn(content,"Rocket","",new Vector2(360,120),UiKit.PanelC,UiKit.Ink,16,()=>Rocket.Open());
            var rocketIcon=UiKit.Node("Icon",_rocketButton.transform).At(new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(12,0),new Vector2(64,64)).gameObject.AddComponent<Image>();
            rocketIcon.sprite=Resources.Load<Sprite>("Art/Items/Pixel/rocket"); rocketIcon.preserveAspect=true; rocketIcon.raycastTarget=false;
            _rocketText=UiKit.Label(_rocketButton.transform,"Info","",18,UiKit.Ink,TextAnchor.MiddleLeft,FontStyle.Bold);
            _rocketText.rectTransform.At(new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(82,0),new Vector2(268,110));
            var cats = UiKit.Btn(panel.transform,"Btn_shop_cats","伙伴",new Vector2(112,44),UiKit.PanelC,UiKit.Ink,20,()=>scroll.Focus(catHeading));
            cats.GetComponent<RectTransform>().At(new Vector2(0,1),new Vector2(0,1),new Vector2(20,-14),new Vector2(112,44));
            var items = UiKit.Btn(panel.transform,"Btn_shop_items","用品",new Vector2(112,44),UiKit.PanelC,UiKit.Ink,20,()=>scroll.Focus(itemHeading));
            items.GetComponent<RectTransform>().At(new Vector2(0,1),new Vector2(0,1),new Vector2(150,-14),new Vector2(112,44));
            var rocket = UiKit.Btn(panel.transform,"Btn_shop_rocket","火箭",new Vector2(112,44),UiKit.PanelC,UiKit.Ink,20,()=>scroll.Focus(rocketHeading));
            rocket.GetComponent<RectTransform>().At(new Vector2(0,1),new Vector2(0,1),new Vector2(280,-14),new Vector2(112,44));
        }

        static RectTransform ShopHeading(Transform content,string name,string text)
        {
            var heading = UiKit.Label(content,name,text,20,UiKit.Ink,TextAnchor.MiddleLeft,FontStyle.Bold);
            heading.rectTransform.sizeDelta = new Vector2(360,32);
            return heading.rectTransform;
        }

        CatCard CreateCatCard(Transform parent, CatDefinition definition)
        {
            var button = UiKit.Btn(parent, "Cat_" + definition.id, "", new Vector2(360f, 104f), UiKit.PanelC, UiKit.Ink, 16,
                () => { var e = Economy; if (e != null) e.BuyCat(definition.id); });
            var portrait = UiKit.Node("Portrait", button.transform).At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(9f, 0f), new Vector2(64f, 64f)).gameObject.AddComponent<Image>();
            portrait.sprite = CatVisualLibrary.Load(definition).Portrait;
            portrait.preserveAspect = true;
            var label = UiKit.Label(button.transform, "Info", "", 15, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(272f, 92f));
            return new CatCard { button = button, text = label, id = definition.id };
        }

        ItemCard CreateItemCard(Transform parent, ItemDefinition definition)
        {
            var button = UiKit.Btn(parent, "Item_" + definition.id, "", new Vector2(360f, 104f), UiKit.PanelC, UiKit.Ink, 16,
                () => { var e = Economy; if (e != null) e.BuyItem(definition.id); });
            var iconNode = UiKit.Node("Icon", button.transform);
            iconNode.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(64f, 64f));
            var icon = iconNode.gameObject.AddComponent<Image>();
            icon.sprite = ItemVisualLibrary.Load(definition.id);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var label = UiKit.Label(button.transform, "Info", "", 15, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.At(new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(82f, 0f), new Vector2(268f, 92f));
            return new ItemCard { button = button, text = label, id = definition.id };
        }

        void Update()
        {
            _refresh += Time.unscaledDeltaTime;
            if (_refresh >= 0.5f) { _refresh = 0f; Refresh(); }
        }

        void Refresh()
        {
            var economy = Economy;
            if (economy == null || economy.State == null) return;
            var state = economy.State;
            if (_metrics != null)
                _metrics.text = "每秒产量  " + FormatNumber(economy.ProductionPerSecond) + "\n猫毛  " + FormatNumber(state.catHair) + "    点击 +" + FormatNumber(economy.CurrentClickValue) + "\n猫  " + economy.OwnedCats + " 只";
            if (_title != null) _title.text = "猫窝";
            RefreshStack();
            foreach (var card in _catCards) RefreshCatCard(card, state);
            foreach (var card in _itemCards) RefreshItemCard(card, state);
            if (_rocketButton != null)
            {
                _rocketButton.interactable=!state.rocketPurchased && state.catHair>=EndingRules.RocketPrice;
                _rocketText.text=state.rocketPurchased ? "火箭 · 已出发\n感谢你和猫猫的陪伴" : "火箭 · 一次性\n价格 "+FormatNumber(EndingRules.RocketPrice)+"\n购买后进入结局";
            }
        }

        public void RefreshDisplay() { Refresh(); }

        void RefreshStack()
        {
            var roster = Roster;
            if (roster == null) return;
            if (_stackLabel != null) _stackLabel.transform.parent.gameObject.SetActive(roster.StackedCount > 0);
            if (_stackLabel != null) _stackLabel.text = roster.StackedCount > 0 ? "堆叠猫咪  +" + roster.StackedCount + " 只" : "";
            var stacked = new List<CatDefinition>();
            foreach (var definition in CatCatalog.All)
                if (roster.OwnedCountFor(definition.id) > roster.VisibleCountFor(definition.id)) stacked.Add(definition);
            stacked.Sort((a, b) => (roster.OwnedCountFor(b.id) - roster.VisibleCountFor(b.id)).CompareTo(roster.OwnedCountFor(a.id) - roster.VisibleCountFor(a.id)));
            for (int i = 0; i < _stackPortraits.Count; i++)
            {
                bool active = i < stacked.Count;
                _stackPortraits[i].gameObject.SetActive(active);
                _stackAmounts[i].gameObject.SetActive(active);
                if (!active) continue;
                var definition = stacked[i];
                _stackPortraits[i].sprite = CatVisualLibrary.Load(definition).Portrait;
                _stackAmounts[i].text = "+" + (roster.OwnedCountFor(definition.id) - roster.VisibleCountFor(definition.id));
            }
        }

        void RefreshCatCard(CatCard card, GameState state)
        {
            var definition = FindCatDefinition(card.id);
            int owned = FindCatCount(state, card.id);
            double price = IdleMath.CatPrice(card.id, owned);
            card.button.interactable = owned < definition.maxOwned && price <= state.catHair;
            string status = owned >= definition.maxOwned ? "已住满 40/40" : "价格 " + FormatNumber(price);
            card.text.text = definition.name + "  " + owned + "/40\n" + status + "\n" + FormatNumber(definition.income) + "/秒";
        }

        void RefreshItemCard(ItemCard card, GameState state)
        {
            var definition = FindItemDefinition(card.id);
            int level = FindItemLevel(state, card.id);
            double price = IdleMath.ItemPrice(card.id, level);
            card.button.interactable = level < ItemCatalog.LevelsPerItem && price <= state.catHair;
            string status = level >= ItemCatalog.LevelsPerItem ? "已满级" : "价格 " + FormatNumber(price);
            card.text.text = definition.name + "  Lv." + level + "/5\n" + status + "\n" + (definition.click ? "点击加成" : "产量加成");
        }

        void OnDestroy()
        {
            if (Modals != null) Modals.Release(this);
            var economy = Economy;
            if (economy != null)
            {
                economy.StateChanged -= Refresh;
                economy.AchievementUnlocked -= OnAchievementUnlocked;
            }
        }

        static int FindCatCount(GameState state, string id) { foreach (var row in state.cats) if (row.id == id) return row.count; return 0; }
        static int FindItemLevel(GameState state, string id) { foreach (var row in state.items) if (row.id == id) return row.level; return 0; }
        static CatDefinition FindCatDefinition(string id) { foreach (var row in CatCatalog.All) if (row.id == id) return row; return null; }
        static ItemDefinition FindItemDefinition(string id) { foreach (var row in ItemCatalog.All) if (row.id == id) return row; return null; }
        static string ToRoman(int stage) { return stage == 1 ? "I" : stage == 2 ? "II" : stage == 3 ? "III" : stage == 4 ? "IV" : "V"; }

        static string FormatDuration(double seconds)
        {
            int whole = Mathf.Max(0, (int)seconds);
            int hours = whole / 3600;
            int minutes = (whole % 3600) / 60;
            return hours > 0 ? hours + " 小时 " + minutes + " 分钟" : minutes + " 分钟";
        }

        public static string FormatNumber(double value)
        {
            if (double.IsNaN(value)) return "—";
            if (double.IsInfinity(value)) return "∞";
            int mode = GlobalSettings.NumberFormat;
            double magnitude = Math.Abs(value);
            if (mode == 1 && magnitude >= 1000000000.0) return (value / 1000000000.0).ToString("0.##") + "B";
            if (mode == 1 && magnitude >= 1000000.0) return (value / 1000000.0).ToString("0.##") + "M";
            if (mode == 1 && magnitude >= 1000.0) return (value / 1000.0).ToString("0.##") + "K";
            if (magnitude >= 100000000.0) return value.ToString("0.##e+0");
            if (magnitude >= 1000.0) return value.ToString("#,##0.##");
            return value.ToString("0.##");
        }
    }
}
