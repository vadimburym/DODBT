// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Light.Editor
{

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
            if (_graphView.IsReadOnly || _graphView.GraphAsset == null) return;
            var parentNode = edge.output?.node as BtNodeView;
            var childNode = edge.input?.node as BtNodeView;
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
                ReplaceConnections(unityGraphView, newEdge.input, newEdge);
                ReplaceConnections(unityGraphView, newEdge.output, newEdge);
                newEdge.input.Connect(newEdge);
                newEdge.output.Connect(newEdge);
                unityGraphView.AddElement(newEdge);
                _graphView.OnEdgeCreated(newEdge);
            }
            finally { _graphView.EndGroupEdit(); }
        }

        private void ReplaceConnections(GraphView view, Port port, Edge replacement)
        {
            if (port.capacity != Port.Capacity.Single) return;
            var previous = port.connections.ToList();
            foreach (var edge in previous)
            {
                if (edge.input == replacement.input && edge.output == replacement.output) continue;
                string limit = port.direction == Direction.Input ? "parent" : "child";
                _graphView.WarnGraph($"Connection replaced: '{port.node.title}' accepts only one {limit}. " +
                    $"Removed '{edge.output?.node.title}' -> '{edge.input?.node.title}' " +
                    $"to connect '{replacement.output.node.title}' -> '{replacement.input.node.title}'.");
            }
            if (previous.Count > 0) view.DeleteElements(previous);
        }
    }
}
