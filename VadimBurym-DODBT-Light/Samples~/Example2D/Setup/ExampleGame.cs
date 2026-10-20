using DODBT.Light.Example.Services;
using UnityEngine;

namespace DODBT.Light.Example.Setup
{
    [DefaultExecutionOrder(-100)]
    public sealed class ExampleGame : MonoBehaviour
    {
        [SerializeField] private GameInstaller _gameInstaller;
        [SerializeField] private SceneInstaller _sceneInstaller;
        private static ExampleGame _active;
        private GameSession _session;
        private bool _ready;

        private void OnEnable()
        {
            if (_active != null && _active != this)
            {
                Debug.LogWarning("The Light example supports one active session. The second Example Game is disabled.", this);
                enabled = false;
                return;
            }
            _active = this;
            try
            {
                ServiceLocator.Clear();
                _session = new GameSession();
                _gameInstaller.InstallBindings();
                _sceneInstaller.InstallBindings();
                EcsWorlds.WarmUp();
                _session.Construct();
                _session.Initialize();
                _ready = true;
            }
            catch
            {
                StopSession();
                enabled = false;
                throw;
            }
        }

        private void Update() { if (_ready) _session.OnUpdate(); }
        private void LateUpdate() { if (_ready) _session.OnLateUpdate(); }
        private void OnDisable() { if (_active == this) StopSession(); }

        private void StopSession()
        {
            _ready = false;
            try { _session?.OnDestroy(); }
            finally { _session = null; _active = null; }
        }
    }
}
