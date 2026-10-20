using System;
using UnityEngine;

namespace DODBT.Light.Example.Gameplay.Scene
{
    [Serializable]
    public sealed class PatrolPosition
    {
        public Vector2 Position;
        //public float DifficultyWeight;
        public int[] NeighbourPoints;
    }
}
