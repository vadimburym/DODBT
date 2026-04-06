// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

internal sealed class BtCreateAssetWindow : EditorWindow
{
    private Action<string> onConfirm;
    private TextField nameField;

    private string windowTitle;
    private string messageText;

    public static void Open(EditorWindow owner, string title, string initialName, Action<string> onConfirm, string message = null)
    {
        var window = CreateInstance<BtCreateAssetWindow>();
        window.windowTitle = title;
        window.messageText = message;
        window.onConfirm = onConfirm;

        window.titleContent = new GUIContent(title);
        window.minSize = new Vector2(460, 220);
        window.maxSize = new Vector2(460, 220);

        window.ShowUtility();
        window.CenterOnOwnerOrScreen(owner);

        window.BuildUI(initialName);
        window.Focus();
    }

    private void BuildUI(string initialName)
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
        body.style.paddingTop = 14;
        body.style.paddingBottom = 10;
        root.Add(body);

        if (!string.IsNullOrEmpty(messageText))
        {
            var messageLabel = new Label(messageText);
            messageLabel.style.whiteSpace = WhiteSpace.Normal;
            messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            messageLabel.style.color = new Color(1f, 1f, 1f, 0.72f);
            messageLabel.style.marginBottom = 12;
            body.Add(messageLabel);
        }
        
        var fieldCard = new VisualElement();
        fieldCard.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
        fieldCard.style.borderTopLeftRadius = 10;
        fieldCard.style.borderTopRightRadius = 10;
        fieldCard.style.borderBottomLeftRadius = 10;
        fieldCard.style.borderBottomRightRadius = 10;
        fieldCard.style.borderLeftWidth = 1;
        fieldCard.style.borderRightWidth = 1;
        fieldCard.style.borderTopWidth = 1;
        fieldCard.style.borderBottomWidth = 1;
        fieldCard.style.borderLeftColor = new Color(1f, 1f, 1f, 0.10f);
        fieldCard.style.borderRightColor = new Color(1f, 1f, 1f, 0.10f);
        fieldCard.style.borderTopColor = new Color(1f, 1f, 1f, 0.10f);
        fieldCard.style.borderBottomColor = new Color(1f, 1f, 1f, 0.10f);
        fieldCard.style.paddingLeft = 12;
        fieldCard.style.paddingRight = 12;
        fieldCard.style.paddingTop = 10;
        fieldCard.style.paddingBottom = 10;
        body.Add(fieldCard);

        var fieldTitle = new Label("Asset name");
        fieldTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        fieldTitle.style.color = new Color(1f, 1f, 1f, 0.85f);
        fieldTitle.style.marginBottom = 6;
        fieldCard.Add(fieldTitle);

        nameField = new TextField();
        nameField.value = initialName ?? "BtGraph";
        nameField.style.height = 26;
        nameField.style.unityTextAlign = TextAnchor.MiddleLeft;
        fieldCard.Add(nameField);
        
        var footer = new VisualElement();
        footer.style.height = 58;
        footer.style.marginTop = 10;
        footer.style.justifyContent = Justify.Center;
        footer.style.alignItems = Align.Center;
        root.Add(footer);

        var buttonsRow = new VisualElement();
        buttonsRow.style.flexDirection = FlexDirection.Row;
        buttonsRow.style.justifyContent = Justify.Center;
        buttonsRow.style.alignItems = Align.Center;
        footer.Add(buttonsRow);

        var cancelButton = CreateNiceButton("Cancel", new Color(0.20f, 0.20f, 0.20f, 1f), new Color(1f, 1f, 1f, 0.18f));
        cancelButton.clicked += Close;
        cancelButton.style.marginRight = 10;
        var okButton = CreateNiceButton("OK", new Color(0.12f, 0.38f, 0.66f, 1f), new Color(0.65f, 0.85f, 1f, 0.20f));
        okButton.clicked += Confirm;

        buttonsRow.Add(cancelButton);
        buttonsRow.Add(okButton);

        root.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
            {
                Confirm();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Close();
                evt.StopPropagation();
            }
        });

        // Focus
        nameField.schedule.Execute(() =>
        {
            nameField.Focus();
            nameField.SelectAll();
        });
    }

    private void Confirm()
    {
        string assetName = nameField?.value?.Trim();
        if (string.IsNullOrEmpty(assetName))
        {
            EditorUtility.DisplayDialog("Invalid name", "Asset name cannot be empty.", "OK");
            return;
        }

        onConfirm?.Invoke(assetName);
        Close();
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