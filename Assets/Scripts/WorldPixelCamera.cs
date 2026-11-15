using UnityEngine;
using UnityEngine.U2D;

namespace CatGame
{
    /// <summary>Built-in pipeline world raster. Overlay UI keeps the native display resolution.</summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class WorldPixelCamera : MonoBehaviour
    {
        Camera _camera;
        int _width, _height;
        bool _rendering;
        Rect _originalRect;
        float _originalHalfHeight;
        bool _originalForceTexture;
        public Vector2Int LogicalRenderSize { get; private set; }
        public bool IntegerUpscale { get; private set; }
        public float ConfiguredHalfHeight { get; private set; }
        public Vector2Int LastRenderedTextureSize { get; private set; }
        public int RenderCount { get; private set; }

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _originalRect = _camera.rect;
            _originalHalfHeight = _camera.orthographicSize;
            _originalForceTexture = _camera.forceIntoRenderTexture;
        }

        public void Fit(int width, int height)
        {
            if (_camera == null) Awake();
            var rules = WorldArtSettings.Current;
            rules.Validate();
            _width = width;
            _height = height;
            ConfiguredHalfHeight = rules.HalfHeight(width, height);
            LogicalRenderSize = rules.RenderSize(width, height);
            IntegerUpscale = width % LogicalRenderSize.x == 0 && height % LogicalRenderSize.y == 0 &&
                width / LogicalRenderSize.x == height / LogicalRenderSize.y;
            RestoreDisplayViewport();
        }

        void LateUpdate()
        {
            if (Screen.width <= 0 || Screen.height <= 0) return;
            if (_width != Screen.width || _height != Screen.height) Fit(Screen.width, Screen.height);
            // Unity's built-in PixelPerfectCamera uses the same offscreen rendering lifecycle.
            // Set before culling so input sees the full native viewport between renders.
            _camera.forceIntoRenderTexture = _camera.targetTexture == null || _originalForceTexture;
        }

        void OnPreCull()
        {
            if (_camera.targetTexture != null || _width <= 0 || _height <= 0) return;
            _rendering = true;
            _camera.orthographicSize = ConfiguredHalfHeight;
            _camera.pixelRect = new Rect(0, 0, LogicalRenderSize.x, LogicalRenderSize.y);
            _camera.aspect = (float)_width / _height;
        }

        void OnPreRender()
        {
            if (_rendering) PixelPerfectRendering.pixelSnapSpacing = WorldArtSettings.Current.UnitsPerPixel;
        }

        void OnPostRender()
        {
            if (!_rendering) return;
            var texture = _camera.activeTexture;
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                LastRenderedTextureSize = new Vector2Int(texture.width, texture.height);
            }
            RenderCount++;
            PixelPerfectRendering.pixelSnapSpacing = 0;
            RestoreDisplayViewport();
            _rendering = false;
        }

        void RestoreDisplayViewport()
        {
            _camera.rect = new Rect(0, 0, 1, 1);
            _camera.aspect = (float)_width / _height;
            _camera.orthographicSize = ConfiguredHalfHeight;
        }

        void OnDisable()
        {
            if (_camera == null) return;
            if (_rendering) PixelPerfectRendering.pixelSnapSpacing = 0;
            _rendering = false;
            _camera.rect = _originalRect;
            _camera.orthographicSize = _originalHalfHeight;
            _camera.forceIntoRenderTexture = _originalForceTexture;
            _camera.ResetAspect();
        }
    }
}
