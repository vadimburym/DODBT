using System;
using UnityEngine;

namespace _Project.Code.Features.Test
{
    [Serializable]
    public struct Movement
    {
        public Vector2 Direction;
        public float MoveSpeed;

        public void Setup(Vector2 direction, float moveSpeed)
        {
            Direction = direction;
            MoveSpeed = moveSpeed;
        }
    }
}