// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    internal sealed class BtLauncherWindow : EditorWindow
    {
        private const string BannerGuid = "79ca2e7d34eb4ce9b93e6c1ae945cf38";
        private Toggle _icons;

        [MenuItem("Tools/VadimBurym/DODBT-Dots", false, 100)]
        internal static void Open()
        {
            var window = GetWindow<BtLauncherWindow>(true, "DODBT-Dots", true);
            BtCreationWindowUi.ShowCentered(window, new Vector2(440, 524));
        }

        private void OnEnable() => BtEditorPreferences.Changed += UpdateSettings;
        private void OnDisable() => BtEditorPreferences.Changed -= UpdateSettings;
        private void UpdateSettings() => _icons?.SetValueWithoutNotify(BtEditorPreferences.UseNodeIcons);

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.Clear();
            root.style.backgroundColor = new Color(.12f, .12f, .12f);
            root.style.paddingLeft = root.style.paddingRight = 22;
            root.style.paddingTop = root.style.paddingBottom = 18;

            var banner = new Image
            {
                image = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(BannerGuid)),
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            banner.style.height = 130;
            banner.style.marginBottom = 8;
            root.Add(banner);
            var heading = new Label("DODBT-Dots");
            heading.style.fontSize = 20;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.unityTextAlign = TextAnchor.MiddleCenter;
            heading.style.marginBottom = 18;
            root.Add(heading);

            AddAction("BT Editor", BtEditorWindow.Open, true);
            AddAction("BT Debugger", BtEntityDebuggerWindow.Open);
            AddAction("New Context", ContextTemplateCreator.Open);

            AddAction("New Leaf", LeafTemplateCreator.CreateNewLeaf);

            var settings = new VisualElement();
            settings.style.marginTop = 14;
            settings.style.paddingLeft = settings.style.paddingRight = 14;
            settings.style.paddingTop = settings.style.paddingBottom = 12;
            settings.style.backgroundColor = new Color(.09f, .09f, .095f);
            Round(settings, 9);
            root.Add(settings);
            var label = new Label("Settings");
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 10;
            settings.Add(label);
            _icons = new Toggle("Use node icons") { value = BtEditorPreferences.UseNodeIcons };
            _icons.tooltip = "Compact nodes with icons. Node parameters remain available in Details.";
            _icons.RegisterValueChangedCallback(e => BtEditorPreferences.UseNodeIcons = e.newValue);
            settings.Add(_icons);
            var markers = new Toggle("Show scene markers") { value = BtSceneDebugOverlay.Enabled };
            markers.style.marginTop = 6;
            markers.RegisterValueChangedCallback(e => BtSceneDebugOverlay.Enabled = e.newValue);
            settings.Add(markers);
            root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Escape) return;
                e.StopPropagation(); Close();
            });
        }

        private void AddAction(string label, Action action, bool primary = false)
        {
            var button = new Button(() => { Close(); action(); }) { text = label };
            button.style.height = 36;
            button.style.marginBottom = 6;
            button.style.marginLeft = button.style.marginRight = 0;
            button.style.fontSize = 13;
            button.style.unityFontStyleAndWeight = FontStyle.Normal;
            button.style.color = new Color(.82f, .84f, .86f);
            button.style.borderTopWidth = button.style.borderBottomWidth = 1;
            button.style.borderLeftWidth = button.style.borderRightWidth = 1;
            var idle = primary ? new Color(.18f, .23f, .26f) : new Color(.16f, .17f, .18f);
            var hover = primary ? new Color(.23f, .30f, .34f) : new Color(.21f, .23f, .25f);
            var border = primary ? new Color(.32f, .40f, .44f) : new Color(.27f, .29f, .31f);
            bool hovered = false, focused = false;
            void RefreshColors()
            {
                button.style.backgroundColor = hovered ? hover : idle;
                var outline = focused ? new Color(.46f, .59f, .65f) : border;
                button.style.borderTopColor = button.style.borderBottomColor = outline;
                button.style.borderLeftColor = button.style.borderRightColor = outline;
            }
            button.RegisterCallback<MouseEnterEvent>(_ => { hovered = true; RefreshColors(); });
            button.RegisterCallback<MouseLeaveEvent>(_ => { hovered = false; RefreshColors(); });
            button.RegisterCallback<FocusInEvent>(_ => { focused = true; RefreshColors(); });
            button.RegisterCallback<FocusOutEvent>(_ => { focused = false; RefreshColors(); });
            RefreshColors();
            Round(button, 6);
            rootVisualElement.Add(button);
        }

        private static void Round(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = element.style.borderBottomRightRadius = radius;
        }
    }
}
