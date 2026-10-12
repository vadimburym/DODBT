using System;
using System.Collections.Generic;

// Preserve graph data when the optional extension is disabled.
[Serializable]
internal sealed class BtDecoratorNodeData
{
    public string Guid;
    public double Duration = 1;
    public List<string> ChildrenGuids = new();
}
