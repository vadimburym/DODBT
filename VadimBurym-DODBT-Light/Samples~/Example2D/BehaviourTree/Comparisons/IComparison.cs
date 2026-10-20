namespace DODBT.Light.Example.BehaviourTree.Comparisons
{
    public interface IComparison
    {
        bool CompareInt(int left, int right);
        bool CompareFloat(float left, float right);
    }
}
