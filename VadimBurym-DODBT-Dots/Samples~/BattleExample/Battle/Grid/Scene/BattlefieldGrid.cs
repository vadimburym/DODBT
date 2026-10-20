namespace DODBT.Example.Battle.Grid
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    [DisallowMultipleComponent]
    public sealed class BattlefieldGrid : MonoBehaviour
    {
        public enum GridDebugMode
        {
            None,
            BoundsOnly,
            BlockedRectsOnly,
            BlockedCellsOnly,
            GridOnly,
            BlockedAndGrid
        }

        [Header("Grid")]
        public Vector3 Origin;
        [Min(0.1f)] public float CellSize = 1f;
        [Min(1)] public int Width = 64;
        [Min(1)] public int Height = 64;

        [Header("Walkability")]
        public bool FillWalkableByDefault = true;

        [Tooltip("Явно заблокированные прямоугольники в координатах клеток.")]
        public List<GridBlockedRect> BlockedRects = new();

        [Header("Debug")]
        public GridDebugMode DebugMode = GridDebugMode.BlockedRectsOnly;
        public bool DrawOnlyWhenSelected = true;
        public bool DrawOuterBounds = true;

        [Tooltip("Максимальный радиус от камеры SceneView, в пределах которого рисуются клетки.")]
        [Min(1f)] public float VisibleRangeFromSceneCamera = 200f;

        [Tooltip("На большой высоте камеры сетка рисуется реже.")]
        public bool UseAdaptiveLineStep = true;

        [Tooltip("Рисовать полупрозрачную заливку blocked-клеток.")]
        public bool DrawBlockedFill = true;

        [Tooltip("Рисовать контур blocked-клеток.")]
        public bool DrawBlockedWire = false;

        [SerializeField, HideInInspector]
        private byte[] _walkable;

        public byte[] Walkable => _walkable;

        [Serializable]
        public struct GridBlockedRect
        {
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }

        private void OnValidate()
        {
            Width = Mathf.Max(1, Width);
            Height = Mathf.Max(1, Height);
            CellSize = Mathf.Max(0.1f, CellSize);
            VisibleRangeFromSceneCamera = Mathf.Max(1f, VisibleRangeFromSceneCamera);

            int total = Width * Height;
            if (_walkable == null || _walkable.Length != total)
                _walkable = new byte[total];

            byte fill = FillWalkableByDefault ? (byte)1 : (byte)0;
            for (int i = 0; i < _walkable.Length; i++)
                _walkable[i] = fill;

            for (int r = 0; r < BlockedRects.Count; r++)
            {
                var rect = BlockedRects[r];

                int rectWidth = Mathf.Max(0, rect.Width);
                int rectHeight = Mathf.Max(0, rect.Height);

                int xMin = Mathf.Clamp(rect.X, 0, Width);
                int yMin = Mathf.Clamp(rect.Y, 0, Height);
                int xMax = Mathf.Clamp(rect.X + rectWidth, 0, Width);
                int yMax = Mathf.Clamp(rect.Y + rectHeight, 0, Height);

                for (int y = yMin; y < yMax; y++)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        _walkable[y * Width + x] = 0;
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (DrawOnlyWhenSelected)
                return;
            DrawGizmosInternal();
        }

        private void OnDrawGizmosSelected()
        {
            if (!DrawOnlyWhenSelected)
                return;
            DrawGizmosInternal();
        }

        private void DrawGizmosInternal()
        {
    #if UNITY_EDITOR
            if (DebugMode == GridDebugMode.None)
                return;

            if (_walkable == null || _walkable.Length != Width * Height)
                OnValidate();

            var sceneView = SceneView.currentDrawingSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                DrawFallbackBoundsOnly();
                return;
            }

            Camera cam = sceneView.camera;

            if (DrawOuterBounds || DebugMode == GridDebugMode.BoundsOnly)
                DrawBounds();

            if (DebugMode == GridDebugMode.BoundsOnly)
                return;

            GetVisibleCellRange(
                cam,
                out int minX,
                out int maxX,
                out int minY,
                out int maxY);

            switch (DebugMode)
            {
                case GridDebugMode.BlockedRectsOnly:
                    DrawBlockedRects();
                    break;

                case GridDebugMode.BlockedCellsOnly:
                    DrawBlockedCells(minX, maxX, minY, maxY);
                    break;

                case GridDebugMode.GridOnly:
                    DrawGridLines(cam, minX, maxX, minY, maxY);
                    break;

                case GridDebugMode.BlockedAndGrid:
                    DrawBlockedCells(minX, maxX, minY, maxY);
                    DrawGridLines(cam, minX, maxX, minY, maxY);
                    break;
            }
    #else
            DrawFallbackBoundsOnly();
    #endif
        }

        private void DrawFallbackBoundsOnly()
        {
            if (DrawOuterBounds || DebugMode == GridDebugMode.BoundsOnly)
                DrawBounds();
        }

        private void DrawBounds()
        {
            var boundsCenter = new Vector3(
                Origin.x + Width * CellSize * 0.5f,
                Origin.y,
                Origin.z + Height * CellSize * 0.5f
            );

            var boundsSize = new Vector3(
                Width * CellSize,
                0.05f,
                Height * CellSize
            );

            Gizmos.color = new Color(1f, 1f, 1f, 0.45f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);
        }

        private void DrawBlockedRects()
        {
            if (BlockedRects == null || BlockedRects.Count == 0)
                return;

            for (int i = 0; i < BlockedRects.Count; i++)
            {
                var rect = BlockedRects[i];
                if (rect.Width <= 0 || rect.Height <= 0)
                    continue;

                int xMin = Mathf.Clamp(rect.X, 0, Width);
                int yMin = Mathf.Clamp(rect.Y, 0, Height);
                int xMax = Mathf.Clamp(rect.X + rect.Width, 0, Width);
                int yMax = Mathf.Clamp(rect.Y + rect.Height, 0, Height);

                if (xMax <= xMin || yMax <= yMin)
                    continue;

                var center = new Vector3(
                    Origin.x + (xMin + (xMax - xMin) * 0.5f) * CellSize,
                    Origin.y,
                    Origin.z + (yMin + (yMax - yMin) * 0.5f) * CellSize
                );

                var size = new Vector3(
                    (xMax - xMin) * CellSize,
                    0.02f,
                    (yMax - yMin) * CellSize
                );

                if (DrawBlockedFill)
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.30f);
                    Gizmos.DrawCube(center, size);
                }

                Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.8f);
                Gizmos.DrawWireCube(center, size);
            }
        }

        private void DrawBlockedCells(int minX, int maxX, int minY, int maxY)
        {
            var cellSize3 = new Vector3(CellSize, 0.02f, CellSize);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int index = y * Width + x;
                    bool walkable = _walkable[index] != 0;
                    if (walkable)
                        continue;

                    var center = new Vector3(
                        Origin.x + (x + 0.5f) * CellSize,
                        Origin.y,
                        Origin.z + (y + 0.5f) * CellSize
                    );

                    if (DrawBlockedFill)
                    {
                        Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
                        Gizmos.DrawCube(center, cellSize3);
                    }

                    if (DrawBlockedWire)
                    {
                        Gizmos.color = new Color(1f, 0.15f, 0.15f, 0.85f);
                        Gizmos.DrawWireCube(center, cellSize3);
                    }
                }
            }
        }

    #if UNITY_EDITOR
        private void DrawGridLines(Camera cam, int minX, int maxX, int minY, int maxY)
        {
            int lineStep = 1;
            if (UseAdaptiveLineStep)
            {
                float camHeight = Mathf.Abs(cam.transform.position.y - Origin.y);
                if (camHeight > 40f) lineStep = 2;
                if (camHeight > 80f) lineStep = 4;
                if (camHeight > 160f) lineStep = 8;
                if (camHeight > 320f) lineStep = 16;
            }

            Gizmos.color = new Color(0.3f, 1f, 0f, 0.85f);

            for (int x = minX; x <= maxX + 1; x += lineStep)
            {
                float wx = Origin.x + x * CellSize;
                var from = new Vector3(wx, Origin.y, Origin.z + minY * CellSize);
                var to = new Vector3(wx, Origin.y, Origin.z + (maxY + 1) * CellSize);
                Gizmos.DrawLine(from, to);
            }

            for (int y = minY; y <= maxY + 1; y += lineStep)
            {
                float wz = Origin.z + y * CellSize;
                var from = new Vector3(Origin.x + minX * CellSize, Origin.y, wz);
                var to = new Vector3(Origin.x + (maxX + 1) * CellSize, Origin.y, wz);
                Gizmos.DrawLine(from, to);
            }
        }

        private void GetVisibleCellRange(
            Camera cam,
            out int minX,
            out int maxX,
            out int minY,
            out int maxY)
        {
            var camPos = cam.transform.position;
            float range = VisibleRangeFromSceneCamera;

            float minWorldX = camPos.x - range;
            float maxWorldX = camPos.x + range;
            float minWorldZ = camPos.z - range;
            float maxWorldZ = camPos.z + range;

            minX = Mathf.Max(0, Mathf.FloorToInt((minWorldX - Origin.x) / CellSize) - 1);
            maxX = Mathf.Min(Width - 1, Mathf.CeilToInt((maxWorldX - Origin.x) / CellSize) + 1);
            minY = Mathf.Max(0, Mathf.FloorToInt((minWorldZ - Origin.z) / CellSize) - 1);
            maxY = Mathf.Min(Height - 1, Mathf.CeilToInt((maxWorldZ - Origin.z) / CellSize) + 1);
        }
    #endif

        [ContextMenu("Fill Walkable")]
        private void FillWalkable()
        {
            FillWalkableByDefault = true;
            OnValidate();
        }

        [ContextMenu("Fill Blocked")]
        private void FillBlocked()
        {
            FillWalkableByDefault = false;
            OnValidate();
        }

        [ContextMenu("Rebuild Walkability")]
        private void RebuildWalkability()
        {
            OnValidate();
        }
    }
}
