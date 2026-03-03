using UnityEngine;
using UnityEngine.AI;

namespace Utils
{
    public static class NavMeshUtils
    {
        public static bool TryGetNavMeshDistance(Vector2 start, Vector2 end, out float distance)
        {
            NavMeshPath path = new NavMeshPath();
            distance = Mathf.Infinity;
            if (!NavMesh.CalculatePath(start, end, NavMesh.AllAreas, path))
                return false;
            if (path.status != NavMeshPathStatus.PathComplete)
                return false;
            distance = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distance += Vector2.Distance(
                    path.corners[i - 1],
                    path.corners[i]
                );
            }
            return true;
        }
    }
}