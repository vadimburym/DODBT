using System;

namespace _ExampleProject.Code.Features._Core.States
{
    [Serializable]
    public struct StrafeEntityState
    {
        public float StrafeDistance;
        public int EntityIndex;
        public bool IsReached;
        public float StrafeCooldown;
        public float TickTime;

        public void Setup(float strafeDistance, int entityIndex, float strafeCooldown)
        {
            StrafeDistance = strafeDistance;
            EntityIndex = entityIndex;
            StrafeCooldown = strafeCooldown;
            TickTime = strafeCooldown;
            IsReached = true;
        }
    }
}