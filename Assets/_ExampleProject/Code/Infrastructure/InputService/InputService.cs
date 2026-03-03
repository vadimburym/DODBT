using _Project.Code.Core.Abstractions.Contracts;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.InputService
{
    public sealed class InputService : IInputService, ITick
    {
        public Vector2 MoveDirection => _moveDirection;
        
        private Vector2 _moveDirection;
        
        public void Tick()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    y += 1f;

            var rawMove = new Vector2(x, y);
            _moveDirection = rawMove.sqrMagnitude > 1f ? rawMove.normalized : rawMove;
        }
    }
}