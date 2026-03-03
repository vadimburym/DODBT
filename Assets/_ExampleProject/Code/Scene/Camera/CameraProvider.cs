using UnityEngine;

namespace _ExampleProject.Code.Scene
{
    public sealed class CameraProvider : MonoBehaviour
    {
        public Camera Camera => _camera;
        public Vector2 DeadZoneSize => _deadZoneSize;
        public bool IsSmoothing => _isSmoothing;
        public float SmoothTime => _smoothTime;
        public float MaxSpeed => _maxSpeed;
        
        [SerializeField] private Camera _camera;
        [Header("Follow target dead-zone (world units)")]
        [SerializeField] private Vector2 _deadZoneSize = new(2.0f, 1.2f);
        [Header("Smoothing")] 
        [SerializeField] private bool _isSmoothing;
        [SerializeField] private float _smoothTime = 0.35f;
        [SerializeField] private float _maxSpeed = 100f;
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_camera == null) return;

            var p = _camera.transform.position;
            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireCube(new Vector3(p.x, p.y, 0f), new Vector3(_deadZoneSize.x, _deadZoneSize.y, 0f));
        }
#endif
    }
}