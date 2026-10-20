// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using VadimBurym.DodBehaviourTree.Dots.Editor;
using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace VadimBurym.DodBehaviourTree.Dots.Tests
{
    public sealed class CreationWindowTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private string _folder;
        private BtCreationSettings _settings;

        [SetUp]
        public void Setup()
        {
            string name = "__DODBTCreation_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name);
            _folder = "Assets/" + name;
            _settings = new BtCreationSettings(name);
        }

        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset(_folder);
            string prefix = (string)typeof(BtCreationSettings).GetField("_prefix", Private).GetValue(_settings);
            foreach (string key in new[] { "Folder", "FolderGuid", "Namespace", "Context" }) EditorPrefs.DeleteKey(prefix + key);
        }

        [Test]
        public void FolderChoice_RejectsOutsideAssetsAndMissingFolders()
        {
            Assert.That(BtCreationSettings.TryAssetFolder(Path.GetFullPath(_folder), out string relative), Is.True);
            Assert.That(relative, Is.EqualTo(_folder));
            Assert.That(BtCreationSettings.TryAssetFolder("Assets/../ProjectSettings", out _), Is.False);
            Assert.That(BtCreationSettings.TryAssetFolder(Application.dataPath + "Other", out _), Is.False);
            Assert.That(BtCreationSettings.TryAssetFolder(_folder + "/Deleted", out _), Is.False);
            Assert.That(BtCreationSettings.TryAssetFolder("Packages", out _), Is.False);
        }

        [Test]
        public void SavedFolder_FollowsItsGuidAndFallsBackAfterDeletion()
        {
            _settings.Folder = _folder;
            string destination = _folder + "_Moved";
            Assert.That(AssetDatabase.MoveAsset(_folder, destination), Is.Empty);
            _folder = destination;
            Assert.That(_settings.Folder, Is.EqualTo(destination));
            AssetDatabase.DeleteAsset(destination);
            Assert.That(_settings.Folder, Is.EqualTo("Assets"));
        }

        [Test]
        public void SavedNamespace_RoundTripsAndMissingContextDoesNotSelectAnotherType()
        {
            _settings.Namespace = "Game.Custom.AI";
            Assert.That(_settings.Namespace, Is.EqualTo("Game.Custom.AI"));
            string prefix = (string)typeof(BtCreationSettings).GetField("_prefix", Private).GetValue(_settings);
            EditorPrefs.SetString(prefix + "Context", "DeletedAssembly:DeletedContext");
            Assert.That(_settings.Context, Is.Null);
        }

        [TestCase("../Outside")]
        [TestCase("a/b")]
        [TestCase("a\\b")]
        [TestCase("..")]
        [TestCase(" ")]
        [TestCase("Tree:")]
        [TestCase("NUL")]
        [TestCase("com1")]
        [TestCase("LPT9.asset")]
        public void GraphName_RejectsPathsAndInvalidFileNames(string name)
            => Assert.That(BtCreationSettings.GraphNameError(name), Is.Not.Null);

        [Test]
        public void LeafForm_RejectsFolderOutsideTheContextAssembly()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Leaf creation form interaction requires a graphics device.");
            var window = ScriptableObject.CreateInstance<Editor.LeafTemplateCreator>();
            try
            {
                var type = typeof(Editor.LeafTemplateCreator);
                type.GetField("_folder", Private).SetValue(window, _folder);
                type.GetField("_context", Private).SetValue(window, typeof(AuditContext));
                string path = _folder + "/NewLeaf.cs";
                Assert.That(type.GetMethod("Validate", Private).Invoke(window, new object[] { path }),
                    Is.EqualTo("Choose a folder in the same assembly as the context."));
                BtCreationWindowUi.ShowCentered(window, 4);
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                window.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return });
                Assert.That(File.Exists(path), Is.False);
                LogAssert.NoUnexpectedReceived();
            }
            finally { window.Close(); }
        }

        [Test]
        public void ContextCreation_RejectsDuplicateTypesAndSameContextNameAcrossNamespaces()
        {
            var types = new[] { typeof(AuditContext), typeof(SnapshotTestLeaf) };
            Assert.That(Editor.ContextTemplateCreator.NameConflict("AuditContext", "Other.AuditContext", types), Does.Contain("distinct names"));
            Assert.That(Editor.ContextTemplateCreator.NameConflict("SnapshotTestLeaf", typeof(SnapshotTestLeaf).FullName, types), Does.Contain("already exists"));
            Assert.That(Editor.ContextTemplateCreator.NameConflict("NewContext", "Other.NewContext", types), Is.Null);
        }

        [Test]
        public void ContextForm_DrawsAndRejectsEditorFolder()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Context form interaction requires a graphics device.");
            AssetDatabase.CreateFolder(_folder, "Editor");
            var window = ScriptableObject.CreateInstance<Editor.ContextTemplateCreator>();
            try
            {
                var type = typeof(Editor.ContextTemplateCreator);
                type.GetField("_folder", Private).SetValue(window, _folder + "/Editor");
                string path = _folder + "/Editor/NewContext.cs";
                Assert.That(type.GetMethod("Validate", Private).Invoke(window, new object[] { path }), Does.Contain("runtime assembly"));
                BtCreationWindowUi.ShowCentered(window, 3);
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                window.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return });
                Assert.That(File.Exists(path), Is.False);
                LogAssert.NoUnexpectedReceived();
            }
            finally { window.Close(); }
        }

        [Test]
        public void GraphForm_SubmitsPathAndContext_AndRejectsExistingFile()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Creation form interaction requires a graphics device.");
            int calls = 0;
            var window = ScriptableObject.CreateInstance<BtCreateGraphWindow>();
            try
            {
                typeof(BtCreateGraphWindow).GetField("_folder", Private).SetValue(window, _folder);
                typeof(BtCreateGraphWindow).GetField("_context", Private).SetValue(window, typeof(AuditContext));
                typeof(BtCreateGraphWindow).GetField("_create", Private).SetValue(window, new Func<string, Type, bool>((path, context) =>
                {
                    Assert.That(path, Is.EqualTo(_folder + "/BtGraph.asset"));
                    Assert.That(context, Is.EqualTo(typeof(AuditContext)));
                    calls++;
                    return false;
                }));
                BtCreationWindowUi.ShowCentered(window, 3);
                Assert.That(window.position.size, Is.EqualTo(window.minSize));
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.Repaint });
                window.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return });
                Assert.That(calls, Is.EqualTo(1));
                var graph = ScriptableObject.CreateInstance<BtGraphAsset>();
                AssetDatabase.CreateAsset(graph, _folder + "/BtGraph.asset");
                window.SendEvent(new Event { type = EventType.Layout });
                window.SendEvent(new Event { type = EventType.KeyDown, keyCode = KeyCode.Return });
                Assert.That(calls, Is.EqualTo(1), "Create must not overwrite an existing asset.");
                LogAssert.NoUnexpectedReceived();
            }
            finally { window.Close(); }
        }

        [Test]
        public void GraphCreation_UsesChosenFolderForGraphAndLinkedCompiledAsset()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Graph editor creation requires a graphics device.");
            var editor = ScriptableObject.CreateInstance<BtEditorWindow>();
            try
            {
                editor.ShowUtility();
                editor.CreateGUI();
                string path = _folder + "/NewGraph.asset";
                var result = typeof(BtEditorWindow).GetMethod("CreateNewAsset", Private).Invoke(editor, new object[] { path, typeof(AuditContext) });
                Assert.That(result, Is.EqualTo(true));
                var graph = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(path);
                Assert.That(graph.ContextIdentity, Is.EqualTo(BtGraphContext.Identity(typeof(AuditContext))));
                Assert.That(AssetDatabase.GetAssetPath(graph.CompiledAsset), Is.EqualTo(_folder + "/NewGraph_Compiled.asset"));
                Assert.That(graph.CompiledAsset.InternalGUID, Is.EqualTo(AssetDatabase.AssetPathToGUID(path)));
                LogAssert.NoUnexpectedReceived();
            }
            finally { editor.Close(); }
        }
    }
}
