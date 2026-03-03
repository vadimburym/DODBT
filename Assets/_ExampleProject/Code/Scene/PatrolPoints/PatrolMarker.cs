using System;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace _ExampleProject.Code.Features.Enemy.Behaviours
{
    [SelectionBase]
    [ExecuteAlways]
    public sealed class PatrolMarker : MonoBehaviour
    {
        //public float DifficultyWeight => _difficultyWeight;
        public float NeighbourDistance => _neighbourDistance;
        public Vector2 Position => transform.position;
        
        [Min(0f)][SerializeField] private float _neighbourDistance = 5f;
        //[Min(0f)][SerializeField] private float _difficultyWeight;
        
        //[SerializeField, HideInInspector] private TMP_Text _text;           
        [SerializeField, HideInInspector] private SpriteRenderer _sprite;   
        
        public void SetTint(Color c)
        {
            if (_sprite != null)
                _sprite.color = c;
        }
        
#if UNITY_EDITOR
        public static event Action Changed;
        
        private void OnDrawGizmosSelected()
        {
            var center = Position;
            var r = NeighbourDistance;
            
            Handles.color = new Color(0.2f, 0.55f, 1f, 0.025f);
            Handles.DrawSolidDisc(center, Vector3.forward, r);
            
            Handles.color = new Color(1f, 1f, 1f, 0.8f);
            Handles.DrawWireDisc(center, Vector3.forward, r);
        }

        private void OnValidate()
        {
            //if (_text != null)
            //    _text.text = _difficultyWeight.ToString("0.00");
            Changed?.Invoke();
            ApplyEditorRules();
        }

        private void OnTransformParentChanged() => Changed?.Invoke();
        private void OnEnable()  => ApplyEditorRules();

        private void ApplyEditorRules()
        {
            if (Application.isPlaying) return;
            if (_sprite != null)
                _sprite.hideFlags = HideFlags.HideInInspector;
            //if (_text != null)
            //{
            //    _text.gameObject.hideFlags = HideFlags.HideInHierarchy;
            //    SceneVisibilityManager.instance.DisablePicking(_text.gameObject, true);
            //    EditorUtility.SetDirty(_text.gameObject);
            //}
            EditorApplication.RepaintHierarchyWindow();
            SceneView.RepaintAll();
        }
#endif
    }
}