#if UNITY_EDITOR
using _ExampleProject.Code.Features.Enemy.Behaviours;
using UnityEditor;
using UnityEngine;
using Utils;

[InitializeOnLoad]
public static class PatrolMarkerSelectionHighlighter
{
    static readonly Color NormalColor    = new Color(0.2f, 0.55f, 1f, 1f);   // синий
    static readonly Color NeighbourColor = new Color(1f, 0.2f, 0.2f, 1f);    // красный

    // Как часто пересчитывать во время перетаскивания (сек)
    const double DragRecalcInterval = 0.15;

    static double _nextAllowedRecalcTime;
    static bool _dirty;

    static PatrolMarker _selected;
    static PatrolPointsProvider _root;
    static PatrolMarker[] _markers = System.Array.Empty<PatrolMarker>();

    // Дешёвый трекинг изменений позиций (чтобы не считать путь зря)
    static Vector3 _lastSelectedPos;
    static float _lastSelectedRadius;

    static PatrolMarkerSelectionHighlighter()
    {
        Selection.selectionChanged += OnSelectionChanged;
        EditorApplication.update += OnEditorUpdate;
        SceneView.duringSceneGui += OnSceneGUI;

        PatrolMarker.Changed += MarkDirty;

        // первичная инициализация
        EditorApplication.delayCall += OnSelectionChanged;
    }

    static void MarkDirty() => _dirty = true;

    static void OnSelectionChanged()
    {
        _selected = null;
        _root = null;
        _markers = System.Array.Empty<PatrolMarker>();

        if (Selection.activeGameObject)
            _selected = Selection.activeGameObject.GetComponentInParent<PatrolMarker>();

        if (_selected)
        {
            _root = _selected.GetComponentInParent<PatrolPointsProvider>();
            if (_root)
                _markers = _root.GetComponentsInChildren<PatrolMarker>(true);

            _lastSelectedPos = _selected.transform.position;
            _lastSelectedRadius = _selected.NeighbourDistance;
        }

        // При смене выделения — пересчитать сразу
        _dirty = true;
        RecalcNowIfPossible();
    }

    static void OnSceneGUI(SceneView view)
    {
        // Хотим: на отпускание мыши после перетаскивания — пересчитать сразу
        var e = Event.current;
        if (e == null) return;

        if (e.type == EventType.MouseUp)
        {
            // отпускание мыши обычно означает конец драга
            _dirty = true;
            RecalcNowIfPossible(force: true);
        }
    }

    static void OnEditorUpdate()
    {
        // если нет валидного root — ничего не делаем
        if (!_root || _markers.Length == 0)
            return;

        // если маркер не выбран — можно (опционально) сбросить цвета
        if (!_selected)
            return;

        // Отмечаем dirty, если реально изменились позиция/радиус выбранного
        var pos = _selected.transform.position;
        var rad = _selected.NeighbourDistance;

        if (pos != _lastSelectedPos || !Mathf.Approximately(rad, _lastSelectedRadius))
        {
            _lastSelectedPos = pos;
            _lastSelectedRadius = rad;
            _dirty = true;
        }

        if (!_dirty)
            return;

        // Throttle: если сейчас идёт активное редактирование (перетаскивание), считаем редко
        bool isDraggingNow = GUIUtility.hotControl != 0; // хороший общий индикатор "редактор сейчас что-то тащит"

        if (isDraggingNow)
        {
            if (EditorApplication.timeSinceStartup < _nextAllowedRecalcTime)
                return;

            _nextAllowedRecalcTime = EditorApplication.timeSinceStartup + DragRecalcInterval;
            RecalcNowIfPossible();
        }
        else
        {
            // не тащим — можно пересчитать сразу
            RecalcNowIfPossible(force: true);
        }
    }

    static void RecalcNowIfPossible(bool force = false)
    {
        if (!_root || _markers.Length == 0)
            return;

        // если выделение ушло — вернём всем синий
        if (!_selected)
        {
            foreach (var m in _markers)
                if (m) m.SetTint(NormalColor);

            _dirty = false;
            SceneView.RepaintAll();
            return;
        }

        // если не force, а NavMesh ещё не готов — ты сам решаешь что делать.
        // Здесь просто будем пытаться.
        var from = _selected.Position;
        var radius = _selected.NeighbourDistance;

        foreach (var m in _markers)
        {
            if (!m) continue;

            if (m == _selected)
            {
                // можно оставить красным или сделать отдельный цвет
                m.SetTint(NeighbourColor);
                continue;
            }

            var to = m.Position;

            if (NavMeshUtils.TryGetNavMeshDistance(from, to, out var dist) && dist <= radius)
                m.SetTint(NeighbourColor);
            else
                m.SetTint(NormalColor);
        }

        _dirty = false;
        SceneView.RepaintAll();
    }
}
#endif