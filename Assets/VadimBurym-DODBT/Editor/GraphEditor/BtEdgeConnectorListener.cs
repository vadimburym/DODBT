// DODBT (Data Oriented Design Behaviour Tree for Unity)
// Repository: https://github.com/vadimburym/DODBT
// Copyright (c) 2026 vadimburym (Vadim Burym)
// Licensed under the Custom Game-Use and Redistribution License.
// See LICENSE file in the project root for full license information.

using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

internal sealed class BtEdgeConnectorListener : IEdgeConnectorListener
{
    private readonly BtGraphView _graphView;

    public BtEdgeConnectorListener(BtGraphView graphView)
    {
        _graphView = graphView;
    }

    public void OnDropOutsidePort(Edge edge, Vector2 position)
    {
        
    }

    public void OnDrop(GraphView unityGraphView, Edge edge)
    {
        var parentNode = edge.output.node as BtNodeView;
        var childNode = edge.input.node as BtNodeView;
        if (parentNode == null || childNode == null)
            return;
        bool createsCycle = _graphView.WouldCreateCycle(parentNode, childNode) || !_graphView.CanConnectGroups(parentNode.Guid, childNode.Guid);
        if (createsCycle)
        {
            _graphView.schedule.Execute(() => _graphView.ReloadWithoutLog());
            return;
        }
        _graphView.BeginGroupEdit();
        try
        {
            var newEdge = new BtEdge {
                input = edge.input,
                output = edge.output };
            edge.input?.Disconnect(edge);
            edge.output?.Disconnect(edge);
            unityGraphView.RemoveElement(edge);
            if (newEdge.input != null && newEdge.input.capacity == Port.Capacity.Single)
            {
                var oldEdges = newEdge.input.connections.ToList();
                unityGraphView.DeleteElements(oldEdges);
            }
            if (newEdge.output != null && newEdge.output.capacity == Port.Capacity.Single)
                unityGraphView.DeleteElements(newEdge.output.connections.ToList());
            newEdge.input.Connect(newEdge);
            newEdge.output.Connect(newEdge);
            unityGraphView.AddElement(newEdge);
            _graphView.OnEdgeCreated(newEdge);
        }
        finally { _graphView.EndGroupEdit(); }
    }
}
