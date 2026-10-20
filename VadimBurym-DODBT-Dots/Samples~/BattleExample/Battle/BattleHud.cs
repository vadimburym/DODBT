using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace DODBT.Example
{
    [DisallowMultipleComponent]
    public sealed class BattleHud : MonoBehaviour
    {
        public float FrameMilliseconds { get; private set; }
        public float FramesPerSecond { get; private set; }
        public int EntityCount { get; private set; }

        private Canvas _canvas;
        private Text _statistics;
        private Text _entityCount;
        private BattleExample _example;
        private float _elapsed;
        private int _frames;

        private void OnEnable()
        {
            _elapsed = 0;
            _frames = 0;
            FrameMilliseconds = 0;
            FramesPerSecond = 0;
            EntityCount = 0;
            _example = GetComponent<BattleExample>();
            var root = new GameObject("Battle HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            _canvas = root.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var frameColor = new Color(0, 1, .2793231f);
            var panel = CreatePanel("Frame statistics", new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -16), new Vector2(450, 150));
            panel.localScale = Vector3.one * .75f;
            _statistics = CreateText(panel, font, "Frame: -- ms\nFPS: --", 45, frameColor, new Vector2(24, -12), new Vector2(-48, 126));
            _statistics.fontStyle = FontStyle.Bold;
            var agents = CreatePanel("Entity count", new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(200, 72));
            var caption = CreateText(agents, font, "ENTITIES", 12, new Color(.45f, .8f, 1), new Vector2(16, -8), new Vector2(-32, 20));
            caption.alignment = TextAnchor.MiddleCenter;
            _entityCount = CreateText(agents, font, "0", 28, Color.white, new Vector2(16, -28), new Vector2(-32, 36));
            _entityCount.alignment = TextAnchor.MiddleCenter;
            var controls = CreatePanel("Controls", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 16), new Vector2(560, 42));
            var hint = CreateText(controls, font, "WASD / Arrow keys: Move    |    Mouse wheel: Zoom", 18, Color.white, new Vector2(12, -8), new Vector2(-24, 26));
            hint.alignment = TextAnchor.MiddleCenter;
        }

        private RectTransform CreatePanel(string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(_canvas.transform, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = panel.GetComponent<Image>();
            image.color = new Color(.025f, .035f, .05f, .88f);
            image.raycastTarget = false;
            return rect;
        }

        private static Text CreateText(RectTransform parent, Font font, string value, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rect = label.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            var text = label.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private void Update()
        {
            if (Time.unscaledDeltaTime <= 0) return;
            _elapsed += Time.unscaledDeltaTime;
            _frames++;
            if (_elapsed < .25f) return;
            FrameMilliseconds = 1000f * _elapsed / _frames;
            FramesPerSecond = _frames / _elapsed;
            _statistics.text = string.Format(CultureInfo.InvariantCulture, "Frame: {0:F2} ms\nFPS: {1:F1}", FrameMilliseconds, FramesPerSecond);
            EntityCount = _example != null ? _example.Session?.AgentCount ?? 0 : 0;
            _entityCount.text = EntityCount.ToString("N0", CultureInfo.InvariantCulture);
            _elapsed = 0;
            _frames = 0;
        }

        private void OnDisable()
        {
            if (_canvas == null) return;
            _canvas.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(_canvas.gameObject);
            else DestroyImmediate(_canvas.gameObject);
            _canvas = null;
            _statistics = null;
            _entityCount = null;
            _example = null;
        }
    }
}
