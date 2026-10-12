using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

internal sealed partial class BtGraphView
{
    internal event Action<int, int> SearchResultsChanged;
    private string _searchQuery = "";
    private List<string> _searchMatches = new();
    private int _searchIndex = -1;
    private string _lastPasteText;
    private int _pasteCount;
    protected override bool canCopySelection => selection.OfType<BtNodeView>().Any(node => node.Kind != BtNodeKind.Root);
    protected override bool canDuplicateSelection => !IsReadOnly && canCopySelection;
    protected override bool canCutSelection => !IsReadOnly && canCopySelection;

    internal void SetSearchQuery(string query)
    {
        _searchQuery = query ?? "";
        _searchIndex = -1;
        RefreshSearch();
    }

    private void RefreshSearch()
    {
        string current = _searchIndex >= 0 && _searchIndex < _searchMatches.Count ? _searchMatches[_searchIndex] : null;
        _searchMatches = BtGraphSearch.Find(GraphAsset, _searchQuery);
        _searchIndex = current == null ? -1 : _searchMatches.IndexOf(current);
        var matches = _searchMatches.ToHashSet();
        foreach (var pair in _nodeViewsByGuid) pair.Value.SetSearchMatch(matches.Contains(pair.Key));
        SearchResultsChanged?.Invoke(_searchIndex + 1, _searchMatches.Count);
    }

    internal void NavigateSearch(int direction)
    {
        if (_searchMatches.Count == 0) return;
        _searchIndex = _searchIndex < 0 ? (direction < 0 ? _searchMatches.Count - 1 : 0) :
            (_searchIndex + direction + _searchMatches.Count) % _searchMatches.Count;
        if (_nodeViewsByGuid.TryGetValue(_searchMatches[_searchIndex], out var node))
        {
            ClearSelection();
            AddToSelection(node);
            FrameSelection();
        }
        SearchResultsChanged?.Invoke(_searchIndex + 1, _searchMatches.Count);
    }

    private void ConfigureClipboard()
    {
        serializeGraphElements = elements =>
        {
            var ids = elements.OfType<BtNodeView>().Where(node => node.Kind != BtNodeKind.Root).Select(node => node.Guid).ToHashSet();
            // Cutting a group root deletes the whole instance, so its clipboard must contain every member.
            foreach (string id in ids.ToArray())
            {
                var instance = BtLinkedGroups.Find(GraphAsset, id);
                if (instance?.RootGuid == id) ids.UnionWith(instance.Members.Select(m => m.Guid));
            }
            return BtGraphClipboard.Copy(GraphAsset, ids);
        };
        canPasteSerializedData = text => !IsReadOnly && GraphAsset != null && BtGraphClipboard.CanPaste(text);
        unserializeAndPaste = (_, text) => PasteNodes(text);
    }

    private void PasteNodes(string text)
    {
        if (IsReadOnly || GraphAsset == null) return;
        try
        {
            _pasteCount = text == _lastPasteText ? _pasteCount + 1 : 1;
            _lastPasteText = text;
            var added = BtGraphClipboard.Paste(GraphAsset, text, new Vector2(40, 40) * _pasteCount);
            ReloadWithoutLog();
            ClearSelection();
            foreach (string id in added)
                if (_nodeViewsByGuid.TryGetValue(id, out var node)) AddToSelection(node);
            FrameSelection();
        }
        catch (Exception exception)
        {
            LogRequested?.Invoke("Cannot paste nodes: " + exception.GetBaseException().Message, LogLevel.Error, false);
        }
    }
}
