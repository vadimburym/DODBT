// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

#if ODIN_INSPECTOR
using System;
using System.Globalization;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VadimBurym.DodBehaviourTree;
using SerializationUtility = Sirenix.Serialization.SerializationUtility;
using Sirenix.OdinInspector.Editor;

internal sealed class BtEditorWindow : EditorWindow
{
    private BtGraphView _graphView;
    private BtNodeView _selectedNodeView;
    
    private ObjectField _graphAssetField;
    private Label _nodesCountLabel;
    private Label _leafNodesCountLabel;
    
    private TextField _nameField;
    private EnumField _kindField;
    private Label _childrenOrderLabel;
    private Label _childrenOrderTitleLabel;
    private Label _descriptionTitleLabel;
    private Label _descriptionTextLabel;
    private IMGUIContainer _odinDetailsContainer;
    private PropertyTree _selectedNodeTree;
    private string _selectedNodeTreeGuid;
    
    private ScrollView _outputScroll;
    private Label _outputLabel;
    
    private bool _isReadOnly;
    private BtMonoDebug _monoDebug;

    [MenuItem("Tools/VadimBurym/BT Editor")]
    public static void Open()
    {
        var window = GetWindow<BtEditorWindow>();
        window.titleContent = new GUIContent("BT Editor");
        window.minSize = new Vector2(1000, 650);
        window._isReadOnly = false;
        window.Enable();
        window.Show();
    }
    
    internal static void OpenWithAsset(BtGraphAsset asset)
    {
        var window = GetWindow<BtEditorWindow>();
        window.titleContent = new GUIContent("BT Editor");
        window.minSize = new Vector2(1000, 650);
        window._isReadOnly = false;
        window.Enable();
        window.Show();
        window.Focus();
        window.rootVisualElement.schedule.Execute(() => window.SetCurrentGraphAsset(asset));
    }

    internal static void OpenWithDebugMode(BtGraphAsset asset, BtMonoDebug monoDebug)
    {
        var window = GetWindow<BtEditorWindow>();
        window.titleContent = new GUIContent("BT Editor (Debug-Mode)");
        window.minSize = new Vector2(1000, 650);
        window._isReadOnly = true;
        window.EnableReadOnly();
        window.Show();
        window.Focus();
        window._monoDebug = monoDebug;
        window.rootVisualElement.schedule.Execute(() => window.SetCurrentGraphAsset(asset));
    }
    
    private void Enable()
    {
        rootVisualElement.Clear();
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;
        rootVisualElement.Add(root);
        
        var toolbar = new Toolbar();
        toolbar.style.alignItems = Align.Center;
        var buttonNew = new Button(OpenCreateNewAssetWindow) { text = "New" };
        var buttonDelete = new Button(OpenDeleteAssetWindow) { text = "Delete" };
        var buttonCopy = new Button(OpenCopyAssetWindow) { text = "Copy" };
        var buttonRename = new Button(OpenRenameAssetWindow) { text = "Rename" };
        toolbar.Add(buttonNew);
        toolbar.Add(buttonDelete);
        toolbar.Add(buttonCopy);
        toolbar.Add(buttonRename);
        toolbar.Add(new ToolbarSpacer());
        //var searchField = new ToolbarSearchField();
        //toolbar.Add(searchField);
        _graphAssetField = new ObjectField("Graph Asset") {
            objectType = typeof(BtGraphAsset),
            allowSceneObjects = false };
        _graphAssetField.style.unityFontStyleAndWeight = FontStyle.Bold;
        toolbar.Add(_graphAssetField);
        var compileButton = new Button(OnCompileClicked) { text = "Compile ▶" };
        compileButton.RegisterCallback<MouseEnterEvent>(_ => {
            compileButton.style.backgroundColor = new Color(0.40f, 0.70f, 0.45f, 1f); });
        compileButton.RegisterCallback<MouseLeaveEvent>(_ => {
            compileButton.style.backgroundColor = new Color(0.35f, 0.60f, 0.40f, 1f); });
        compileButton.style.marginLeft = 8;
        compileButton.style.backgroundColor = new Color(0.35f, 0.60f, 0.40f, 1f);
        compileButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        toolbar.Add(compileButton);
        toolbar.Add(new ToolbarSpacer());

        _nodesCountLabel = new Label("Nodes: 0");
        _nodesCountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _nodesCountLabel.style.marginLeft = 8;
        _nodesCountLabel.style.marginRight = 16;
        toolbar.Add(_nodesCountLabel);
        _leafNodesCountLabel = new Label("Leafs: 0");
        _leafNodesCountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _leafNodesCountLabel.style.marginRight = 8;
        toolbar.Add(_leafNodesCountLabel);
        root.Add(toolbar);
        
        var mainSplit = new TwoPaneSplitView(1, 360, TwoPaneSplitViewOrientation.Horizontal);
        mainSplit.style.flexGrow = 1;
        root.Add(mainSplit);

        _graphView = new BtGraphView(false);
        _graphView.NodeSelected += OnNodeSelected;
        _graphView.LogRequested += Log;
        _graphView.GraphStructureChanged += RefreshCounters;
        _graphView.NodeDataChanged += guid =>
        {
            if (_selectedNodeView != null && _selectedNodeView.Guid == guid)
            {
                RefreshDetails(_selectedNodeView);
                RebuildDetailsTreeIfNeeded(_selectedNodeView);
                _odinDetailsContainer?.MarkDirtyRepaint();
            }
        };
        _graphAssetField.RegisterValueChangedCallback(eventData =>
        {
            var asset = eventData.newValue as BtGraphAsset;
            _graphView.Bind(asset);
            ClearDetails();
            RefreshCounters();
        });
        
        mainSplit.Add(_graphView);
        mainSplit.Add(BuildDetailsPanel());
        root.Add(BuildOutputPanel());
        Log("Editor started.");
        Log("To start choose or create new GraphAsset.");
        Log("To create new node right click on canvas: Add/...");
    }

