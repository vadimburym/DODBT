// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class BtConfirmDeleteWindow : EditorWindow
{
    private Action onYes;

    public static void Open(EditorWindow owner, string assetName, Action onYes)
    {
        var window = CreateInstance<BtConfirmDeleteWindow>();
        window.titleContent = new GUIContent("Delete Graph");
        window.minSize = new Vector2(460, 220);
        window.maxSize = new Vector2(460, 220);
        window.onYes = onYes;

        window.ShowUtility();
        window.CenterOnOwnerOrScreen(owner);

        window.BuildUI(assetName);
        window.Focus();
    }

    private void BuildUI(string assetName)
    {
        rootVisualElement.Clear();

        var root = new VisualElement();
        root.style.flexGrow = 1;
        root.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);
        root.style.paddingLeft = 14;
        root.style.paddingRight = 14;
        root.style.paddingTop = 12;
        root.style.paddingBottom = 12;
        rootVisualElement.Add(root);
        
        var body = new VisualElement();
        body.style.flexGrow = 1;
        body.style.paddingLeft = 6;
        body.style.paddingRight = 6;
        body.style.paddingTop = 16;
        body.style.paddingBottom = 10;
        root.Add(body);

        var message = new Label($"Are you sure you want to delete:\n\"{assetName}\" ?");
        message.style.whiteSpace = WhiteSpace.Normal;
        message.style.unityTextAlign = TextAnchor.MiddleCenter;
        message.style.color = new Color(1f, 1f, 1f, 0.80f);
        message.style.fontSize = 13;
        message.style.marginBottom = 12;
        body.Add(message);

        var hint = new Label("This action cannot be undone.");
        hint.style.unityTextAlign = TextAnchor.MiddleCenter;
        hint.style.color = new Color(1f, 0.85f, 0.85f, 0.70f);
        hint.style.marginBottom = 10;
        body.Add(hint);

        var footer = new VisualElement();
        footer.style.height = 58;
        footer.style.justifyContent = Justify.Center;
        footer.style.alignItems = Align.Center;
        root.Add(footer);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.Center;
        row.style.alignItems = Align.Center;
        footer.Add(row);

        var noButton = CreateNiceButton("No", new Color(0.20f, 0.20f, 0.20f, 1f), new Color(1f, 1f, 1f, 0.18f));
        noButton.clicked += Close;
        noButton.style.marginRight = 10;
        var yesButton = CreateNiceButton("Yes, delete", new Color(0.62f, 0.16f, 0.16f, 1f), new Color(1f, 0.65f, 0.65f, 0.22f));
        yesButton.clicked += () =>
        {
            onYes?.Invoke();
            Close();
        };

        row.Add(noButton);
        row.Add(yesButton);
        
        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                onYes?.Invoke();
                Close();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        });
    }

    private static Button CreateNiceButton(string text, Color background, Color border)
    {
        var button = new Button { text = text };

        button.style.width = 150;
        button.style.height = 34;
        button.style.unityFontStyleAndWeight = FontStyle.Bold;
        button.style.fontSize = 13;

        button.style.backgroundColor = background;

        button.style.borderTopWidth = 1;
        button.style.borderBottomWidth = 1;
        button.style.borderLeftWidth = 1;
        button.style.borderRightWidth = 1;

        button.style.borderTopColor = border;
        button.style.borderBottomColor = border;
        button.style.borderLeftColor = border;
        button.style.borderRightColor = border;

        button.style.borderTopLeftRadius = 10;
        button.style.borderTopRightRadius = 10;
        button.style.borderBottomLeftRadius = 10;
        button.style.borderBottomRightRadius = 10;

        return button;
    }

    private void CenterOnOwnerOrScreen(EditorWindow owner)
    {
        Rect pos = position;

        if (owner != null)
        {
            Rect ownerPos = owner.position;
            float x = ownerPos.x + (ownerPos.width - pos.width) * 0.5f;
            float y = ownerPos.y + (ownerPos.height - pos.height) * 0.5f;
            position = new Rect(x, y, pos.width, pos.height);
            return;
        }
        
        Rect screen = GetScreenRect();
        float sx = screen.x + (screen.width - pos.width) * 0.5f;
        float sy = screen.y + (screen.height - pos.height) * 0.5f;
        position = new Rect(sx, sy, pos.width, pos.height);
    }

    private static Rect GetScreenRect()
    {
        int width = Display.main != null ? Display.main.systemWidth : 1920;
        int height = Display.main != null ? Display.main.systemHeight : 1080;

        return new Rect(0, 0, width, height);
    }
}
#endif