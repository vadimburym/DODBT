using DODBT.Light.Example.Setup;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DODBT.Light.Example.Services
{
    public sealed class InputService : IInputService, ITick
    {
        public Vector2 MoveDirection => _moveDirection;
        
        private Vector2 _moveDirection;
        
        public void Tick()
        {
            float x = 0f;
            float y = 0f;

            var keyboard = Keyboard.current;
            if (keyboard != null && Application.isFocused)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1;
            }

            var rawMove = new Vector2(x, y);
            _moveDirection = rawMove.sqrMagnitude > 1f ? rawMove.normalized : rawMove;
        }
    }
}
