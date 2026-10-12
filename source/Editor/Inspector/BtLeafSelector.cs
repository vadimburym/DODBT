using System;
using UnityEditor;
using UnityEngine;

internal static class BtLeafSelector
{
    internal const float Height = 26;
    private static readonly Vector3[] LeafShape = new Vector3[8];
    private static readonly Vector3[] Chevron = new Vector3[3];
    private static GUIStyle _label;

    internal static bool Draw(Rect rect, Type type, string controlName)
    {
        _label ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip,
            padding = new RectOffset(30, 25, 0, 0),
            fontSize = 12
        };
        var button = rect;
        bool hover = GUI.enabled && button.Contains(Event.current.mousePosition);
        bool focused = GUI.enabled && GUI.GetNameOfFocusedControl() == controlName;
        if (Event.current.type == EventType.Repaint)
        {
            var border = focused ? new Color(.5f, .65f, .78f) : hover ? new Color(.36f, .38f, .4f) : new Color(.22f, .23f, .24f);
            var fill = hover ? new Color(.19f, .20f, .21f) : new Color(.135f, .14f, .145f);
            GUI.DrawTexture(button, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, border, 0, 6);
            GUI.DrawTexture(new Rect(button.x + 1, button.y + 1, button.width - 2, button.height - 2),
                Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, fill, 0, 5);
        }
        Color text = type == null ? new Color(.59f, .61f, .63f) : new Color(.92f, .93f, .94f);
        _label.normal.textColor = _label.hover.textColor = _label.focused.textColor = _label.active.textColor = text;
        GUI.SetNextControlName(controlName);
        bool open = EditorGUI.DropdownButton(button, new GUIContent(type?.Name ?? "Выбрать Leaf…",
            type?.FullName ?? "Выбрать реализацию Leaf"), FocusType.Keyboard, _label);
        if (GUI.enabled && GUI.GetNameOfFocusedControl() == controlName && Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter ||
             Event.current.keyCode == KeyCode.DownArrow || Event.current.keyCode == KeyCode.Space))
        {
            open = true;
            Event.current.Use();
        }
        DrawIcon(new Rect(button.x + 8, button.y + 5, 16, 16), type == null ? text : Color.white);
        DrawChevron(new Rect(button.xMax - 16, button.y + 11, 7, 4), true, new Color(.64f, .66f, .68f));
        return open;
    }

    private static void DrawChevron(Rect rect, bool down, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        Chevron[0] = new Vector3(rect.x, rect.y);
        Chevron[1] = down ? new Vector3(rect.center.x, rect.yMax) : new Vector3(rect.xMax, rect.center.y);
        Chevron[2] = down ? new Vector3(rect.xMax, rect.y) : new Vector3(rect.x, rect.yMax);
        var saved = Handles.color;
        if (!GUI.enabled) color.a *= .45f;
        Handles.color = color;
        Handles.DrawAAPolyLine(1.5f, Chevron);
        Handles.color = saved;
    }

    internal static void DrawIcon(Rect rect, Color color)
    {
        if (Event.current.type != EventType.Repaint) return;
        var saved = Handles.color;
        LeafShape[0] = new Vector3(rect.x + 3, rect.y + 12);
        LeafShape[1] = new Vector3(rect.x + 2, rect.y + 8);
        LeafShape[2] = new Vector3(rect.x + 4, rect.y + 4);
        LeafShape[3] = new Vector3(rect.x + 9, rect.y + 2);
        LeafShape[4] = new Vector3(rect.x + 14, rect.y + 1);
        LeafShape[5] = new Vector3(rect.x + 13, rect.y + 7);
        LeafShape[6] = new Vector3(rect.x + 10, rect.y + 11);
        LeafShape[7] = new Vector3(rect.x + 6, rect.y + 13);
        if (!GUI.enabled) color.a *= .45f;
        Handles.color = color;
        Handles.DrawAAConvexPolygon(LeafShape);
        Handles.DrawAAPolyLine(1.5f, new Vector3(rect.x + 1, rect.y + 15), new Vector3(rect.x + 6, rect.y + 10));
        Handles.color = new Color(.35f, .39f, .42f, color.a);
        Handles.DrawAAPolyLine(1, new Vector3(rect.x + 5, rect.y + 11), new Vector3(rect.x + 11, rect.y + 5));
        Handles.color = saved;
    }
}
