using System;
using UnityEngine;

namespace Utils
{
    [Serializable]
    public sealed class EqualComparison : IComparison
    {
        public bool CompareInt(int left, int right) => left == right;
        public bool CompareFloat(float left, float right) => Mathf.Approximately(left, right);
    }
}