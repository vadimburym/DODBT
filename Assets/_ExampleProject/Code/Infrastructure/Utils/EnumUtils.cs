using System;
using System.Collections.Generic;

namespace Utils
{
    public static class EnumUtils<T> where T : struct, Enum
    {
        public static readonly T[] Values = CalculateValues();
        private static readonly Dictionary<T, string> _cachedStrings = new();

        public static string ToString(T t)
        {
            if (_cachedStrings.TryGetValue(t, out string exist) == false)
            {
                exist = t.ToString();
                _cachedStrings.Add(t, exist);
            }

            return exist;
        }

        public static T MoveNext(T current)
        {
            var values = Values;
            T target = default;
            for (int i = 0; i < values.Length; i++)
            {
                if (EqualityComparer<T>.Default.Equals(current, values[i]))
                {
                    if (i == values.Length - 1)
                    {
                        target = values[0];
                    }
                    else
                    {
                        target = values[i + 1];
                    }
                }
            }
            return target;
        }

        private static T[] CalculateValues()
        {
            var type = typeof(T);
            return (T[])Enum.GetValues(type);
        }
    }
}
