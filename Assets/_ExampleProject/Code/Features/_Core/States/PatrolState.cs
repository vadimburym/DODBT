using System;

namespace _Project.Code.Features.Test
{
    [Serializable]
    public struct PatrolState
    {
        public int TargetPositionIndex;
        public bool IsReached;
        public float NearestTimer;

        public void Reset()
        {
            TargetPositionIndex = -1;
            IsReached = true;
            NearestTimer = 0;
        }
    }
}