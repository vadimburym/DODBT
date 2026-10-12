using System;
using UnityEditor;
using UnityEngine;

// Both node and Details inspectors edit the actual asset, never a detached copy.
internal sealed class BtNodeInspector : IDisposable
{
    private readonly BtGraphAsset _asset;
    private readonly string _guid;
    private readonly BtNodeKind _kind;
    private readonly Action<bool> _changed;
    private Type _leafType;
    private readonly SerializedObject _serialized;
    private readonly BtSerializedFields _fields;

    internal BtNodeInspector(BtGraphAsset asset, string guid, BtNodeKind kind, Action<bool> changed)
    {
        _asset = asset;
        _guid = guid;
        _kind = kind;
        _changed = changed;
        _leafType = asset.FindLeafData(guid)?.Leaf?.GetType();
        _serialized = new SerializedObject(asset);
        _fields = new BtSerializedFields(asset, RemapPath, NotifyChanged);
    }

    internal string RootPath()
    {
        string list = _kind switch
        {
            BtNodeKind.Leaf => "LeafNodes",
            BtNodeKind.MemorySequence => "MemorySequenceNodes",
            BtNodeKind.MemorySelector => "MemorySelectorNodes",
            BtNodeKind.Parallel => "ParallelNodes",
            BtNodeKind.Timeout or BtNodeKind.Cooldown or BtNodeKind.Inverter => "DecoratorNodes",
            _ => null
        };
        if (list == null || _asset == null) return null;
        var array = _serialized.FindProperty(list);
        for (int i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).FindPropertyRelative("Guid").stringValue == _guid)
                return $"{list}.Array.data[{i}]";
        return null;
    }

    private string RemapPath(string oldPath)
    {
        _serialized.Update();
        var root = RootPath();
        int end = oldPath.IndexOf(']');
        return root == null || end < 0 ? null : root + oldPath.Substring(end + 1);
    }

    internal void Draw(bool readOnly)
    {
        if (_asset == null) return;
        _serialized.Update();
        string root = RootPath();
        if (root == null) return;
        using (new EditorGUI.DisabledScope(readOnly))
        {
            switch (_kind)
            {
                case BtNodeKind.Timeout:
                case BtNodeKind.Cooldown:
                case BtNodeKind.Inverter:
#if DODBT_DECORATORS
                    if (_kind != BtNodeKind.Inverter) DrawField(root, "Duration");
                    else EditorGUILayout.LabelField("Success ↔ Failure; Running unchanged", EditorStyles.miniLabel);
#else
                    EditorGUILayout.HelpBox("Enable DODBT_DECORATORS to use this node. Saved data is preserved.", MessageType.Warning);
#endif
                    break;
                case BtNodeKind.Leaf:
                    _fields.DrawLayout(_serialized.FindProperty(root + ".Leaf"), GUIContent.none);
                    break;
                case BtNodeKind.MemorySequence:
                    DrawField(root, "ResetOnFailure");
                    DrawField(root, "ResetOnAbort");
                    break;
                case BtNodeKind.MemorySelector:
                    DrawField(root, "PickRandom");
                    DrawField(root, "ResetOnAbort");
                    break;
                case BtNodeKind.Parallel:
                    DrawField(root, "CacheChildStatus");
                    DrawField(root, "FailureThreshold");
                    DrawField(root, "SuccessThreshold");
                    break;
            }
        }
        if (_serialized.ApplyModifiedProperties()) NotifyChanged();
    }

    private void DrawField(string root, string name) =>
        _fields.DrawLayout(_serialized.FindProperty(root + "." + name), new GUIContent(ObjectNames.NicifyVariableName(name)));

    private void NotifyChanged()
    {
        if (_asset == null) return;
        var leafType = _asset.FindLeafData(_guid)?.Leaf?.GetType();
        bool typeChanged = leafType != _leafType;
        _leafType = leafType;
        _asset.MarkModified();
        _changed?.Invoke(typeChanged);
    }

    public void Dispose()
    {
        _fields.Dispose();
        _serialized.Dispose();
    }
}
