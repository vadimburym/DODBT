using System.Collections.Generic;
using DODBT.Light.Example.Gameplay.Scene;
using DODBT.Light.Example.Gameplay.Settings;
using UnityEngine;
using DODBT.Light.Example.BehaviourTree.Comparisons;
using DODBT.Light.Example.Services;

namespace DODBT.Light.Example.Gameplay.Scene
{
    public sealed class PatrolPointsProvider : MonoBehaviour
    {
        public PatrolPosition[] PatrolPositions => _patrolPositions;
        
        [SerializeField] private GameObject _markersRoot;
        [SerializeField] private PatrolPosition[] _patrolPositions;
        private readonly List<int> _buffer = new();
        
        [ContextMenu("Collect Patrol Points")]
        public void Collect()
        {
            if (_markersRoot == null)
            {
                Debug.LogWarning("MarkersRoot is null", this);
                return;
            }
            
            var markers = _markersRoot.GetComponentsInChildren<PatrolMarker>(true);
            _patrolPositions = new PatrolPosition[markers.Length];
            for (int i = 0; i < markers.Length; i++)
            {
                var marker = markers[i];
                _patrolPositions[i] = new PatrolPosition
                {
                    //DifficultyWeight =  marker.DifficultyWeight,
                    Position = marker.Position,
                };
            }

            for (int i = 0; i < _patrolPositions.Length; i++)
            {
                var patrol = _patrolPositions[i];
                var neighbourDistance = markers[i].NeighbourDistance;
                _buffer.Clear();
                for (int k = 0; k < _patrolPositions.Length; k++)
                {
                    if (k == i) continue;
                    var position = _patrolPositions[k].Position;
                    if (NavMeshUtils.TryGetNavMeshDistance(patrol.Position, position, out var distance))
                    {
                        if (distance <= neighbourDistance)
                            _buffer.Add(k);
                    }
                }
                patrol.NeighbourPoints = _buffer.ToArray();
            }
            
            Debug.Log($"Collected {markers.Length} patrol markers.", this);
        }
    }
}
