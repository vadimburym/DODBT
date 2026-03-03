// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

[Serializable]
internal sealed class BtSelectorNodeData
{
    public string Guid;
    public List<string> ChildrenGuids = new();
}