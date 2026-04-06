namespace _Project.Code._Bootstrap
{
    public interface IContext
    {
        void Construct();
        void Initialize();
        void Restart();
        void Load();
        void OnUpdate();
        void OnFixedUpdate();
        void OnLateUpdate();
        void OnSave();
        void OnDestroy();
    }
}