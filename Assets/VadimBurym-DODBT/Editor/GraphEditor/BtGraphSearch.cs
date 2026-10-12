using System;
using System.Collections.Generic;
using System.Linq;

internal static class BtGraphSearch
{
    internal static List<string> Find(BtGraphAsset graph, string query)
    {
        var result = new List<string>();
        if (graph == null || string.IsNullOrWhiteSpace(query)) return result;
        var words = query.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        foreach (var node in graph.Nodes.OrderBy(n => n.Position.x).ThenBy(n => n.Position.y).ThenBy(n => n.Guid, StringComparer.Ordinal))
        {
            var type = graph.FindLeafData(node.Guid)?.Leaf?.GetType();
            string text = node.Title + " " + node.Kind + " " + type?.FullName;
            if (words.All(word => text.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)) result.Add(node.Guid);
        }
        return result;
    }
}
