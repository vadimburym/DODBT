// DODBT-Dots
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

namespace VadimBurym.DodBehaviourTree.Dots.Editor
{
    using UnityEditor.Experimental.GraphView;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal sealed class BtEdge : Edge
    {
        private bool _groupHighlighted;
        private Color _groupColor;

        internal void SetGroupHighlight(bool highlighted, Color color)
        {
            _groupHighlighted = highlighted;
            _groupColor = color;
            UpdateEdgeControl();
        }

        public override bool UpdateEdgeControl()
        {
            bool updated = base.UpdateEdgeControl();
            ApplyGroupHighlight();
            return updated;
        }

        protected override void OnCustomStyleResolved(ICustomStyle styles)
        {
            base.OnCustomStyleResolved(styles);
            ApplyGroupHighlight();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            ApplyGroupHighlight();
        }

        public override void OnUnselected()
        {
            base.OnUnselected();
            ApplyGroupHighlight();
        }

        private void ApplyGroupHighlight()
        {
            if (!_groupHighlighted) return;
            edgeControl.inputColor = _groupColor;
            edgeControl.outputColor = _groupColor;
            edgeControl.edgeWidth = 5;
        }
    }
}
