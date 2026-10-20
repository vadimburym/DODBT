// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    using System;
    using UnityEditor;
    using UnityEngine;

    // Both node and Details inspectors edit the actual asset, never a detached copy.
    internal sealed class BtNodeInspector : IDisposable
    {
        private static readonly Unity.Profiling.ProfilerMarker DrawMarker = new("DODBT.Light.NodeInspector");
        private readonly BtGraphAsset _asset;
        private readonly string _guid;
        private readonly BtNodeKind _kind;
        private readonly Action<bool> _changed;
        private Type _leafType;
        private readonly SerializedObject _serialized;
        private readonly BtSerializedFields _fields;
        private int _rootIndex = -1;
        private string _rootPath;

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
            using var array = _serialized.FindProperty(list);
            if (_rootIndex >= 0 && _rootIndex < array.arraySize)
            {
                using var item = array.GetArrayElementAtIndex(_rootIndex);
                using var guid = item.FindPropertyRelative("Guid");
                if (guid.stringValue == _guid) return _rootPath;
            }
            for (int i = 0; i < array.arraySize; i++)
            {
                using var item = array.GetArrayElementAtIndex(i);
                using var guid = item.FindPropertyRelative("Guid");
                if (guid.stringValue != _guid) continue;
                _rootIndex = i;
                return _rootPath = $"{list}.Array.data[{i}]";
            }
            _rootIndex = -1;
            _rootPath = null;
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
            using var profile = DrawMarker.Auto();
            if (_asset == null) return;
            if (readOnly) _serialized.UpdateIfRequiredOrScript();
            else _serialized.Update();
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
#else
                        EditorGUILayout.HelpBox("Enable DODBT_DECORATORS to use this node. Saved data is preserved.", MessageType.Warning);
#endif
                        break;
                    case BtNodeKind.Leaf:
                        using (var property = _serialized.FindProperty(root + ".Leaf"))
                            _fields.DrawLeafLayout(property);
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

        private void DrawField(string root, string name)
        {
            using var property = _serialized.FindProperty(root + "." + name);
            _fields.DrawLayout(property, new GUIContent(ObjectNames.NicifyVariableName(name)));
        }

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
}
