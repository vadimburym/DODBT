using System;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.StaticData.Enemy
{
    [Serializable]
    public sealed class PatrolPosition
    {
        public Vector2 Position;
        //public float DifficultyWeight;
        public int[] NeighbourPoints;
    }
}