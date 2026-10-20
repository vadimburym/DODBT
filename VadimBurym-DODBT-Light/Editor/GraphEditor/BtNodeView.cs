// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{

    using System;
    using UnityEditor;
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;
    using VadimBurym.DodBehaviourTree.Light;
    using Node = UnityEditor.Experimental.GraphView.Node;


    internal sealed class BtNodeView : Node
    {
        public static readonly Vector2 DefaultSize = new Vector2(320, 50);
        private const float PortPillWidth = 56f;
        private const float PortPillHeight = 15f;
    
        public string Guid { get; }
        public string Title;
        public BtNodeKind Kind { get; }
        internal event Action<BtNodeView, Vector2> PositionChanged;

        public override void SetPosition(Rect newPos)
        {
            Vector2 previous = GetPosition().position;
            base.SetPosition(newPos);
            if (previous != newPos.position) PositionChanged?.Invoke(this, newPos.position);
        }

        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        private BtGraphAsset _graphAsset;
        private Action<string> _onDataChanged;
        private Action _onPortsColorChanged;
    
        private IntegerField _integerFieldOne;
        private IntegerField _integerFieldTwo;
        private Toggle _boolToggleOne;
        private Toggle _boolToggleTwo;

        private VisualElement _outputVisual;
        private VisualElement _inputVisual;
    
        private IMGUIContainer _leafContainer;
        private BtNodeInspector _leafInspector;
    
        private bool _isReadOnly;
        private VisualElement _groupOutline;
        private VisualElement _groupIcon;
        private bool _isGroupRoot;
        private bool _hasContextMismatch;
        private Label _contextWarning;
        private VisualElement _contextWarningOutline;
        private Image _contextWarningIcon;
        private Color _currentDebugColor = new Color(0.10f, 0.70f, 1.00f, 1f);
        private NodeStatus? _lastDebugStatus;
    
        internal BtNodeView(string guid, BtNodeKind kind, string title, IEdgeConnectorListener edgeConnectorListener)
        {
            Guid = guid;
            Kind = kind;
            this.title = title;
            Title = title;
        
            titleContainer.style.justifyContent = Justify.Center;
            titleContainer.style.alignItems = Align.Center;

            var titleLabel = _presentationTitle = titleContainer.Q<Label>();
            if (titleLabel != null)
            {
                titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                titleLabel.style.fontSize = 17;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
        
            style.width = 320;
            style.minHeight = 50;
            style.overflow = Overflow.Visible;
            mainContainer.style.overflow = Overflow.Visible;
            titleContainer.style.overflow = Overflow.Visible;
            extensionContainer.style.overflow = Overflow.Visible;
            //style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
            //mainContainer.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
            contentContainer.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
            extensionContainer.style.backgroundColor = new Color(0.14f, 0.14f, 0.14f, 1f);
            extensionContainer.style.borderBottomLeftRadius = 11;
            extensionContainer.style.borderBottomRightRadius = 11;
  
            style.borderTopLeftRadius = 12;
            style.borderTopRightRadius = 12;
            style.borderBottomLeftRadius = 12;
            style.borderBottomRightRadius = 12;

            mainContainer.style.borderTopLeftRadius = 12;
            mainContainer.style.borderTopRightRadius = 12;
            mainContainer.style.borderBottomLeftRadius = 12;
            mainContainer.style.borderBottomRightRadius = 12;

            contentContainer.style.borderBottomLeftRadius = 12;
            contentContainer.style.borderBottomRightRadius = 12;

            titleContainer.style.backgroundColor = new Color(0.10f, 0.10f, 0.10f, 1f);
            titleContainer.style.borderTopLeftRadius = 11;
            titleContainer.style.borderTopRightRadius = 11;
            titleContainer.style.height = 50;
        
            mainContainer.style.borderTopWidth = 4;
            mainContainer.style.borderTopColor = kind switch
            {
                BtNodeKind.Leaf => new Color(0.95f, 0.35f, 0.35f),
                _ => new Color(0.75f, 0.55f, 0.95f)
            };

            inputContainer.Clear();
            outputContainer.Clear();

            if (kind == BtNodeKind.Root)
            {
                capabilities &= ~Capabilities.Deletable;
            }
            if (kind != BtNodeKind.Leaf)
            {
                OutputPort = CreateHiddenPort(Direction.Output,
                    kind == BtNodeKind.Root || BtNodeKinds.IsDecorator(kind) ? Port.Capacity.Single : Port.Capacity.Multi,
                    edgeConnectorListener, false);
                _outputVisual = CreatePortVisual(isInput: false);
                OutputPort.RegisterCallback<MouseEnterEvent>(_ => _outputVisual.AddToClassList("is-hover"));
                OutputPort.RegisterCallback<MouseLeaveEvent>(_ => _outputVisual.RemoveFromClassList("is-hover"));
                VisualElement outputHost = CreatePortHost(isTop: false);
                outputHost.Add(OutputPort);
                outputHost.Add(_outputVisual);
                Add(outputHost);
            }
            if (kind != BtNodeKind.Root)
            {
                InputPort = CreateHiddenPort(Direction.Input, Port.Capacity.Single, edgeConnectorListener, isTop: true);
                _inputVisual = CreatePortVisual(isInput: true);
                InputPort.RegisterCallback<MouseEnterEvent>(_ => _inputVisual.AddToClassList("is-hover"));
                InputPort.RegisterCallback<MouseLeaveEvent>(_ => _inputVisual.RemoveFromClassList("is-hover"));
                VisualElement inputHost = CreatePortHost(isTop: true);
                inputHost.Add(InputPort);
                inputHost.Add(_inputVisual);
                Add(inputHost);
            }
        
            RefreshPorts();
            RefreshExpandedState();
            RegisterCallback<DetachFromPanelEvent>(_ => DisposeNodeTrees());
        }

        private Label _presentationTitle;
        private VisualElement _nodeIcon;
        private Image _leafIcon;
        private bool _compactPresentation;

        internal void RefreshPresentation()
        {
            bool builtIn = Kind == BtNodeKind.Selector || Kind == BtNodeKind.Sequence ||
                Kind == BtNodeKind.MemorySelector || Kind == BtNodeKind.MemorySequence || Kind == BtNodeKind.Parallel;
            Texture2D texture = BtEditorPreferences.UseNodeIcons && Kind == BtNodeKind.Leaf
                ? BtLeafIconCache.Get(_graphAsset?.FindLeafData(Guid)?.Leaf?.GetType()) : null;
            bool compact = BtEditorPreferences.UseNodeIcons && (builtIn || texture != null);
            if (compact && _nodeIcon == null)
            {
                _nodeIcon = builtIn ? new BtNodeGlyph(Kind) : (_leafIcon = new Image { scaleMode = ScaleMode.ScaleToFit });
                _nodeIcon.name = "bt-node-icon";
                _nodeIcon.pickingMode = PickingMode.Ignore;
                _nodeIcon.style.width = 56;
                // Trim empty space below the single arrow, keeping the caption close to the symbol.
                _nodeIcon.style.height = Kind == BtNodeKind.Sequence || Kind == BtNodeKind.MemorySequence ? 34 : 48;
                _nodeIcon.style.flexShrink = 0;
                _nodeIcon.style.marginTop = 0;
                _nodeIcon.style.marginBottom = 0;
                titleContainer.Insert(0, _nodeIcon);
            }
            if (_leafIcon != null) _leafIcon.image = texture;
            if (_nodeIcon != null) _nodeIcon.style.display = compact ? DisplayStyle.Flex : DisplayStyle.None;
            _compactPresentation = compact;
            style.width = compact ? 180 : DefaultSize.x;
            titleContainer.style.height = compact ? 78 : 50;
            titleContainer.style.flexDirection = compact ? FlexDirection.Column : FlexDirection.Row;
            extensionContainer.style.display = compact && Kind == BtNodeKind.Leaf || extensionContainer.childCount == 0
                ? DisplayStyle.None : DisplayStyle.Flex;
            StyleInlineField(_boolToggleOne, compact);
            StyleInlineField(_boolToggleTwo, compact);
            StyleInlineField(_integerFieldOne, compact);
            StyleInlineField(_integerFieldTwo, compact);
            if (_presentationTitle != null)
            {
                _presentationTitle.style.fontSize = compact ? 11 : 17;
                _presentationTitle.style.unityFontStyleAndWeight = compact ? FontStyle.Normal : FontStyle.Bold;
                _presentationTitle.style.height = compact ? new StyleLength(14) : new StyleLength(StyleKeyword.Null);
                _presentationTitle.style.marginTop = compact ? new StyleLength(0f) : new StyleLength(StyleKeyword.Null);
                _presentationTitle.style.marginBottom = compact ? new StyleLength(0f) : new StyleLength(StyleKeyword.Null);
                _presentationTitle.style.flexGrow = compact ? 0 : 1;
                _presentationTitle.style.maxWidth = Length.Percent(100);
                _presentationTitle.style.overflow = Overflow.Hidden;
                _presentationTitle.style.textOverflow = TextOverflow.Ellipsis;
                _presentationTitle.tooltip = title;
            }
            UpdateTitleAdornments();
        }

        private static void StyleInlineField<T>(BaseField<T> field, bool compact)
        {
            if (field == null) return;
            field.style.fontSize = compact ? new StyleLength(11) : new StyleLength(StyleKeyword.Null);
            // Unity's default label minimum is too wide for a 180px node, especially IntegerField.
            field.labelElement.style.minWidth = compact ? new StyleLength(0f) : new StyleLength(StyleKeyword.Null);
            field.labelElement.style.width = compact ? new StyleLength(112) : new StyleLength(StyleKeyword.Null);
        }

        internal void SetSearchMatch(bool match)
        {
            titleContainer.style.backgroundColor = match ? new Color(.38f, .30f, .08f) : new Color(.10f, .10f, .10f);
        }

        internal void SetGroupRoot(bool isRoot, string groupName)
        {
            _isGroupRoot = isRoot;
            if (_groupIcon == null)
            {
                _groupIcon = new VisualElement { name = "linked-group-icon", pickingMode = PickingMode.Ignore };
                _groupIcon.style.position = Position.Absolute;
                _groupIcon.style.right = 9; _groupIcon.style.top = 8;
                _groupIcon.style.width = 20; _groupIcon.style.height = 20;
                _groupIcon.generateVisualContent += context =>
                {
                    var painter = context.painter2D;
                    painter.strokeColor = Color.white; painter.lineWidth = 1.5f;
                    for (int n = 0; n < 2; n++)
                    {
                        float x = 3 + n * 5, y = 7 - n * 5;
                        painter.BeginPath(); painter.MoveTo(new Vector2(x, y));
                        painter.LineTo(new Vector2(x + 10, y)); painter.LineTo(new Vector2(x + 10, y + 10));
                        painter.LineTo(new Vector2(x, y + 10)); painter.ClosePath(); painter.Stroke();
                    }
                };
                titleContainer.Add(_groupIcon);
            }
            _groupIcon.style.display = isRoot ? DisplayStyle.Flex : DisplayStyle.None;
            titleContainer.tooltip = isRoot ? "Linked group: " + groupName + ". Changes sync to every instance." : null;
            UpdateTitleAdornments();
        }

        private void UpdateTitleAdornments()
        {
            int icons = (_isGroupRoot ? 1 : 0) + (_hasContextMismatch ? 1 : 0);
            float padding = icons == 0 ? 0 : 12 + icons * 24 + (_hasContextMismatch ? 10 : 0);
            // Keep corner adornments clear of the centered glyph and title.
            titleContainer.style.paddingLeft = titleContainer.style.paddingRight = _compactPresentation ? 8 : padding;
            if (_groupIcon != null)
            {
                _groupIcon.style.left = StyleKeyword.Auto;
                _groupIcon.style.right = _hasContextMismatch ? 45 : 9;
                if (_compactPresentation)
                {
                    _groupIcon.style.left = 9;
                    _groupIcon.style.right = StyleKeyword.Auto;
                }
            }
        }

        internal void SetGroupHighlight(bool highlighted)
        {
            if (_groupOutline == null)
            {
                _groupOutline = new VisualElement { name = "linked-group-outline", pickingMode = PickingMode.Ignore };
                _groupOutline.style.position = Position.Absolute;
                _groupOutline.style.left = -5; _groupOutline.style.right = -5;
                _groupOutline.style.top = -5; _groupOutline.style.bottom = -5;
                _groupOutline.style.borderTopLeftRadius = _groupOutline.style.borderTopRightRadius = 15;
                _groupOutline.style.borderBottomLeftRadius = _groupOutline.style.borderBottomRightRadius = 15;
                _groupOutline.style.borderTopWidth = _groupOutline.style.borderBottomWidth = 2;
                _groupOutline.style.borderLeftWidth = _groupOutline.style.borderRightWidth = 2;
                var color = new Color(1f, .8f, .15f);
                _groupOutline.style.borderTopColor = _groupOutline.style.borderBottomColor = color;
                _groupOutline.style.borderLeftColor = _groupOutline.style.borderRightColor = color;
                Add(_groupOutline);
            }
            _groupOutline.style.display = highlighted ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetDebugStatus(NodeStatus status)
        {
            if (_lastDebugStatus == status) return;
            bool firstStatus = !_lastDebugStatus.HasValue;
            _lastDebugStatus = status;
            switch (status)
            {
                case NodeStatus.Success:
                    _currentDebugColor = new Color(0.2f, 0.85f, 0.3f); // green
                    break;
                case NodeStatus.Failure:
                    _currentDebugColor = new Color(1.0f, 0.1f, 0.1f); // red
                    break;
                case NodeStatus.Running:
                    _currentDebugColor = new Color(0.2f, 0.3f, 1.0f); // yellow
                    break;
                case NodeStatus.None:
                default:
                    _currentDebugColor = new Color(0.1f, 0.1f, 0.1f); // blue
                    break;
            }

            if (InputPort != null) InputPort.portColor = _currentDebugColor;
            if (OutputPort != null) OutputPort.portColor = _currentDebugColor;

            InputPort?.MarkDirtyRepaint();
            OutputPort?.MarkDirtyRepaint();
        
            if (firstStatus)
            {
                style.borderLeftWidth = 6;
                style.borderRightWidth = 6;
                style.borderTopWidth = 6;
                style.borderBottomWidth = 6;
            }

            style.borderLeftColor = _currentDebugColor;
            style.borderRightColor = _currentDebugColor;
            style.borderTopColor = _currentDebugColor;
            style.borderBottomColor = _currentDebugColor;
        
            MarkDirtyRepaint();
            // Only the color changed; rebuilding ports triggers layout and redraws
            // every inline inspector even when the tree's structure is unchanged.
            _onPortsColorChanged?.Invoke();
        }
    
        private static void ForceRepaintEdges(Port port)
        {
            if (port == null)
                return;
            foreach (var c in port.connections)
            {
                if (c is not Edge edge)
                    continue;
                edge.MarkDirtyRepaint();
                edge.edgeControl?.MarkDirtyRepaint();
                edge.parent?.MarkDirtyRepaint();
            }
            port.MarkDirtyRepaint();
            port.parent?.MarkDirtyRepaint();
        }
    
        public void Bind(BtGraphAsset graphAsset, Action<string> onDataChanged, bool isReadOnly, Action onPortsColorChanged)
        {
            DisposeInspector();
            _isReadOnly = isReadOnly;
            _graphAsset = graphAsset;
            _onDataChanged = onDataChanged;
            _onPortsColorChanged = onPortsColorChanged;

            if (_isReadOnly)
            {
                capabilities &= ~Capabilities.Movable;
                capabilities &= ~Capabilities.Deletable;
                if (_inputVisual != null)
                    _inputVisual.style.display = DisplayStyle.None;
                if (_outputVisual != null)
                    _outputVisual.style.display = DisplayStyle.None;
            }
        
            BuildInlineInspectorUI();
            RefreshFromAsset();
        }

        public void RefreshFromAsset()
        {
            if (_graphAsset == null)
                return;
        
            int headerIndex = _graphAsset.FindHeaderIndex(Guid);
            if (headerIndex >= 0)
            {
                BtNodeHeader header = _graphAsset.Nodes[headerIndex];
                if (!string.IsNullOrEmpty(header.Title) && title != header.Title)
                    SetTitle(header.Title);
            }

            if (Kind == BtNodeKind.MemorySequence)
            {
                BtMemorySequenceNodeData data = _graphAsset.FindMemorySequenceData(Guid);
                if (data != null)
                {
                    _boolToggleOne?.SetValueWithoutNotify(data.ResetOnFailure);
                    _boolToggleTwo?.SetValueWithoutNotify(data.ResetOnAbort);
                }
            }
            else if (Kind == BtNodeKind.MemorySelector)
            {
                BtMemorySelectorNodeData data = _graphAsset.FindMemorySelectorData(Guid);
                if (data != null)
                {
                    _boolToggleOne?.SetValueWithoutNotify(data.PickRandom);
                    _boolToggleTwo?.SetValueWithoutNotify(data.ResetOnAbort);
                }
            }
            else if (Kind == BtNodeKind.Parallel)
            {
                BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
                if (data != null)
                {
                    _boolToggleOne?.SetValueWithoutNotify(data.CacheChildStatus);
                    _integerFieldOne?.SetValueWithoutNotify(data.FailureThreshold);
                    _integerFieldTwo?.SetValueWithoutNotify(data.SuccessThreshold);
                }
            }
            if (Kind == BtNodeKind.Leaf)
            {
                var leaf = _graphAsset.FindLeafData(Guid)?.Leaf;
                bool mismatch = !_isReadOnly && leaf != null && !BtGraphContext.AcceptsLeaf(_graphAsset, leaf.GetType());
                if (mismatch && _contextWarning == null)
                {
                    _contextWarning = new Label("Context / state mismatch");
                    _contextWarning.style.color = new Color(1, .75f, .3f);
                    _contextWarning.style.paddingLeft = 8;
                    _contextWarning.style.paddingBottom = 4;
                    mainContainer.Add(_contextWarning);

                    // A separate outline preserves selection/debug borders and does not resize the node.
                    _contextWarningOutline = new VisualElement { name = "context-warning-outline", pickingMode = PickingMode.Ignore };
                    _contextWarningOutline.style.position = Position.Absolute;
                    _contextWarningOutline.style.left = _contextWarningOutline.style.right = -2;
                    _contextWarningOutline.style.top = _contextWarningOutline.style.bottom = -2;
                    _contextWarningOutline.style.borderTopLeftRadius = _contextWarningOutline.style.borderTopRightRadius = 14;
                    _contextWarningOutline.style.borderBottomLeftRadius = _contextWarningOutline.style.borderBottomRightRadius = 14;
                    _contextWarningOutline.style.borderTopWidth = _contextWarningOutline.style.borderBottomWidth = 4;
                    _contextWarningOutline.style.borderLeftWidth = _contextWarningOutline.style.borderRightWidth = 4;
                    var orange = new Color(1f, .55f, .12f);
                    _contextWarningOutline.style.borderTopColor = _contextWarningOutline.style.borderBottomColor = orange;
                    _contextWarningOutline.style.borderLeftColor = _contextWarningOutline.style.borderRightColor = orange;
                    Add(_contextWarningOutline);

                    _contextWarningIcon = new Image
                    {
                        name = "context-warning-icon",
                        image = EditorGUIUtility.IconContent("console.warnicon").image,
                        scaleMode = ScaleMode.ScaleToFit
                    };
                    _contextWarningIcon.style.position = Position.Absolute;
                    _contextWarningIcon.style.right = 10;
                    _contextWarningIcon.style.top = 8;
                    _contextWarningIcon.style.width = _contextWarningIcon.style.height = 30;
                    titleContainer.Add(_contextWarningIcon);
                }
                if (_contextWarning != null)
                {
                    _contextWarning.style.display = _contextWarningOutline.style.display = _contextWarningIcon.style.display =
                        mismatch ? DisplayStyle.Flex : DisplayStyle.None;
                    _contextWarning.tooltip = _contextWarningIcon.tooltip = mismatch ? "Choose a leaf matching this graph's context and leaf state, or change the selected pair." : "";
                }
                _hasContextMismatch = mismatch;
                UpdateTitleAdornments();
                _leafContainer?.MarkDirtyRepaint();
            }
            if (BtNodeKinds.IsDecorator(Kind))
            {
                _leafContainer?.MarkDirtyRepaint();
            }
            RefreshPresentation();
        }

        public void SetTitle(string newTitle)
        {
            title = newTitle;
        }

        private void BuildInlineInspectorUI()
        {
            extensionContainer.Clear();
            extensionContainer.style.paddingLeft = 8;
            extensionContainer.style.paddingRight = 8;
            extensionContainer.style.paddingTop = 6;
            extensionContainer.style.paddingBottom = 6;

            bool allowEdit = !_isReadOnly;
            if (Kind == BtNodeKind.MemorySequence)
            {
                _boolToggleOne = new Toggle("Reset On Failure");
                _boolToggleTwo = new Toggle("Reset On Abort");
                _boolToggleOne.SetEnabled(allowEdit);
                _boolToggleTwo.SetEnabled(allowEdit);
                _boolToggleOne.RegisterValueChangedCallback(eventData =>
                {
                    if (_graphAsset == null) return;
                    BtMemorySequenceNodeData data = _graphAsset.FindMemorySequenceData(Guid);
                    if (data == null) return;
                    Undo.RecordObject(_graphAsset, "BT MemorySequence Change");
                    data.ResetOnFailure = eventData.newValue;
                    EditorUtility.SetDirty(_graphAsset);
                    _onDataChanged?.Invoke(Guid);
                });
                _boolToggleTwo.RegisterValueChangedCallback(eventData =>
                {
                    if (_graphAsset == null) return;
                    BtMemorySequenceNodeData data = _graphAsset.FindMemorySequenceData(Guid);
                    if (data == null) return;
                    Undo.RecordObject(_graphAsset, "BT MemorySequence Change");
                    data.ResetOnAbort = eventData.newValue;
                    EditorUtility.SetDirty(_graphAsset);
                    _onDataChanged?.Invoke(Guid);
                });
                extensionContainer.Add(_boolToggleOne);
                extensionContainer.Add(_boolToggleTwo);
                RefreshExpandedState();
                RefreshPorts();
                return;
            }
            if (Kind == BtNodeKind.MemorySelector)
            {
                _boolToggleOne = new Toggle("Pick Random");
                _boolToggleTwo = new Toggle("Reset On Abort");
                _boolToggleOne.SetEnabled(allowEdit);
                _boolToggleTwo.SetEnabled(allowEdit);
                _boolToggleOne.RegisterValueChangedCallback(eventData =>
                {
                    if (_graphAsset == null) return;
                    BtMemorySelectorNodeData data = _graphAsset.FindMemorySelectorData(Guid);
                    if (data == null) return;
                    Undo.RecordObject(_graphAsset, "BT MemorySelector Change");
                    data.PickRandom = eventData.newValue;
                    EditorUtility.SetDirty(_graphAsset);
                    _onDataChanged?.Invoke(Guid);
                });

                _boolToggleTwo.RegisterValueChangedCallback(eventData =>
                {
                    if (_graphAsset == null) return;
                    BtMemorySelectorNodeData data = _graphAsset.FindMemorySelectorData(Guid);
                    if (data == null) return;
                    Undo.RecordObject(_graphAsset, "BT MemorySelector Change");
                    data.ResetOnAbort = eventData.newValue;
                    EditorUtility.SetDirty(_graphAsset);
                    _onDataChanged?.Invoke(Guid);
                });
                extensionContainer.Add(_boolToggleOne);
                extensionContainer.Add(_boolToggleTwo);
                RefreshExpandedState();
                RefreshPorts();
                return;
            }
            if (Kind == BtNodeKind.Parallel)
            {
                _boolToggleOne = new Toggle("Cache Child Status");
                _boolToggleOne.SetEnabled(allowEdit);
                _boolToggleOne.RegisterValueChangedCallback(eventData =>
                {
                    if (_graphAsset == null) return;
                    BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
                    if (data == null) return;
                    Undo.RecordObject(_graphAsset, "BT Parallel Change");
                    data.CacheChildStatus = eventData.newValue;
                    EditorUtility.SetDirty(_graphAsset);
                    _onDataChanged?.Invoke(Guid);
                });
                _integerFieldOne = new IntegerField("Failure Threshold");
                _integerFieldOne.SetEnabled(allowEdit);
                _integerFieldOne.RegisterValueChangedCallback(eventData =>
                {
                    if (_graphAsset == null) return;
                    BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
                    if (data == null) return;
                    Undo.RecordObject(_graphAsset, "BT Parallel Change");
                    data.FailureThreshold = eventData.newValue;
                    EditorUtility.SetDirty(_graphAsset);
                    _onDataChanged?.Invoke(Guid);
                });
                _integerFieldTwo = new IntegerField("Success Threshold");
                _integerFieldTwo.SetEnabled(allowEdit);
                _integerFieldTwo.RegisterValueChangedCallback(eventData =>
                {
                    if (_graphAsset == null) return;
                    BtParallelNodeData data = _graphAsset.FindParallelData(Guid);
                    if (data == null) return;
                    Undo.RecordObject(_graphAsset, "BT Parallel Change");
                    data.SuccessThreshold = eventData.newValue;
                    EditorUtility.SetDirty(_graphAsset);
                    _onDataChanged?.Invoke(Guid);
                });
                extensionContainer.Add(_boolToggleOne);
                extensionContainer.Add(_integerFieldOne);
                extensionContainer.Add(_integerFieldTwo);
                RefreshExpandedState();
                RefreshPorts();
                return;
            }
            if (Kind == BtNodeKind.Leaf || BtNodeKinds.IsDecorator(Kind))
            {
                _leafContainer = new IMGUIContainer(DrawLeafInNode) { name = "inline-node-inspector" };
                _leafContainer.RegisterCallback<MouseMoveEvent>(_ => _leafContainer.MarkDirtyRepaint());
                _leafContainer.RegisterCallback<MouseLeaveEvent>(_ => _leafContainer.MarkDirtyRepaint());
                _leafContainer.style.marginTop = 2;
                _leafContainer.SetEnabled(allowEdit);
                extensionContainer.Add(_leafContainer);
                RefreshExpandedState();
                RefreshPorts();
                return;
            }
            if (Kind == BtNodeKind.Sequence)
            {
                RefreshExpandedState();
                RefreshPorts();
            }
            if (Kind == BtNodeKind.Selector)
            {
                RefreshExpandedState();
                RefreshPorts();
            }
        }
    
        private void DrawLeafInNode()
        {
            if (Kind != BtNodeKind.Leaf && !BtNodeKinds.IsDecorator(Kind) || _graphAsset == null) return;
            _leafInspector ??= new BtNodeInspector(_graphAsset, Guid, Kind, typeChanged =>
            {
                if (typeChanged) TryAutoRenameLeafNodeTitle(_graphAsset);
                _onDataChanged?.Invoke(Guid);
            });
            _leafInspector.Draw(_isReadOnly);
        }

        internal void DisposeInspector()
        {
            _leafInspector?.Dispose();
            _leafInspector = null;
        }

        private void TryAutoRenameLeafNodeTitle(BtGraphAsset asset)
        {
            BtLeafNodeData leafData = asset.FindLeafData(Guid);
            if (leafData == null || leafData.Leaf == null)
                return;

            string typeName = leafData.Leaf.GetType().Name;
            if (typeName.EndsWith("Leaf", StringComparison.Ordinal))
                typeName = typeName.Substring(0, typeName.Length - "Leaf".Length);

            int headerIndex = asset.FindHeaderIndex(Guid);
            if (headerIndex < 0)
                return;

            BtNodeHeader header = asset.Nodes[headerIndex];
            if (header.Title == typeName)
                return;

            header.Title = typeName;
            asset.Nodes[headerIndex] = header;

            SetTitle(typeName);
        }

        private void DisposeNodeTrees()
        {
            DisposeInspector();
        }

        private VisualElement CreatePortHost(bool isTop)
        {
            var host = new VisualElement();
            host.style.position = Position.Absolute;
            host.style.width = PortPillWidth;
            host.style.height = PortPillHeight;

            host.style.left = new Length(50, LengthUnit.Percent);
            host.style.marginLeft = -PortPillWidth * 0.5f;

            if (isTop)
                host.style.top = -PortPillHeight;
            else
                host.style.bottom = -PortPillHeight;

            host.style.justifyContent = Justify.Center;
            host.style.alignItems = Align.Center;
            host.pickingMode = PickingMode.Ignore;

            return host;
        }

        private Port CreateHiddenPort(Direction direction, Port.Capacity capacity, IEdgeConnectorListener edgeConnectorListener, bool isTop)
        {
            var port = InstantiatePort(Orientation.Vertical, direction, capacity, typeof(bool));

            port.AddToClassList("bt-port");
            port.AddToClassList(direction == Direction.Input ? "bt-port-input" : "bt-port-output");

            port.portName = "";
            port.style.width = PortPillWidth;
            port.style.height = PortPillHeight;

            port.style.position = Position.Absolute;
            if (isTop)
                port.style.right = -4;
            else
                port.style.left = -4;
        
            port.style.top = 0;
            port.style.opacity = 0;
            port.pickingMode = PickingMode.Position;
        
            port.portColor = new Color(0.10f, 0.70f, 1.00f, 1f);
            //port.RegisterCallback<AttachToPanelEvent>(_ => port.MarkDirtyRepaint());
        
            port.AddManipulator(new EdgeConnector<Edge>(edgeConnectorListener));
            return port;
        }

        private VisualElement CreatePortVisual(bool isInput)
        {
            var pill = new VisualElement();
            pill.AddToClassList("bt-port-pill");
            pill.AddToClassList(isInput ? "bt-port-in" : "bt-port-out");

            pill.pickingMode = PickingMode.Ignore;
            pill.style.position = Position.Absolute;
            pill.style.left = 0;
            pill.style.top = 0;
            pill.style.width = PortPillWidth;
            pill.style.height = PortPillHeight;

            var dot = new VisualElement();
            dot.AddToClassList("bt-port-dot");
            dot.pickingMode = PickingMode.Ignore;
            pill.Add(dot);

            return pill;
        }
    }
}
