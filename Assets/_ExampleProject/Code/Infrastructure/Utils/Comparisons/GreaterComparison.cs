using System;

namespace Utils
{
    [Serializable]
    public sealed class GreaterComparison : IComparison
    {
        public bool CompareInt(int left, int right) => left > right;
        public bool CompareFloat(float left, float right) => left > right;
    }
}