    private void EnableReadOnly()
    {
        rootVisualElement.Clear();
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Column;
        root.style.flexGrow = 1;
        rootVisualElement.Add(root);
        rootVisualElement.schedule
            .Execute(LateDebugUpdate)
            .Every(16);
        _graphView = new BtGraphView(true);
        _graphView.Add(BuildDebugModeBanner());
        root.Add(_graphView);
    }
    
    private void OnDisable()
    {
        if (_graphView != null)
        {
            _graphView.GraphStructureChanged -= RefreshCounters;
            _graphView.LogRequested -= Log;
            _graphView.GraphStructureChanged -= RefreshCounters;
        }
        DisposeDetailsTree();
    }

    public void LateDebugUpdate()
    {
        if (_graphView == null)
            return;
        if (_monoDebug == null)
            return;
        _graphView.SetDebugStatus(_monoDebug.DebugStatus);
    }
    
    private VisualElement BuildDebugModeBanner()
    {
        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.top = 10;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.alignItems = Align.Center;
        
        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Column;
        card.style.alignItems = Align.Center;
        card.style.paddingLeft = 10;
        card.style.paddingRight = 10;
        card.style.paddingTop = 8;
        card.style.paddingBottom = 8;
        card.style.borderTopLeftRadius = 10;
        card.style.borderTopRightRadius = 10;
        card.style.borderBottomLeftRadius = 10;
        card.style.borderBottomRightRadius = 10;
        card.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        card.style.borderLeftWidth = 1;
        card.style.borderRightWidth = 1;
        card.style.borderTopWidth = 1;
        card.style.borderBottomWidth = 1;
        card.style.borderLeftColor = new Color(1f, 1f, 1f, 0.12f);
        card.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);
        card.style.borderTopColor = new Color(1f, 1f, 1f, 0.12f);
        card.style.borderBottomColor = new Color(1f, 1f, 1f, 0.12f);
        
