// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VadimBurym.DodBehaviourTree.Dots.Editor;
using Object = UnityEngine.Object;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class EditorIconTests
    {
        private const string Folder = "Assets/__DODBT_Dots_IconTests";
        private const string IconPath = Folder + "/icon.asset";
        private bool _previousIcons;
        private bool _folderCreated;

        [Serializable, LeafIcon(IconPath)]
        private class IconLeaf : ILeaf { public LeafData GetCompiledData() => default; }
        [Serializable]
        private sealed class InheritedIconLeaf : IconLeaf { }
        [Serializable, LeafIcon("Assets/Missing/Icon.png")]
        private sealed class MissingIconLeaf : ILeaf { public LeafData GetCompiledData() => default; }
        [Serializable]
        private sealed class PlainLeaf : ILeaf { public LeafData GetCompiledData() => default; }

        [SetUp]
        public void Setup()
        {
            _folderCreated = false;
            _previousIcons = BtEditorPreferences.UseNodeIcons;
            BtEditorPreferences.UseNodeIcons = false;
            BtLeafIconCache.Invalidate();
            Assert.That(AssetDatabase.IsValidFolder(Folder), Is.False, "Test fixture folder already exists; refusing to overwrite it.");
        }

        [TearDown]
        public void Cleanup()
        {
            BtEditorPreferences.UseNodeIcons = _previousIcons;
            if (_folderCreated && AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
            BtLeafIconCache.Invalidate();
        }

        private Texture2D CreateIcon()
        {
            AssetDatabase.CreateFolder("Assets", "__DODBT_Dots_IconTests");
            _folderCreated = true;
            var texture = new Texture2D(4, 4);
            AssetDatabase.CreateAsset(texture, IconPath);
            BtLeafIconCache.Invalidate();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        }

        [Test]
        public void IconResolutionSupportsInheritanceAndGuidMovesAndDeletion()
        {
            Assert.That(BtLeafIconCache.Get(typeof(IconLeaf)), Is.Null);
            var texture = CreateIcon();
            Assert.That(BtLeafIconCache.Get(typeof(InheritedIconLeaf)), Is.EqualTo(texture));
            Assert.That(BtLeafIconCache.Get(typeof(PlainLeaf)), Is.Null);
            Assert.That(BtLeafIconCache.Get(typeof(MissingIconLeaf)), Is.Null);
            string guid = AssetDatabase.AssetPathToGUID(IconPath);
            Assert.That(BtLeafIconCache.Resolve(guid), Is.EqualTo(texture));
            string moved = Folder + "/moved.asset";
            Assert.That(AssetDatabase.MoveAsset(IconPath, moved), Is.Empty);
            BtLeafIconCache.Invalidate();
            Assert.That(BtLeafIconCache.Resolve(guid), Is.EqualTo(texture));
            Assert.That(BtLeafIconCache.Get(typeof(IconLeaf)), Is.Null, "A missing path must fall back to the regular leaf.");
            AssetDatabase.DeleteAsset(moved);
            BtLeafIconCache.Invalidate();
            Assert.That(BtLeafIconCache.Resolve(guid), Is.Null);
        }

        [UnityTest]
        public IEnumerator LiveTogglePreservesGraphSelectionCameraAndSettings()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.RootNode = new BtRootNodeData { Guid = "root", ChildrenGuid = "memory" };
            graph.Nodes.Add(new BtNodeHeader { Guid = "memory", Kind = BtNodeKind.MemorySequence,
                Title = "Memory Sequence", ParentGuid = "root", Position = new Vector2(120, 180) });
            graph.MemorySequenceNodes.Add(new BtMemorySequenceNodeData { Guid = "memory", ResetOnAbort = true });
            var host = ScriptableObject.CreateInstance<IconHost>();
            var view = new BtGraphView(false);
            try
            {
                host.Show();
                host.rootVisualElement.Add(view);
                view.Bind(graph);
                yield return null;
                var node = view.Query<BtNodeView>().ToList().Single(n => n.Guid == "memory");
                view.AddToSelection(node);
                node.SetGroupRoot(true, "Linked group");
                view.UpdateViewTransform(new Vector3(20, 30), new Vector3(.7f, .7f, 1));
                string before = EditorJsonUtility.ToJson(graph);
                bool dirty = EditorUtility.IsDirty(graph);
                BtEditorPreferences.UseNodeIcons = true;
                yield return null;
                yield return null;
                Assert.That(node.layout.width, Is.EqualTo(180).Within(1));
                Assert.That(node.titleContainer.layout.height, Is.EqualTo(78).Within(1));
                Assert.That(node.Q("bt-node-icon").layout.height, Is.GreaterThanOrEqualTo(34));
                var glyph = node.Q("bt-node-icon");
                var label = node.titleContainer.Q<Label>();
                Assert.That(glyph.worldBound.center.x, Is.EqualTo(node.titleContainer.worldBound.center.x).Within(1));
                Assert.That(node.Q("linked-group-icon").worldBound.xMax, Is.LessThan(glyph.worldBound.xMin));
                Assert.That(label.worldBound.yMin, Is.GreaterThanOrEqualTo(glyph.worldBound.yMax));
                Assert.That(node.extensionContainer.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(node.extensionContainer.layout.height, Is.GreaterThan(30));
                Assert.That(label.resolvedStyle.fontSize, Is.EqualTo(node.extensionContainer.Q<Toggle>().labelElement.resolvedStyle.fontSize));
                Assert.That(label.worldBound.yMin - glyph.worldBound.yMax, Is.LessThanOrEqualTo(2));
                Assert.That(view.selection, Does.Contain(node));
                Assert.That(view.contentViewContainer.resolvedStyle.scale.value.x, Is.EqualTo(.7f));
                Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(before));
                Assert.That(EditorUtility.IsDirty(graph), Is.EqualTo(dirty));
                BtEditorPreferences.UseNodeIcons = false;
                yield return null;
                yield return null;
                Assert.That(node.layout.width, Is.EqualTo(320).Within(1));
                Assert.That(node.extensionContainer.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(node.extensionContainer.Query<Toggle>().ToList()[1].value, Is.True);
                Assert.That(EditorJsonUtility.ToJson(graph), Is.EqualTo(before));
            }
            finally { view.DisposeInspectors(); host.Close(); Object.DestroyImmediate(graph); }
        }

        [Test]
        public void AllCompositeGlyphsWorkInEditAndDebugViews()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            try
            {
                BtEditorPreferences.UseNodeIcons = true;
                foreach (bool readOnly in new[] { false, true })
                foreach (var kind in new[] { BtNodeKind.Selector, BtNodeKind.Sequence, BtNodeKind.MemorySelector,
                    BtNodeKind.MemorySequence, BtNodeKind.Parallel, BtNodeKind.Root })
                {
                    var node = new BtNodeView("node", kind, kind.ToString(), null);
                    node.Bind(graph, _ => Assert.Fail("Presentation must not modify data."), readOnly, () => { });
                    bool compact = kind != BtNodeKind.Root;
                    Assert.That(node.style.width.value.value, Is.EqualTo(compact ? 180 : 320));
                    Assert.That(node.Q("bt-node-icon") != null, Is.EqualTo(compact));
                    if (kind == BtNodeKind.MemorySelector || kind == BtNodeKind.MemorySequence || kind == BtNodeKind.Parallel)
                    {
                        Assert.That(node.extensionContainer.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                        foreach (var field in node.extensionContainer.Children())
                            Assert.That(field.enabledSelf, Is.EqualTo(!readOnly));
                    }
                    node.DisposeInspector();
                }
            }
            finally { Object.DestroyImmediate(graph); }
        }

        [UnityTest]
        public IEnumerator CompactCompositeFieldsFitAndEditSerializedSettings()
        {
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.MemorySelectorNodes.Add(new BtMemorySelectorNodeData { Guid = "selector" });
            graph.MemorySequenceNodes.Add(new BtMemorySequenceNodeData { Guid = "sequence" });
            graph.ParallelNodes.Add(new BtParallelNodeData { Guid = "parallel" });
            var host = ScriptableObject.CreateInstance<IconHost>();
            var nodes = new[]
            {
                new BtNodeView("selector", BtNodeKind.MemorySelector, "Memory Selector", null),
                new BtNodeView("sequence", BtNodeKind.MemorySequence, "Memory Sequence", null),
                new BtNodeView("parallel", BtNodeKind.Parallel, "Parallel", null)
            };
            try
            {
                BtEditorPreferences.UseNodeIcons = true;
                host.Show();
                host.position = new Rect(20, 20, 600, 400);
                host.rootVisualElement.style.flexDirection = FlexDirection.Row;
                foreach (var node in nodes)
                {
                    node.Bind(graph, _ => { }, false, () => { });
                    host.rootVisualElement.Add(node);
                }
                yield return null;
                yield return null;
                foreach (var node in nodes)
                foreach (var field in node.extensionContainer.Children())
                {
                    Assert.That(field.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(field.layout.height, Is.GreaterThan(12));
                    var input = field.Q(className: "unity-base-field__input");
                    Assert.That(input.worldBound.xMax, Is.LessThanOrEqualTo(node.worldBound.xMax));
                    Assert.That(input.layout.width, Is.GreaterThan(20));
                }
                nodes[0].extensionContainer.Query<Toggle>().ToList()[0].value = true;
                nodes[1].extensionContainer.Query<Toggle>().ToList()[1].value = true;
                nodes[2].extensionContainer.Q<Toggle>().value = true;
                var thresholds = nodes[2].extensionContainer.Query<IntegerField>().ToList();
                thresholds[0].value = 2;
                thresholds[1].value = 3;
                Assert.That(graph.MemorySelectorNodes[0].PickRandom, Is.True);
                Assert.That(graph.MemorySequenceNodes[0].ResetOnAbort, Is.True);
                Assert.That(graph.ParallelNodes[0].CacheChildStatus, Is.True);
                Assert.That(graph.ParallelNodes[0].FailureThreshold, Is.EqualTo(2));
                Assert.That(graph.ParallelNodes[0].SuccessThreshold, Is.EqualTo(3));
            }
            finally
            {
                foreach (var node in nodes) node.DisposeInspector();
                host.Close();
                Undo.ClearUndo(graph);
                Object.DestroyImmediate(graph);
            }
        }

        [Test]
        public void LeafFallsBackToFullInspectorWhenIconDisappears()
        {
            CreateIcon();
            var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
            graph.LeafNodes.Add(new BtLeafNodeData { Guid = "leaf", Leaf = new IconLeaf() });
            var node = new BtNodeView("leaf", BtNodeKind.Leaf, "Icon leaf", null);
            try
            {
                BtEditorPreferences.UseNodeIcons = true;
                node.Bind(graph, _ => { }, false, () => { });
                Assert.That(node.style.width.value.value, Is.EqualTo(180));
                Assert.That(node.Q("context-warning-icon").style.display.value, Is.EqualTo(DisplayStyle.Flex));
                AssetDatabase.DeleteAsset(IconPath);
                BtLeafIconCache.Invalidate();
                node.RefreshPresentation();
                Assert.That(node.style.width.value.value, Is.EqualTo(320));
                Assert.That(node.extensionContainer.style.display.value, Is.EqualTo(DisplayStyle.Flex));
                Assert.That(node.extensionContainer.Q<IMGUIContainer>(), Is.Not.Null);
            }
            finally { node.DisposeInspector(); Object.DestroyImmediate(graph); }
        }

        [UnityTest]
        public IEnumerator LauncherContentFitsItsWindowAndLoadsBanner()
        {
            var window = ScriptableObject.CreateInstance<BtLauncherWindow>();
            try
            {
                BtCreationWindowUi.ShowCentered(window, new Vector2(440, 524));
                yield return null;
                yield return null;
                var root = window.rootVisualElement;
                Assert.That(root.Q<Image>().image, Is.Not.Null);
                var toggles = root.Query<Toggle>().ToList();
                Assert.That(toggles.Count, Is.EqualTo(2));
                Assert.That(toggles.Last().worldBound.yMax, Is.LessThanOrEqualTo(root.worldBound.yMax - 10));
                Assert.That(root.Query<Button>().ToList().Count, Is.EqualTo(4));
            }
            finally { window.Close(); }
        }

        private sealed class IconHost : EditorWindow { }
    }
}
