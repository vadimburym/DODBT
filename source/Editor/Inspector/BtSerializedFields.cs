using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// Native drawers handle ordinary fields. Managed references add a searchable type selector,
// including nested interfaces and list elements, without a runtime reflection dependency.
internal sealed class BtSerializedFields : IDisposable
{
    private const float Gap = 2;
    private static float Line => EditorGUIUtility.singleLineHeight;
    private readonly UnityEngine.Object _owner;
    private readonly Func<string, string> _resolvePath;
    private readonly Action _changed;
    private readonly Dictionary<string, ReorderableList> _lists = new();
    private readonly HashSet<long> _drawing = new();
    private readonly HashSet<long> _measuring = new();
    private BtTypePicker _picker;
    private bool _disposed;
    private readonly string _leafControlName = "DODBT.LeafSelector." + Guid.NewGuid().ToString("N");

    internal BtSerializedFields(UnityEngine.Object owner, Func<string, string> resolvePath, Action changed)
    {
        _owner = owner;
        _resolvePath = resolvePath;
        _changed = changed;
    }

    internal void DrawLayout(SerializedProperty property, GUIContent label)
    {
        if (property == null) return;
        _measuring.Clear();
        float height = Height(property, label);
        var rect = EditorGUILayout.GetControlRect(true, height);
        _drawing.Clear();
        Draw(rect, property, label);
    }

    internal static bool HasVisibleFields(SerializedProperty property)
    {
        if (property == null || property.managedReferenceValue == null) return false;
        using var child = property.Copy();
        using var end = child.GetEndProperty();
        return child.NextVisible(true) && !SerializedProperty.EqualContents(child, end) && child.depth > property.depth;
    }

    internal void DrawLeafLayout(SerializedProperty property)
    {
        if (property == null) return;
        bool hasFields = HasVisibleFields(property);
        var children = new List<SerializedProperty>();
        try
        {
            if (hasFields)
            {
                using var child = property.Copy();
                using var end = child.GetEndProperty();
                if (child.NextVisible(true))
                    do
                    {
                        if (SerializedProperty.EqualContents(child, end)) break;
                        children.Add(child.Copy());
                    } while (child.NextVisible(false));
            }
            _measuring.Clear();
            _measuring.Add(property.managedReferenceId);
            float height = BtLeafSelector.Height;
            foreach (var child in children) height += Gap + Height(child, new GUIContent(child.displayName));
            if (hasFields) height += 9;
            var rect = EditorGUILayout.GetControlRect(false, height);
            var header = new Rect(rect.x, rect.y, rect.width, BtLeafSelector.Height);
            if (BtLeafSelector.Draw(header, BtTypePicker.Resolve(property.managedReferenceFullTypename), _leafControlName))
                OpenPicker(header, property, leafSelection: true);
            if (!hasFields) return;
            EditorGUI.DrawRect(new Rect(rect.x + 8, header.yMax + 4, rect.width - 16, 1), new Color(1, 1, 1, .08f));
            float y = header.yMax + 9;
            _drawing.Clear();
            _drawing.Add(property.managedReferenceId);
            foreach (var child in children)
            {
                var label = new GUIContent(child.displayName, child.tooltip);
                _measuring.Clear();
                _measuring.Add(property.managedReferenceId);
                float childHeight = Height(child, label);
                Draw(new Rect(rect.x + 8, y, rect.width - 16, childHeight), child, label);
                y += childHeight + Gap;
            }
        }
        finally { foreach (var child in children) child.Dispose(); }
    }

    private void OpenPicker(Rect rect, SerializedProperty property, bool leafSelection = false)
    {
        string path = property.propertyPath;
        var baseType = BtTypePicker.Resolve(property.managedReferenceFieldTypename);
        _picker = new BtTypePicker(baseType, selected => Assign(path, selected), BtTypePicker.Resolve(property.managedReferenceFullTypename), leafSelection);
        _picker.Show(rect);
    }

    private static bool NeedsManagedEditor(SerializedProperty property)
    {
        if (property.propertyType == SerializedPropertyType.ManagedReference) return true;
        if (property.isArray && property.propertyType != SerializedPropertyType.String) return true;
        if (property.propertyType != SerializedPropertyType.Generic) return false;
        var cursor = property.Copy();
        var end = cursor.GetEndProperty();
        while (cursor.NextVisible(true) && !SerializedProperty.EqualContents(cursor, end))
            if (cursor.propertyType == SerializedPropertyType.ManagedReference) return true;
        return false;
    }

