// DODBT-Light
// Copyright (c) 2026 Vadim Burym (vadimburym).
// Licensed under the DODBT Custom Game-Use and Redistribution License.
// See https://github.com/vadimburym/DODBT/blob/main/LICENSE.md

using UnityEngine;
using UnityEngine.UIElements;

namespace VadimBurym.DodBehaviourTree.Light.Editor
{
    internal sealed class BtNodeGlyph : VisualElement
    {
        private readonly BtNodeKind _kind;
        internal BtNodeGlyph(BtNodeKind kind)
        {
            _kind = kind;
            pickingMode = PickingMode.Ignore;
            style.width = 56;
            style.height = 48;
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            p.strokeColor = p.fillColor = Color.white;
            p.lineWidth = 3.5f;
            p.lineCap = LineCap.Round;
            p.lineJoin = LineJoin.Round;
            if (_kind == BtNodeKind.Selector || _kind == BtNodeKind.MemorySelector)
            {
                p.BeginPath(); p.MoveTo(new Vector2(18, 14));
                p.BezierCurveTo(new Vector2(18, 2), new Vector2(39, 2), new Vector2(39, 14));
                p.BezierCurveTo(new Vector2(39, 24), new Vector2(28, 23), new Vector2(28, 31));
                p.Stroke();
                p.BeginPath(); p.MoveTo(new Vector2(28, 39)); p.LineTo(new Vector2(28, 40)); p.Stroke();
            }
            else if (_kind == BtNodeKind.Parallel)
            {
                Arrow(p, 12); Arrow(p, 25); Arrow(p, 38);
            }
            else Arrow(p, 25);

            if (_kind == BtNodeKind.MemorySelector || _kind == BtNodeKind.MemorySequence)
            {
                // Bookmark: remembers which child to resume.
                p.lineWidth = 1.8f;
                p.BeginPath(); p.MoveTo(new Vector2(2, 3)); p.LineTo(new Vector2(10, 3));
                p.LineTo(new Vector2(10, 16)); p.LineTo(new Vector2(6, 12));
                p.LineTo(new Vector2(2, 16)); p.ClosePath(); p.Stroke();
            }
        }

        private static void Arrow(Painter2D p, float y)
        {
            p.BeginPath(); p.MoveTo(new Vector2(13, y)); p.LineTo(new Vector2(45, y)); p.Stroke();
            p.BeginPath(); p.MoveTo(new Vector2(37, y - 6)); p.LineTo(new Vector2(45, y));
            p.LineTo(new Vector2(37, y + 6)); p.Stroke();
        }
    }
}
