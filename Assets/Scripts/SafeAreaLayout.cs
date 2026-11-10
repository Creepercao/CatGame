using UnityEngine;

namespace CatGame
{
    /// <summary>Keeps the interactive canvas inside system insets as the display changes.</summary>
    internal sealed class SafeAreaLayout : MonoBehaviour
    {
        Rect _lastBounds;

        void OnEnable() { Update(); }

        void Update()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            var area = Screen.safeArea;
            if (area.width <= 0 || area.height <= 0) return;
            var bounds = Rect.MinMaxRect(area.xMin / Screen.width, area.yMin / Screen.height,
                area.xMax / Screen.width, area.yMax / Screen.height);
            if (bounds == _lastBounds) return;
            _lastBounds = bounds;
            var layout = (RectTransform)transform;
            layout.anchorMin = bounds.min;
            layout.anchorMax = bounds.max;
            layout.offsetMin = Vector2.zero;
            layout.offsetMax = Vector2.zero;
        }
    }
}
