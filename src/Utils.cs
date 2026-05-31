using System;
using System.Collections;
using System.Linq;

namespace ZmanBase
{
    // From https://stackoverflow.com/a/50969107
    public class ArrayByEnum<T,U> : IEnumerable where U : Enum
    {
        private readonly T[] _array;
        private readonly int _lower;

        public ArrayByEnum()
        {
            _lower = Convert.ToInt32(Enum.GetValues(typeof(U)).Cast<U>().Min());
            int upper = Convert.ToInt32(Enum.GetValues(typeof(U)).Cast<U>().Max());
            _array = new T[1 + upper - _lower];
        }

        public T this[U key]
        {
            get { return _array[Convert.ToInt32(key) - _lower]; }
            set { _array[Convert.ToInt32(key) - _lower] = value; }
        }

        public IEnumerator GetEnumerator()
        {
            return Enum.GetValues(typeof(U)).Cast<U>().Select(i => this[i]).GetEnumerator();
        }
    }

    public static class HelperFunctions
    {
        // From https://stackoverflow.com/a/2683487
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if(val.CompareTo(max) > 0) return max;
            else return val;
        }
    }
}