        var title = new Label("DEBUG-MODE");
        title.style.unityFontStyleAndWeight = FontStyle.Bold;
        title.style.fontSize = 13;
        title.style.color = new Color(1f, 1f, 1f, 0.95f);
        title.style.unityTextAlign = TextAnchor.MiddleCenter;
        card.Add(title);
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.marginTop = 6;
        card.Add(row);
        VisualElement Item(Color dotColor, string text)
        {
            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.alignItems = Align.Center;
            wrap.style.marginRight = 14;
            
            var dot = new VisualElement();
            dot.style.width = 10;
            dot.style.height = 10;
            dot.style.borderTopLeftRadius = 50;
            dot.style.borderTopRightRadius = 50;
            dot.style.borderBottomLeftRadius = 50;
            dot.style.borderBottomRightRadius = 50;
            dot.style.backgroundColor = dotColor;
            dot.style.marginRight = 6;

            var label = new Label(text);
            label.style.fontSize = 11;
            label.style.color = new Color(1f, 1f, 1f, 0.85f);

            wrap.Add(dot);
            wrap.Add(label);
            return wrap;
        }
        row.Add(Item(new Color(0.20f, 0.60f, 1.00f, 1f), "running"));  // синий
        row.Add(Item(new Color(0.20f, 0.85f, 0.30f, 1f), "success"));  // зелёный
        row.Add(Item(new Color(0.90f, 0.20f, 0.20f, 1f), "failure"));  // красный
        row.Add(Item(new Color(0.18f, 0.18f, 0.18f, 1f), "none"));     // тёмно-серый
        overlay.Add(card);
        return overlay;
    }
    
    private VisualElement BuildDetailsPanel()
    {
        var details = new VisualElement();
        details.style.paddingLeft = 10;
        details.style.paddingRight = 10;
        details.style.paddingTop = 10;
        details.style.paddingBottom = 10;
        details.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 1f);

        var headerLabel = new Label("Details");
        headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerLabel.style.fontSize = 14;
        headerLabel.style.marginBottom = 8;
        details.Add(headerLabel);
        
        _nameField = new TextField("Name");
        _nameField.SetEnabled(false);
        details.Add(_nameField);

        _kindField = new EnumField("Kind", BtNodeKind.None);
        _kindField.SetEnabled(false);
        details.Add(_kindField);

        _childrenOrderTitleLabel = new Label("Children order");
        _childrenOrderTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _childrenOrderTitleLabel.style.marginTop = 12;
        _childrenOrderTitleLabel.style.color = new Color(1f, 1f, 1f, 0.85f);
        details.Add(_childrenOrderTitleLabel);

        _childrenOrderLabel = new Label();
        _childrenOrderLabel.style.whiteSpace = WhiteSpace.Normal;
        _childrenOrderLabel.style.marginTop = 4; // чуть ниже заголовка
        _childrenOrderLabel.style.color = new Color(1f, 1f, 1f, 0.75f);
        details.Add(_childrenOrderLabel);
        
        var bigSpacer = new VisualElement();
        bigSpacer.style.height = 22;
        details.Add(bigSpacer);
        
        var nodeSettingsHeader = new Label("Node settings");
        nodeSettingsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        nodeSettingsHeader.style.fontSize = 13;
        nodeSettingsHeader.style.marginBottom = 8;
        nodeSettingsHeader.style.marginTop = 2;
        details.Add(nodeSettingsHeader);
        
        _odinDetailsContainer = new IMGUIContainer(DrawDetailsOdin);
        _odinDetailsContainer.style.marginTop = 2;
        details.Add(_odinDetailsContainer);
        
        var bigSpacer2 = new VisualElement();
        bigSpacer2.style.height = 22;
        details.Add(bigSpacer2);
        
        _descriptionTitleLabel = new Label("Description");
        _descriptionTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _descriptionTitleLabel.style.marginTop = 12;
        _descriptionTitleLabel.style.marginBottom = 4;
        _descriptionTextLabel = new Label();
        _descriptionTextLabel.style.whiteSpace = WhiteSpace.Normal;
        _descriptionTextLabel.style.marginBottom = 8;
        _descriptionTextLabel.style.color = new Color(1f, 1f, 1f, 0.85f);
        details.Add(_descriptionTitleLabel);
        details.Add(_descriptionTextLabel);
        
        _nameField.RegisterValueChangedCallback(eventData =>
        {
            if (_selectedNodeView == null)
                return;
            _selectedNodeView.SetTitle(eventData.newValue);
            var graphAsset = _graphView.GraphAsset;
            if (graphAsset == null)
                return;
            int headerIndex = graphAsset.FindHeaderIndex(_selectedNodeView.Guid);
            if (headerIndex < 0)
                return;
            Undo.RecordObject(graphAsset, "BT Rename Node");
            var header = graphAsset.Nodes[headerIndex];
            header.Title = eventData.newValue;
            graphAsset.Nodes[headerIndex] = header;
            graphAsset.MarkModified();
            //EditorUtility.SetDirty(graphAsset);
            _graphView.NotifyNodeDataChanged(_selectedNodeView.Guid);
        });
        return details;
    }

    private VisualElement BuildOutputPanel()
    {
        var bottom = new VisualElement();
        bottom.style.height = 170;
        bottom.style.flexShrink = 0;
        bottom.style.borderTopWidth = 1;
        bottom.style.borderTopColor = new Color(1f, 1f, 1f, 0.10f);
        bottom.style.backgroundColor = new Color(0.09f, 0.09f, 0.09f, 1f);
        bottom.style.paddingLeft = 10;
        bottom.style.paddingRight = 10;
        bottom.style.paddingTop = 8;
        bottom.style.paddingBottom = 8;

        var outTitle = new Label("Output");
        outTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
        outTitle.style.marginBottom = 6;
        bottom.Add(outTitle);

        _outputScroll = new ScrollView(ScrollViewMode.Vertical);
        _outputScroll.style.flexGrow = 1;
        _outputScroll.style.borderTopWidth = 1;
        _outputScroll.style.borderTopColor = new Color(1f, 1f, 1f, 0.06f);

        _outputLabel = new Label();
        _outputLabel.style.whiteSpace = WhiteSpace.Normal;
        _outputLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        _outputLabel.style.color = new Color(1f, 1f, 1f, 0.85f);

        _outputScroll.Add(_outputLabel);
        bottom.Add(_outputScroll);
        return bottom;
    }

    private void OnNodeSelected(BtNodeView node)
    {
        _selectedNodeView = node;
        RefreshDetails(node);
        RebuildDetailsTreeIfNeeded(node);
        _odinDetailsContainer?.MarkDirtyRepaint();
    }

    private void RefreshDetails(BtNodeView node)
    {
        if (node == null)
        {
            ClearDetails();
            return;
        }
        _nameField.SetEnabled(true);
        _kindField.SetEnabled(false);
        _nameField.SetValueWithoutNotify(node.title);
        _kindField.SetValueWithoutNotify(node.Kind);
        UpdateChildrenOrderInDetails(node);
        UpdateDescription(node);
    }

    private void UpdateDescription(BtNodeView node)
    {
        string description;
        switch (node.Kind)
        {
            case BtNodeKind.Root:
                description =
                    "Root node of the Behaviour Tree.\nOnly one root is allowed. It defines the entry point of execution.";
                break;
            case BtNodeKind.Sequence:
                description =
                    "Sequence executes children from left to right.\nFails on first Failure.\nSucceeds only if all children succeed.\nRunning on first Running.";
                break;
            case BtNodeKind.MemorySequence:
                description =
                    "MemorySequence executes children from left to right but resumes with remembered child.\n\nIf child Failure - Fails\nIf child Success - execute and remember next child. If there's no next child - Succeeds and reset remembered child.\nIf child Running - Running.\n\nResetOnFailure: If enable and child Failure - Reset remembered child.\nResetOnAbort: If enable and child Aborted - Reset remembered child.";
                break;
            case BtNodeKind.Selector:
                description =
                    "Selector executes children from left to right.\nSucceeds on first Success.\nFails only if all children fail.\nRunning on first Running.";
                break;
            case BtNodeKind.MemorySelector:
                description =
                    "MemorySelector executes children from letf to right but resumes with remembered child.\n\nIf remembered child Failure - reset remembered child and execute children from left to right.\nIf child Failure - execute next child. If there's no next child - Fails.\nIf child Success - Success and reset remembered child.\nIf child Running - Running and remember this child.\n\nPickRandom: If enable executes children in random order.\nResetOnAbort: If enable and child Aborted - Reset remembered child.";
                break;
            case BtNodeKind.Leaf:
                description = "Leaf node executes gameplay logic.\nIt can be Condition, Action, or Runnable action. Use your ILeaf implementation.";
                break;
            case BtNodeKind.Parallel:
                description = "Parallel executes children from left to right and counts number success and failure.\nIf success >= SuccessThreshold returns Success. \nIf failure >= FailureThreshold returns Failure. \nElse returns Running. Set Threshold = -1 if you want All. Set Threshold = 1 if you want Any.\n\nCacheChildStatus: If enable - cached failure/success child status and dont tick them until returns Failure/Success.";
                break;
            default:
                description = "";
                break;
        }
        _descriptionTextLabel.text = description;
    }
    
    private void UpdateChildrenOrderInDetails(BtNodeView nodeView)
    {
        var graphAsset = _graphView.GraphAsset;
        if (graphAsset == null || nodeView == null)
        {
            _childrenOrderLabel.text = "";
            return;
        }
        int headerIndex = graphAsset.FindHeaderIndex(nodeView.Guid);
        if (headerIndex < 0)
        {
            _childrenOrderLabel.text = "";
            return;
        }
        var header = graphAsset.Nodes[headerIndex];
        if (header.Kind != BtNodeKind.Sequence &&
            header.Kind != BtNodeKind.MemorySequence &&
            header.Kind != BtNodeKind.Selector &&
            header.Kind != BtNodeKind.MemorySelector &&
            header.Kind != BtNodeKind.Parallel)
        {
            _childrenOrderLabel.text = "<none>";
            return;
        }
        var children = graphAsset.GetChildrenList(header.Guid, header.Kind);
        if (children == null || children.Count == 0)
        {
            _childrenOrderLabel.text = "<none>";
            return;
        }
        var builder = new System.Text.StringBuilder();

        for (int index = 0; index < children.Count; index++)
        {
            var childHeader = graphAsset.FindHeader(children[index]);
            string childTitle = childHeader == null ? children[index] :
                (string.IsNullOrEmpty(childHeader.Title) ? childHeader.Guid : childHeader.Title);
            builder.Append(index);
            builder.Append(": ");
            builder.AppendLine(childTitle);
        }
        _childrenOrderLabel.text = builder.ToString();
    }

    private void ClearDetails()
    {
        _nameField.SetValueWithoutNotify(string.Empty);
        _kindField.SetValueWithoutNotify(BtNodeKind.None);
        _nameField.SetEnabled(false);
        _kindField.SetEnabled(false);
        _childrenOrderLabel.text = "";
        _descriptionTextLabel.text = "";
        DisposeDetailsTree();
        _odinDetailsContainer?.MarkDirtyRepaint();
    }


    private void DisposeDetailsTree()
    {
        _selectedNodeTree?.Dispose();
        _selectedNodeTree = null;
        _selectedNodeTreeGuid = null;
    }

    private void RebuildDetailsTreeIfNeeded(BtNodeView node)
    {
        if (node == null || _graphView.GraphAsset == null)
        {
            DisposeDetailsTree();
            return;
        }
        if (_selectedNodeTree != null && _selectedNodeTreeGuid == node.Guid)
            return;
        DisposeDetailsTree();
        var asset = _graphView.GraphAsset;
        if (node.Kind == BtNodeKind.Leaf)
        {
            var leafData = asset.FindLeafData(node.Guid);
            if (leafData != null)
            {
                _selectedNodeTree = PropertyTree.Create(leafData);
                _selectedNodeTreeGuid = node.Guid;
            }
        }
        else if (node.Kind == BtNodeKind.MemorySequence)
        {
            var memoryData = asset.FindMemorySequenceData(node.Guid);
            if (memoryData != null)
            {
                _selectedNodeTree = PropertyTree.Create(memoryData);
                _selectedNodeTreeGuid = node.Guid;
            }
        }
        else if (node.Kind == BtNodeKind.MemorySelector)
        {
            var memoryData = asset.FindMemorySelectorData(node.Guid);
            if (memoryData != null)
            {
                _selectedNodeTree = PropertyTree.Create(memoryData);
                _selectedNodeTreeGuid = node.Guid;
            }
        }
        else if (node.Kind == BtNodeKind.Parallel)
        {
            var parallelData = asset.FindParallelData(node.Guid);
            if (parallelData != null)
            {
                _selectedNodeTree = PropertyTree.Create(parallelData);
                _selectedNodeTreeGuid = node.Guid;
            }
        }
    }

    private void DrawDetailsOdin()
    {
        var asset = _graphView.GraphAsset;
        if (asset == null || _selectedNodeView == null)
            return;
        RebuildDetailsTreeIfNeeded(_selectedNodeView);
        if (_selectedNodeTree == null)
            return;
        Undo.RecordObject(asset, "BT Node Settings Change");
        _selectedNodeTree.BeginDraw(false);
        BtLeafNodeData leafData = null;
        string leafTypeBefore = null;
        if (_selectedNodeView.Kind == BtNodeKind.Leaf)
        {
            leafData = asset.FindLeafData(_selectedNodeView.Guid);
            leafTypeBefore = leafData?.Leaf == null ? null : leafData.Leaf.GetType().FullName;

            var leafProperty = _selectedNodeTree.RootProperty.Children["Leaf"];
            leafProperty?.Draw();
        }
        else if (_selectedNodeView.Kind == BtNodeKind.MemorySequence)
        {
            var resetOnFailureProperty = _selectedNodeTree.RootProperty.Children["ResetOnFailure"];
            var resetOnAbortProperty = _selectedNodeTree.RootProperty.Children["ResetOnAbort"];
            resetOnFailureProperty?.Draw();
            resetOnAbortProperty?.Draw();
        }
        else if (_selectedNodeView.Kind == BtNodeKind.MemorySelector)
        {
            var pickRandomProperty = _selectedNodeTree.RootProperty.Children["PickRandom"];
            var resetOnAbortProperty = _selectedNodeTree.RootProperty.Children["ResetOnAbort"];
            pickRandomProperty?.Draw();
            resetOnAbortProperty?.Draw();
        }
        else if (_selectedNodeView.Kind == BtNodeKind.Parallel)
        {
            var cacheChildProperty = _selectedNodeTree.RootProperty.Children["CacheChildStatus"];
            var FailureThresholdProperty = _selectedNodeTree.RootProperty.Children["FailureThreshold"];
            var SuccessThresholdProperty = _selectedNodeTree.RootProperty.Children["SuccessThreshold"];
            cacheChildProperty?.Draw();
            FailureThresholdProperty?.Draw();
            SuccessThresholdProperty?.Draw();
        }

        _selectedNodeTree.EndDraw();
        bool leafTypeChanged = false;
        if (_selectedNodeView.Kind == BtNodeKind.Leaf)
        {
            string leafTypeAfter = leafData?.Leaf == null ? null : leafData.Leaf.GetType().FullName;
            leafTypeChanged = leafTypeBefore != leafTypeAfter;
        }
        if (GUI.changed || leafTypeChanged)
        {
            asset.MarkModified();
            //EditorUtility.SetDirty(asset);
            if (leafTypeChanged && leafData?.Leaf != null)
                TryAutoRenameLeafTitleFromAsset(asset, _selectedNodeView.Guid);
            _graphView.NotifyNodeDataChanged(_selectedNodeView.Guid);
        }
    }

    private static void TryAutoRenameLeafTitleFromAsset(BtGraphAsset asset, string guid)
    {
        var leafData = asset.FindLeafData(guid);
        if (leafData == null || leafData.Leaf == null)
            return;
        string typeName = leafData.Leaf.GetType().Name;
        if (typeName.EndsWith("Leaf", StringComparison.Ordinal))
            typeName = typeName.Substring(0, typeName.Length - "Leaf".Length);
        int headerIndex = asset.FindHeaderIndex(guid);
        if (headerIndex < 0)
            return;
        var header = asset.Nodes[headerIndex];
        if (header.Title == typeName)
            return;
        header.Title = typeName;
        asset.Nodes[headerIndex] = header;
    }

    private void Log(string message, LogLevel level = LogLevel.Info, bool isImportant = false)
    {
        if (_outputLabel == null)
            return;
        string prefix;
        string coloredMessage;
        switch (level)
        {
            case LogLevel.Error:
                prefix = "[ERROR] ";
                coloredMessage = $"<color=#FF5555>{prefix}{message}</color>"; // красный
                break;
            case LogLevel.Warning:
                prefix = "[WARNING] ";
                coloredMessage = $"<color=#FFD54F>{prefix}{message}</color>"; // желтый
                break;
            default:
                prefix = "[INFO] ";
                if (isImportant)
                    coloredMessage = $"<color=#00BFFF>{prefix}{message}</color>"; // голубой
                else
                    coloredMessage = $"{prefix}{message}"; // обычный (белый)
                break;
        }

        _outputLabel.text += coloredMessage + "\n";
        _outputScroll.schedule.Execute(() =>
        {
            _outputScroll.verticalScroller.value = _outputScroll.verticalScroller.highValue;
        }).StartingIn(1);
    }
    
    private void OpenCreateNewAssetWindow()
    {
        BtCreateAssetWindow.Open(
            this,
            title: "Create BT Graph",
            initialName: "BtGraph",
            onConfirm: newAssetName =>
            {
                string folderPath = BtEditorPaths.GetEditorAssetsFolderPath();
                BtEditorPaths.EnsureFolderExists(folderPath);
                var asset = ScriptableObject.CreateInstance<BtGraphAsset>();
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{newAssetName}.asset");
                AssetDatabase.CreateAsset(asset, assetPath);
                
                var assetPathGuid = AssetDatabase.AssetPathToGUID(assetPath);
                
                folderPath = BtEditorPaths.GetAssetsFolderPath();
                BtEditorPaths.EnsureFolderExists(folderPath);
                var compiledAsset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
                assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{newAssetName}.asset");
                AssetDatabase.CreateAsset(compiledAsset, assetPath);
                
                asset.CompiledAsset = compiledAsset;
                compiledAsset.InternalGUID = assetPathGuid;
                asset.RootNode = new BtRootNodeData
                {
                    Guid = System.Guid.NewGuid().ToString("N"),
                    Position = Vector2.zero,
                    ChildrenGuid = null
                };
                asset.CreationDate = System.DateTime.Now.ToString("G", CultureInfo.CurrentCulture);
                asset.NodesCount = 0;
                asset.LeafNodesCount = 0;
                asset.MarkModified();
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                SetCurrentGraphAsset(asset);
                _graphView.ResetViewToOrigin();
                PingCompiledAsset(compiledAsset);
                Log($"Created new graph asset: {asset.name}", LogLevel.Info, isImportant: true);
            },
            message: "Enter a name for the new graph asset.");
    }

    private void OpenCopyAssetWindow()
    {
        if (_graphView.GraphAsset == null)
        {
            Log("Copy failed: current asset is null.", LogLevel.Error);
            return;
        }
        var sourceAsset = _graphView.GraphAsset;
        BtCreateAssetWindow.Open(
            this,
            title: "Copy BT Graph",
            initialName: $"{sourceAsset.name}_Copy",
            onConfirm: newAssetName =>
            {
                string folderPath = BtEditorPaths.GetEditorAssetsFolderPath();
                BtEditorPaths.EnsureFolderExists(folderPath);
                
                //var clone = SerializationUtility.DeserializeValue<BtGraphAsset>(
                //    SerializationUtility.SerializeValue(sourceAsset, DataFormat.Binary),
                //    DataFormat.Binary);
                var clone = UnityEngine.Object.Instantiate(sourceAsset);
                clone.name = newAssetName;
                clone.CreationDate = System.DateTime.Now.ToString("G", CultureInfo.CurrentCulture);
                string newPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{newAssetName}.asset");
                AssetDatabase.CreateAsset(clone, newPath);
                
                var assetPathGuid = AssetDatabase.AssetPathToGUID(newPath);
                
                folderPath = BtEditorPaths.GetAssetsFolderPath();
                BtEditorPaths.EnsureFolderExists(folderPath);
                //var cloneCompiled = SerializationUtility.DeserializeValue<BehaviourTreeAsset>(
                //    SerializationUtility.SerializeValue(sourceAsset, DataFormat.Binary),
                //    DataFormat.Binary);
                var cloneCompiled = UnityEngine.Object.Instantiate(sourceAsset.CompiledAsset);
                cloneCompiled.name = newAssetName;
                string newCompiledPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{newAssetName}.asset");
                AssetDatabase.CreateAsset(cloneCompiled, newCompiledPath);
                
                clone.CompiledAsset = cloneCompiled;
                cloneCompiled.InternalGUID = assetPathGuid;
                clone.NotActualCompiledVersion = clone.LastModifiedDate != clone.CompiledVersion;
                EditorUtility.SetDirty(clone);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                var loaded = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(newPath);
                if (loaded == null)
                {
                    Log("Copy failed: cannot load copied asset.", LogLevel.Error);
                    return;
                }

                SetCurrentGraphAsset(loaded);
                PingCompiledAsset(cloneCompiled);
                Log($"Copied graph asset: {loaded.name}", LogLevel.Info, isImportant: true);
            },
            message: "Enter a name for the copied graph asset.");
    }

    private void OpenRenameAssetWindow()
    {
        if (_graphView.GraphAsset == null)
        {
            Log("Rename failed: current asset is null.", LogLevel.Error);
            return;
        }
        var currentAsset = _graphView.GraphAsset;
        BtCreateAssetWindow.Open(
            this,
            title: "Rename BT Graph",
            initialName: currentAsset.name,
            onConfirm: newName =>
            {
                string oldPath = AssetDatabase.GetAssetPath(currentAsset);
                string oldPathComp = AssetDatabase.GetAssetPath(currentAsset.CompiledAsset);
                if (string.IsNullOrEmpty(oldPath))
                {
                    Log("Rename failed: cannot resolve asset path.", LogLevel.Error);
                    return;
                }
                string guid = AssetDatabase.AssetPathToGUID(oldPath);
                string error = AssetDatabase.RenameAsset(oldPath, newName);
                AssetDatabase.RenameAsset(oldPathComp, newName);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (!string.IsNullOrEmpty(error))
                {
                    Log($"Rename failed: {error}", LogLevel.Error);
                    return;
                }
                string newPath = AssetDatabase.GUIDToAssetPath(guid);
                var renamedAsset = AssetDatabase.LoadAssetAtPath<BtGraphAsset>(newPath);

                if (renamedAsset == null)
                {
                    SetCurrentGraphAsset(currentAsset);
                    Log("Rename succeeded, but failed to reload asset by GUID.", LogLevel.Warning);
                    return;
                }

                SetCurrentGraphAsset(renamedAsset);
                Log($"Renamed graph asset to: {renamedAsset.name}", LogLevel.Info, isImportant: true);
            },
            message: "Enter a new name for the current graph asset.");
    }

    private void OpenDeleteAssetWindow()
    {
        if (_graphView.GraphAsset == null)
        {
            Log("Delete failed: current asset is null.", LogLevel.Error);
            return;
        }
        BtConfirmDeleteWindow.Open(
            this,
            assetName: _graphView.GraphAsset.name,
            onYes: () =>
            {
                string assetPath = AssetDatabase.GetAssetPath(_graphView.GraphAsset);
                string assetPathComp = AssetDatabase.GetAssetPath(_graphView.GraphAsset.CompiledAsset);

                bool result = AssetDatabase.DeleteAsset(assetPath);
                bool resultComp = AssetDatabase.DeleteAsset(assetPathComp);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (!result)
                {
                    Log("Delete failed: AssetDatabase.DeleteAsset returned false.", LogLevel.Error);
                    return;
                }

                SetCurrentGraphAsset(null);
                Log("Graph asset deleted.", LogLevel.Info, isImportant: true);
            });
    }
    
    internal void SetCurrentGraphAsset(BtGraphAsset asset)
    {
        if (_isReadOnly)
        {
            _graphView.Bind(asset);
            return;
        }
        _graphAssetField.SetValueWithoutNotify(asset);
        _graphView.Bind(asset);
        ClearDetails();
        RefreshCounters();
    }
    
    internal void RefreshCounters()
    {
        var asset = _graphView?.GraphAsset;
        int nodes = asset != null ? asset.NodesCount : 0;
        int leafs = asset != null ? asset.LeafNodesCount : 0;
        _nodesCountLabel.text = $"Nodes: {nodes}";
        _leafNodesCountLabel.text = $"Leafs: {leafs}";
    }
    
    private void OnCompileClicked()
    {
        var graphAsset = _graphView?.GraphAsset;
        if (graphAsset == null)
        {
            Log("Compile failed: No graph selected.", LogLevel.Error);
            return;
        }
        if (graphAsset.CompiledAsset == null)
        {
            var folderPath = BtEditorPaths.GetAssetsFolderPath();
            BtEditorPaths.EnsureFolderExists(folderPath);
            var compiledAsset = ScriptableObject.CreateInstance<BehaviourTreeAsset>();
            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{graphAsset.name}.asset");
            AssetDatabase.CreateAsset(compiledAsset, assetPath);
            graphAsset.CompiledAsset = compiledAsset;
            compiledAsset.InternalGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(graphAsset));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        Log("Compile started...", LogLevel.Info, false);
        var error = BtCompiler.TryCompileAsset(graphAsset, graphAsset.CompiledAsset);
        if (!string.IsNullOrEmpty(error))
        {
            Log("Compile failed: " + error, LogLevel.Error);
            return;
        }
        Log($"Compilation {graphAsset.name} was successful!", LogLevel.Info, true);
        PingCompiledAsset(graphAsset.CompiledAsset);
        graphAsset.SetupNewCompiledVersion();
    }
    
    private void PingCompiledAsset(BehaviourTreeAsset compiledAsset)
    {
        if (compiledAsset == null)
            return;
        Selection.activeObject = compiledAsset;
        EditorGUIUtility.PingObject(compiledAsset);
    }
}

public enum LogLevel
{
    Info,
    Warning,
    Error
}
#else
using UnityEditor;
using UnityEngine;

namespace VadimBurym.DodBehaviourTree
{
    internal sealed class BtEditorFallbackWindow : EditorWindow
    {
        [MenuItem("Tools/VadimBurym/Bt Editor")]
        private static void Open()
        {
            var window = GetWindow<BtEditorFallbackWindow>("BT Editor");
            window.minSize = new Vector2(400, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(15);

            EditorGUILayout.HelpBox(
                "Odin Inspector is required for the full BehaviourTree editor experience.\n" +
                "Please install Odin Inspector to enable this inspector." +
                "If you have already installed Odin Inspector and this window did not disappear,\n" +
                "make sure that 'ODIN_INSPECTOR' is present in:\n" +
                "Project Settings → Player → Scripting Define Symbols.",
                MessageType.Info);

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Open Odin Inspector Page", GUILayout.Height(30)))
            {
                Application.OpenURL("https://odininspector.com/");
            }
        }
    }
}
#endif