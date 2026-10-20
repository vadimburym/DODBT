using UnityEngine;
using UnityEngine.UI;

namespace DODBT.Light.Example.Setup
{
    [DisallowMultipleComponent]
    public sealed class ExampleHud : MonoBehaviour
    {
        private GameObject _root;

        private void OnEnable()
        {
            _root = new GameObject("Example2D HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            _root.transform.SetParent(transform, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            var panel = new GameObject("Controls", typeof(RectTransform), typeof(Image));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(_root.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 16);
            rect.sizeDelta = new Vector2(360, 52);
            var background = panel.GetComponent<Image>();
            background.color = new Color(.025f, .035f, .05f, .88f);
            background.raycastTarget = false;

            var label = new GameObject("Movement hint", typeof(RectTransform), typeof(Text));
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12, 6);
            labelRect.offsetMax = new Vector2(-12, -6);
            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.text = "WASD / Arrow keys: Move";
        }

        private void OnDisable()
        {
            if (_root == null) return;
            _root.SetActive(false);
            if (Application.isPlaying) Destroy(_root);
            else DestroyImmediate(_root);
            _root = null;
        }
    }
}
