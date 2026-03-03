using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.InputService
{
    public interface IInputService
    {
        Vector2 MoveDirection { get; }
    }
}