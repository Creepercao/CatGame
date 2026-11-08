using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CatGame.Idle;

namespace CatGame
{
    /// <summary>Continue/new-game menu with confirmation, persisted preferences, and quit.</summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        GameObject _settingsOverlay;
        GameObject _confirmOverlay;
        Button _continueButton;

        void Start()
        {
            EnsureEventSystem();
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
            var backdrop = UiKit.Panel(root, "MenuBackdrop", UiKit.Cream, false);
            backdrop.rectTransform.Stretch(); backdrop.raycastTarget = false;
            var artFrame = UiKit.Node("MenuIllustration", root);
            artFrame.anchorMin = Vector2.zero; artFrame.anchorMax = new Vector2(.65f,1);
            artFrame.offsetMin = Vector2.zero; artFrame.offsetMax = Vector2.zero;
            artFrame.gameObject.AddComponent<RectMask2D>();
            var art = UiKit.Node("CG", artFrame).Stretch().gameObject.AddComponent<Image>();
            art.sprite = Resources.Load<Sprite>("Art/Story/MenuMomo_v1");
            art.raycastTarget = false;
            art.rectTransform.pivot = new Vector2(.20f,.5f);
            var fit = art.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = art.sprite.rect.width / art.sprite.rect.height;
            var panel = UiKit.Panel(root, "Panel", new Color(1f, 1f, 1f, 0.86f));
            panel.rectTransform.anchorMin = new Vector2(.65f,.5f);
            panel.rectTransform.anchorMax = new Vector2(1,.5f);
            panel.rectTransform.pivot = new Vector2(.5f,.5f);
            panel.rectTransform.anchoredPosition = Vector2.zero;
            panel.rectTransform.sizeDelta = new Vector2(-48,680);

            var title = UiKit.Label(panel.transform, "Title", "喵 屋", 76, UiKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(500f, 100f));
            var sub = UiKit.Label(panel.transform, "Sub", "和猫待一会儿", 24, UiKit.InkSoft, TextAnchor.MiddleCenter);
            sub.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -124f), new Vector2(480f, 38f));

            _continueButton = AddButton(panel.transform, "Btn_Continue", "继续游戏", -208f, UiKit.Coral, UiKit.Cream, ContinueGame);
            AddButton(panel.transform, "Btn_NewGame", "新游戏", -310f, UiKit.Mint, UiKit.Cream, RequestNewGame);
            AddButton(panel.transform, "Btn_Settings", "设置", -412f, UiKit.PanelC, UiKit.Ink, OpenSettings);
            AddButton(panel.transform, "Btn_Quit", "退出", -514f, UiKit.PanelDark, UiKit.Ink, Quit);
            RefreshContinueButton();
        }

        Button AddButton(Transform parent, string name, string label, float y, Color bg, Color fg, System.Action click)
        {
            var button = UiKit.Btn(parent, name, label, new Vector2(360f, 76f), bg, fg, 28, click, 26f);
            button.GetComponent<RectTransform>().At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(360f, 76f));
            return button;
        }

        void RefreshContinueButton()
        {
            if (_continueButton != null)
                _continueButton.interactable = SaveSystem.Instance != null && SaveSystem.Instance.HasExistingSave;
        }

        public void ContinueGame()
        {
            RefreshContinueButton();
            if (!_continueButton.interactable) return;
            SceneFlow.GoHome();
        }

        void RequestNewGame()
        {
            var save = SaveSystem.Instance;
            if (save != null && save.HasExistingSave)
            {
                ShowNewGameConfirmation();
                return;
            }
            ConfirmNewGame();
        }

        public void ConfirmNewGame()
        {
            if (_confirmOverlay != null) Destroy(_confirmOverlay);
            _confirmOverlay = null;
            var save = SaveSystem.Instance;
            if (save == null)
            {
                var go = new GameObject("SaveSystem");
                save = go.AddComponent<SaveSystem>();
            }
            save.BeginNewGame();
            SceneFlow.GoHome();
        }

        void ShowNewGameConfirmation()
        {
            var root = (RectTransform)transform;
            var dim = UiKit.Panel(root, "NewGameConfirmation", new Color(0.16f, 0.12f, 0.10f, 0.62f), false);
            _confirmOverlay = dim.gameObject;
            dim.rectTransform.Stretch();
            var card = UiKit.Panel(dim.transform, "Card", UiKit.Cream);
            card.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 280f));
            var title = UiKit.Label(card.transform, "Heading", "开始新游戏？", 30, UiKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(500f, 52f));
            var note = UiKit.Label(card.transform, "Warning", "新进度会替换当前存档", 21, UiKit.InkSoft, TextAnchor.MiddleCenter);
            note.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(500f, 40f));
            var cancel = UiKit.Btn(card.transform, "Btn_Cancel", "返回", new Vector2(180f, 58f), UiKit.PanelDark, UiKit.Ink, 21, CancelNewGame);
            cancel.GetComponent<RectTransform>().At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-105f, 26f), new Vector2(180f, 58f));
            var confirm = UiKit.Btn(card.transform, "Btn_Confirm", "新建并覆盖", new Vector2(210f, 58f), UiKit.Coral, UiKit.Cream, 21, ConfirmNewGame);
            confirm.GetComponent<RectTransform>().At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(115f, 26f), new Vector2(210f, 58f));
        }

        void CancelNewGame()
        {
            if (_confirmOverlay != null) Destroy(_confirmOverlay);
            _confirmOverlay = null;
        }

        void OpenSettings()
        {
            if (_settingsOverlay != null) return;
            var dim = UiKit.Panel((RectTransform)transform, "SettingsOverlay", new Color(0.16f, 0.12f, 0.10f, 0.58f), false);
            _settingsOverlay = dim.gameObject;
            dim.rectTransform.Stretch();
            var card = UiKit.Panel(dim.transform, "Card", UiKit.Cream);
            card.rectTransform.At(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 520f));
            var title = UiKit.Label(card.transform, "Heading", "设置", 32, UiKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(540f, 56f));

            var format = UiKit.Btn(card.transform, "Btn_number_format", "", new Vector2(380f, 62f), UiKit.PanelC, UiKit.Ink, 20, CycleNumberFormat);
            format.GetComponent<RectTransform>().At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(380f, 62f));
            RefreshNumberFormat(format.GetComponentInChildren<Text>());
            var volume = UiKit.Node("VolumeSlider", card.transform).At(new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -224f), new Vector2(380f, 34f));
            var track = UiKit.Panel(volume, "Track", UiKit.PanelDark);
            track.rectTransform.Stretch();
            var fillRect = UiKit.Node("Fill", volume).Stretch();
            var fill = UiKit.Panel(fillRect, "FillGraphic", UiKit.Mint);
            fill.rectTransform.Stretch();
            var handle = UiKit.Panel(volume, "Handle", UiKit.Coral);
            handle.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            handle.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(26f, 0f);
            var slider = volume.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(PlayerPrefs.GetFloat("CatGame.Volume", AudioListener.volume));
            AudioListener.volume = slider.value;
            slider.onValueChanged.AddListener(GlobalSettings.SetVolume);
            GlobalSettingsControls.Attach(card.transform,format.GetComponentInChildren<Text>(),slider,new Vector2(.5f,1),new Vector2(0,-310));
            var volumeLabel = UiKit.Label(card.transform, "VolumeLabel", "音量", 20, UiKit.Ink, TextAnchor.MiddleLeft);
            volumeLabel.rectTransform.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-205f, -224f), new Vector2(72f, 36f));

            var close = UiKit.Btn(card.transform, "Btn_close", "完成", new Vector2(180f, 56f), UiKit.Mint, UiKit.Cream, 21, CloseSettings);
            close.GetComponent<RectTransform>().At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(180f, 56f));
        }

        void CycleNumberFormat()
        {
            GlobalSettings.CycleFormat();
            PlayerPrefs.Save();
            if (_settingsOverlay != null)
                RefreshNumberFormat(_settingsOverlay.transform.Find("Card/Btn_number_format/Label").GetComponent<Text>());
        }

        static void RefreshNumberFormat(Text label)
        {
            if (label == null) return;
            int mode = PlayerPrefs.GetInt("CatGame.NumberFormat", 0);
            label.text = GlobalSettings.FormatLabel;
        }

        void CloseSettings()
        {
            if (_settingsOverlay != null) Destroy(_settingsOverlay);
            _settingsOverlay = null;
            PlayerPrefs.Save();
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnDestroy()
        {
            if (_settingsOverlay != null) PlayerPrefs.Save();
        }

        static void EnsureEventSystem()
        {
            var es = Object.FindObjectOfType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                es = go.AddComponent<EventSystem>();
            }
            if (es.GetComponent<BaseInputModule>() == null)
                es.gameObject.AddComponent<StandaloneInputModule>();
        }
    }
}
