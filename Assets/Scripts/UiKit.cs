using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    /// <summary>纯代码构建 UI 的小工具集：动态中文字体、圆角九宫格、面板/文字/按钮/进度条。</summary>
    public static class UiKit
    {
        // 配色（取自素材调性：奶油 + 粉棕 + 珊瑚）
        public static readonly Color Cream = new Color32(0xFD, 0xF6, 0xEC, 0xFF);
        public static readonly Color PanelC = new Color32(0xF7, 0xE8, 0xD6, 0xFF);
        public static readonly Color PanelDark = new Color32(0xEF, 0xDC, 0xC6, 0xFF);
        public static readonly Color Ink = new Color32(0x5B, 0x46, 0x36, 0xFF);
        public static readonly Color InkSoft = new Color32(0x9A, 0x84, 0x70, 0xFF);
        public static readonly Color Coral = new Color32(0xE8, 0x8A, 0x6A, 0xFF);
        public static readonly Color Mint = new Color32(0x8F, 0xC6, 0xAE, 0xFF);
        public static readonly Color Sky = new Color32(0x9F, 0xC3, 0xE0, 0xFF);
        public static readonly Color Gold = new Color32(0xF2, 0xC0, 0x5C, 0xFF);

        static UnityEngine.Font _font;

        /// <summary>系统中文字体（动态字体，随字号实时渲染）。</summary>
        public static UnityEngine.Font UIFont
        {
            get
            {
                if (_font == null)
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    _font = Resources.Load<Font>("Fonts/NotoSansSC-Regular");
#endif
                    if (_font == null)
                    _font = UnityEngine.Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "Microsoft YaHei UI", "PingFang SC", "SimHei", "SimSun", "Arial" }, 32);
                    if (_font == null) _font = Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
                }
                return _font;
            }
        }

        internal static RectTransform ContentRoot(RectTransform canvasRoot)
        {
            if (!Application.isMobilePlatform) return canvasRoot;
            var content = Node("SafeArea", canvasRoot).Stretch();
            content.gameObject.AddComponent<SafeAreaLayout>();
            return content;
        }

        static Sprite _roundSoft, _roundPill, _circle;

        public static Sprite RoundSoft
        {
            get { if (_roundSoft == null) _roundSoft = BuildRounded(64, 20, 22); return _roundSoft; }
        }

        public static Sprite RoundPill
        {
            get { if (_roundPill == null) _roundPill = BuildRounded(64, 31, 31); return _roundPill; }
        }

        public static Sprite Circle
        {
            get { if (_circle == null) _circle = BuildRounded(64, 32, 31); return _circle; }
        }

        static Sprite BuildRounded(int size, int radius, int border)
        {
            string name = radius == 20 ? "panel_soft" : radius == 31 ? "panel_pill" : "circle";
            var sprite = Resources.Load<Sprite>("Art/UI/Pixel/" + name);
            if (sprite == null) throw new System.InvalidOperationException("Missing pixel UI mask: " + name);
            return sprite;
        }

        // ---------- 构建原语 ----------

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform At(this RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(this RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        public static Image Panel(Transform parent, string name, Color color, bool rounded = true, float corner = 20f)
        {
            var rt = Node(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (rounded)
            {
                img.sprite = corner > 28f ? RoundPill : RoundSoft;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int fontSize, Color color,
            TextAnchor anchor = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = Node(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = UIFont;
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Button Btn(Transform parent, string name, string label, Vector2 size, Color bg, Color fg,
            int fontSize, System.Action onClick, float corner = 20f)
        {
            var img = Panel(parent, name, bg, true, corner);
            img.rectTransform.sizeDelta = size;
            img.raycastTarget = true;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.78f, 0.78f, 0.78f, 0.65f);
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            var t = Label(img.transform, "Label", label, fontSize, fg, TextAnchor.MiddleCenter);
            t.rectTransform.Stretch();
            return btn;
        }

        // ---------- 进度条 ----------

        public class Bar
        {
            public RectTransform Root;
            public Image Fill;
            public Text Value;
            public Color Tint;
            public float Shown = 1f;

            public void Set(float ratio01)
            {
                Shown = Mathf.Clamp01(ratio01);
                if (Value != null) Value.text = Mathf.RoundToInt(Shown * 100f).ToString();
                Fill.color = Shown < 0.22f
                    ? Color.Lerp(Tint, new Color32(0xD9, 0x54, 0x45, 0xFF), 0.7f)
                    : Tint;
            }

            public void Tween(float dt)
            {
                var cur = Fill.rectTransform.anchorMax.x;
                if (Mathf.Abs(cur - Shown) < 0.0008f) return;
                float next = Mathf.Lerp(cur, Shown, 1f - Mathf.Exp(-11f * dt));
                if (Mathf.Abs(next - Shown) < 0.002f) next = Shown;
                Fill.rectTransform.anchorMax = new Vector2(next, 1f);
            }
        }

        /// <summary>横向圆角进度条：背景 + 填充 + 左侧说明 + 右侧数值。</summary>
        public static Bar MakeBar(Transform parent, string name, Color tint, Vector2 pos, Vector2 size,
            string caption, int captionSize = 20)
        {
            var root = Node(name, parent).At(new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);

            var bg = Panel(root, "Bg", new Color(0f, 0f, 0f, 0.10f), true, size.y * 0.5f);
            bg.rectTransform.Stretch();
            bg.raycastTarget = false;

            var clip = Node("Clip", root);
            clip.Stretch(3f);
            clip.pivot = new Vector2(0f, 0.5f);

            var fillRt = Node("Fill", clip);
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillRt.pivot = new Vector2(0f, 0.5f);
            var fill = fillRt.gameObject.AddComponent<Image>();
            fill.sprite = RoundPill;
            fill.type = Image.Type.Sliced;
            fill.color = tint;
            fill.raycastTarget = false;

            var bar = new Bar { Root = root, Fill = fill, Tint = tint };

            if (!string.IsNullOrEmpty(caption))
            {
                var cap = Label(root, "Caption", caption, captionSize, new Color(1f, 1f, 1f, 0.97f), TextAnchor.MiddleLeft, FontStyle.Bold);
                cap.rectTransform.Stretch();
                cap.rectTransform.offsetMin = new Vector2(12f, 0f);
                cap.rectTransform.offsetMax = new Vector2(-12f, 0f);
                AddOutline(cap.gameObject, new Color(0f, 0f, 0f, 0.45f), 1.1f);
            }

            var v = Label(root, "Value", "0", captionSize, new Color(1f, 1f, 1f, 0.97f), TextAnchor.MiddleRight, FontStyle.Bold);
            v.rectTransform.Stretch();
            v.rectTransform.offsetMin = new Vector2(12f, 0f);
            v.rectTransform.offsetMax = new Vector2(-12f, 0f);
            AddOutline(v.gameObject, new Color(0.22f, 0.17f, 0.12f, 0.85f), 1.4f);
            bar.Value = v;

            return bar;
        }

        /// <summary>给文字加深色描边，保证在浅色/彩色底上都读得清。</summary>
        public static void AddOutline(GameObject go, Color color, float dist = 1.3f)
        {
            var o = go.AddComponent<Outline>();
            o.effectColor = color;
            o.effectDistance = new Vector2(dist, -dist);
            o.useGraphicAlpha = true;
        }

        public static void AddShadow(GameObject go, float alpha = 0.38f, float dist = 1.5f)
        {
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, alpha);
            sh.effectDistance = new Vector2(dist, -dist);
        }
    }
}