    private float Height(SerializedProperty property, GUIContent label)
    {
        if (!NeedsManagedEditor(property)) return EditorGUI.GetPropertyHeight(property, label, true);
        if (property.isArray) return property.isExpanded ? Line + Gap + List(property).GetHeight() : Line;
        if (!property.isExpanded) return Line;
        bool managed = property.propertyType == SerializedPropertyType.ManagedReference;
        if (managed && (property.managedReferenceValue == null || !_measuring.Add(property.managedReferenceId))) return Line;
        float height = Line;
        var child = property.Copy();
        var end = child.GetEndProperty();
        if (child.NextVisible(true))
            do
            {
                if (SerializedProperty.EqualContents(child, end)) break;
                height += Gap + Height(child, new GUIContent(child.displayName));
            } while (child.NextVisible(false));
        if (managed) _measuring.Remove(property.managedReferenceId);
        return height;
    }

    private void Draw(Rect rect, SerializedProperty property, GUIContent label)
    {
        if (!NeedsManagedEditor(property))
        {
            EditorGUI.PropertyField(rect, property, label, true);
            return;
        }
        var header = new Rect(rect.x, rect.y, rect.width, Line);
        if (property.isArray)
        {
            property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, label, true);
            if (property.isExpanded) List(property).DoList(new Rect(rect.x, rect.y + Line + Gap, rect.width, rect.height - Line - Gap));
            return;
        }
        bool managed = property.propertyType == SerializedPropertyType.ManagedReference;
        bool hasValue = !managed || property.managedReferenceValue != null;
        if (managed)
        {
            var foldout = new Rect(header.x, header.y, label == GUIContent.none ? 16 : Math.Min(110, header.width * .4f), Line);
            if (hasValue) property.isExpanded = EditorGUI.Foldout(foldout, property.isExpanded, label, true);
            else if (label != GUIContent.none) EditorGUI.LabelField(foldout, label);
            var button = new Rect(foldout.xMax, header.y, header.width - foldout.width, Line);
            var type = BtTypePicker.Resolve(property.managedReferenceFullTypename);
            if (EditorGUI.DropdownButton(button, new GUIContent(type?.Name ?? "None", type?.FullName), FocusType.Keyboard))
            {
                OpenPicker(button, property);
            }
        }
        else property.isExpanded = EditorGUI.Foldout(header, property.isExpanded, label, true);
        if (!property.isExpanded || !hasValue) return;
        if (managed && !_drawing.Add(property.managedReferenceId)) return;
        float y = rect.y + Line + Gap;
        var child = property.Copy();
        var end = child.GetEndProperty();
        if (child.NextVisible(true))
            do
            {
                if (SerializedProperty.EqualContents(child, end)) break;
                var childLabel = new GUIContent(child.displayName, child.tooltip);
                _measuring.Clear();
                if (managed) _measuring.Add(property.managedReferenceId);
                float height = Height(child, childLabel);
                Draw(new Rect(rect.x + 12, y, rect.width - 12, height), child, childLabel);
                y += height + Gap;
            } while (child.NextVisible(false));
        if (managed) _drawing.Remove(property.managedReferenceId);
    }

    private ReorderableList List(SerializedProperty property)
    {
        string path = property.propertyPath;
        if (!_lists.TryGetValue(path, out var list))
        {
            list = new ReorderableList(property.serializedObject, property.Copy(), true, false, true, true);
            list.elementHeightCallback = i =>
            {
                if (i < 0 || i >= list.serializedProperty.arraySize) return Line;
                return Height(list.serializedProperty.GetArrayElementAtIndex(i), new GUIContent("Element " + i)) + 4;
            };
            list.drawElementCallback = (r, i, active, focused) =>
            {
                if (i < 0 || i >= list.serializedProperty.arraySize) return;
                r.y += 2;
                Draw(r, list.serializedProperty.GetArrayElementAtIndex(i), new GUIContent("Element " + i));
            };
            list.onAddCallback = l =>
            {
                int index = l.serializedProperty.arraySize++;
                var element = l.serializedProperty.GetArrayElementAtIndex(index);
                if (element.propertyType == SerializedPropertyType.ManagedReference) element.managedReferenceValue = null;
                if (element.propertyType == SerializedPropertyType.ObjectReference) element.objectReferenceValue = null;
                l.index = index;
            };
            _lists[path] = list;
        }
        list.serializedProperty = property.Copy();
        return list;
    }

    private void Assign(string oldPath, Type type)
    {
        if (_disposed || _owner == null) return;
        string path = _resolvePath(oldPath);
        if (path == null) return;
        using var serialized = new SerializedObject(_owner);
        var property = serialized.FindProperty(path);
        if (property == null || property.propertyType != SerializedPropertyType.ManagedReference) return;
        if (BtTypePicker.Resolve(property.managedReferenceFullTypename) == type) return;
        object value;
        try { value = type == null ? null : Activator.CreateInstance(type, true); }
        catch (Exception error)
        {
            Debug.LogError("Cannot create " + type?.FullName + ": " + error.GetBaseException().Message, _owner);
            return;
        }
        property.managedReferenceValue = value;
        property.isExpanded = value != null;
        if (serialized.ApplyModifiedProperties())
        {
            _lists.Clear();
            _changed?.Invoke();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _lists.Clear();
        _picker = null;
    }
}
