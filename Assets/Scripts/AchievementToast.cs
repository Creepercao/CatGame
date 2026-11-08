using System.Collections.Generic;
using CatGame.Idle;
using UnityEngine;
using UnityEngine.UI;

namespace CatGame
{
    /// <summary>Queued Steam-style achievement notifications shown in the upper-right.</summary>
    public sealed class AchievementToast : MonoBehaviour
    {
        readonly Queue<AchievementDefinition> _queue = new Queue<AchievementDefinition>();
        RectTransform _root;
        CanvasGroup _opacity;
        Text _title, _condition;
        AchievementDefinition _current;
        float _elapsed;
        int _phase;

        public string CurrentMessage { get { return _current != null ? _current.name : null; } }
        public int PendingCount { get { return _queue.Count; } }

        public void Build(Transform parent)
        {
            var panel = UiKit.Panel(parent, "AchievementToast", new Color(1f, 1f, 1f, 0.96f));
            _root = panel.rectTransform;
            _opacity = panel.gameObject.AddComponent<CanvasGroup>();
            _opacity.blocksRaycasts = false;
            _root.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(420f, -30f), new Vector2(380f, 112f));
            _title = UiKit.Label(_root, "Name", "", 23, UiKit.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _title.rectTransform.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -12f), new Vector2(310f, 42f));
            _condition = UiKit.Label(_root, "Condition", "已解锁", 17, UiKit.InkSoft, TextAnchor.MiddleLeft);
            _condition.rectTransform.At(new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 15f), new Vector2(310f, 34f));
            var icon = UiKit.Label(_root, "Icon", "★", 25, UiKit.Gold, TextAnchor.MiddleCenter);
            icon.rectTransform.At(new Vector2(0f, .5f), new Vector2(0f, .5f), new Vector2(5f, 0f), new Vector2(40f, 44f));
            _root.gameObject.SetActive(false);
        }

        public void Enqueue(AchievementDefinition achievement)
        {
            if (achievement != null) _queue.Enqueue(achievement);
        }

        void Update()
        {
            if (_root == null) return;
            if (_current == null)
            {
                if (_queue.Count == 0) return;
                _current = _queue.Dequeue();
                _title.text = _current.name;
                _condition.text = "已解锁 · " + _current.condition;
                _elapsed = 0f;
                _phase = 0;
                _opacity.alpha = 1f;
                _root.gameObject.SetActive(true);
            }

            _elapsed += Time.unscaledDeltaTime;
            if (_phase == 0)
            {
                float t = Mathf.Clamp01(_elapsed / 0.28f);
                _root.anchoredPosition = new Vector2(Mathf.Lerp(420f, -24f, t), -30f);
                if (t >= 1f) { _phase = 1; _elapsed = 0f; }
            }
            else if (_phase == 1 && _elapsed >= 3f)
            {
                _phase = 2;
                _elapsed = 0f;
            }
            else if (_phase == 2)
            {
                float t = Mathf.Clamp01(_elapsed / 0.28f);
                _root.anchoredPosition = new Vector2(Mathf.Lerp(-24f, 420f, t), -30f);
                _opacity.alpha = 1f - t;
                if (t >= 1f)
                {
                    _root.gameObject.SetActive(false);
                    _current = null;
                }
            }
        }
    }
}
