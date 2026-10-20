using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DODBT.Example
{
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class BattleCamera : MonoBehaviour
    {
        [Min(1)] public float MoveSpeed = 40;
        [Min(1)] public float ZoomStep = 5;
        public Vector2 HeightLimits = new(8, 180);
        public Vector2 XLimits = new(-190, 190);
        public Vector2 ZLimits = new(-190, 190);
        public bool EdgeScrolling = true;
        [Min(0)] public float EdgeSize = 12;

        private void Update()
        {
            if (!Application.isFocused) return;
            ReadInput(Time.unscaledDeltaTime);
        }

        private void ReadInput(float deltaTime)
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            Vector2 move = default;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x++;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y--;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y++;
            }
            if (mouse != null && EdgeScrolling)
            {
                var pointer = mouse.position.ReadValue();
                if (pointer.x >= 0 && pointer.x <= Screen.width && pointer.y >= 0 && pointer.y <= Screen.height)
                {
                    if (pointer.x < EdgeSize) move.x--;
                    if (pointer.x > Screen.width - EdgeSize) move.x++;
                    if (pointer.y < EdgeSize) move.y--;
                    if (pointer.y > Screen.height - EdgeSize) move.y++;
                }
            }
            float zoom = mouse == null ? 0 : mouse.scroll.ReadValue().y;
            if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
                (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer))
                zoom /= 120f;
            Move(move, zoom, deltaTime);
#elif ENABLE_LEGACY_INPUT_MANAGER
            Vector2 move = default;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x--;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x++;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y--;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y++;
            if (EdgeScrolling)
            {
                var pointer = Input.mousePosition;
                if (pointer.x >= 0 && pointer.x <= Screen.width && pointer.y >= 0 && pointer.y <= Screen.height)
                {
                    if (pointer.x < EdgeSize) move.x--;
                    if (pointer.x > Screen.width - EdgeSize) move.x++;
                    if (pointer.y < EdgeSize) move.y--;
                    if (pointer.y > Screen.height - EdgeSize) move.y++;
                }
            }
            Move(move, Input.mouseScrollDelta.y, deltaTime);
#endif
        }

        public void Move(Vector2 direction, float zoom, float deltaTime)
        {
            var move = Vector2.ClampMagnitude(direction, 1) * (MoveSpeed * deltaTime);
            var position = transform.position + new Vector3(move.x, -zoom * ZoomStep, move.y);
            position.x = Mathf.Clamp(position.x, XLimits.x, XLimits.y);
            position.z = Mathf.Clamp(position.z, ZLimits.x, ZLimits.y);
            position.y = Mathf.Clamp(position.y, HeightLimits.x, HeightLimits.y);
            transform.position = position;
        }
    }
}
