using System;

namespace _ExampleProject.Code.Features.Player.Components
{
    [Serializable]
    public struct PlayerVisibility
    {
        public float TickInterval;
        public float TickTime;
        public bool IsPlayerDetected;
        public bool IsPlayerRaycast;
        public float SqrDistanceToPlayer;
        public float DetectSqrDistance;
        public float HuntingSqrDistance;
        public int DetectedPlayer;

        public void Setup(float tickInterval, float tickTime, float detectSqrDistance, float huntingSqrDistance)
        {
            TickInterval = tickInterval;
            TickTime = tickTime;
            DetectSqrDistance = detectSqrDistance;
            HuntingSqrDistance = huntingSqrDistance;
        }
        
        public void Reset()
        {
            IsPlayerDetected = false;
            IsPlayerRaycast = false;
            SqrDistanceToPlayer = -1;
            DetectedPlayer = -1;
        }
    }
}