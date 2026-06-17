using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ZmanBase
{
    using UnityEngine;

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

    // From https://github.com/farlee2121/BidirectionalMap/blob/main/BidirectionalMap/BiMap.cs
    // by user farlee2121 under the MIT license
    public class BiMap<TForwardKey, TReverseKey>: IEnumerable<KeyValuePair<TForwardKey, TReverseKey>>
    {
        public Indexer<TForwardKey, TReverseKey> Forward { get; private set; } = new Indexer<TForwardKey, TReverseKey>();
        public Indexer<TReverseKey, TForwardKey> Reverse { get; private set; } = new Indexer<TReverseKey, TForwardKey>();

        const string DuplicateKeyErrorMessage = "";

        public BiMap()
        {
        }
        public BiMap(IDictionary<TForwardKey, TReverseKey> oneWayMap)
        {
            Forward = new Indexer<TForwardKey, TReverseKey>(oneWayMap);
            var reversedOneWayMap = oneWayMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
            Reverse = new Indexer<TReverseKey, TForwardKey>(reversedOneWayMap);
        }

        public BiMap(IEqualityComparer<TForwardKey> forwardComparer, IEqualityComparer<TReverseKey> reverseComparer)
        {
            Forward = new Indexer<TForwardKey, TReverseKey>(forwardComparer ?? EqualityComparer<TForwardKey>.Default);
            Reverse = new Indexer<TReverseKey, TForwardKey>(reverseComparer ?? EqualityComparer<TReverseKey>.Default);
        }

        public BiMap(IDictionary<TForwardKey, TReverseKey> oneWayMap, IEqualityComparer<TForwardKey> forwardComparer, IEqualityComparer<TReverseKey> reverseComparer)
        {
            Forward = new Indexer<TForwardKey, TReverseKey>(oneWayMap, forwardComparer ?? EqualityComparer<TForwardKey>.Default);
            var reversedOneWayMap = oneWayMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, reverseComparer ?? EqualityComparer<TReverseKey>.Default);
            Reverse = new Indexer<TReverseKey, TForwardKey>(reversedOneWayMap, reverseComparer ?? EqualityComparer<TReverseKey>.Default);
        }

        public void Add(TForwardKey t1, TReverseKey t2)
        {
            if (Forward.ContainsKey(t1))
                throw new ArgumentException(DuplicateKeyErrorMessage, nameof(t1));
            if (Reverse.ContainsKey(t2))
                throw new ArgumentException(DuplicateKeyErrorMessage, nameof(t2));

            Forward.Add(t1, t2);
            Reverse.Add(t2, t1);
        }

        public bool TryAdd(TForwardKey t1, TReverseKey t2)
        {
            if (!Forward.TryAdd(t1, t2))
                return false;
            if (!Reverse.TryAdd(t2, t1))
            {
                Forward.Remove(t1); // Rollback
                return false;
            }
            return true;
        }

        public bool Remove(TForwardKey forwardKey)
        {
            if (Forward.ContainsKey(forwardKey) == false) return false;
            var reverseKey = Forward[forwardKey];
            bool success;
            if (Forward.Remove(forwardKey))
            {
                if (Reverse.Remove(reverseKey))
                {
                    success = true;
                }
                else
                {
                    Forward.Add(forwardKey, reverseKey);
                    success = false;
                }
            }
            else
            {
                success = false;
            }

            return success;
        }

        public int Count()
        {
            return Forward.Count();
        }

        IEnumerator<KeyValuePair<TForwardKey, TReverseKey>> IEnumerable<KeyValuePair<TForwardKey, TReverseKey>>.GetEnumerator()
        {
            return Forward.GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return Forward.GetEnumerator();
        }

        /// <summary>
        /// Publicly read-only lookup to prevent inconsistent state between forward and reverse map lookups
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        public class Indexer<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        {
            private readonly IDictionary<TKey, TValue> _dictionary;

            public Indexer()
            {
                _dictionary = new Dictionary<TKey, TValue>();
            }

            public Indexer(IDictionary<TKey, TValue> dictionary)
            {
                _dictionary = dictionary;
            }
            public Indexer(IEqualityComparer<TKey> comparer)
            {
                _dictionary = new Dictionary<TKey, TValue>(comparer);
            }

            public Indexer(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            {
                _dictionary = new Dictionary<TKey, TValue>(dictionary, comparer ?? EqualityComparer<TKey>.Default);
            }

            public TValue this[TKey index]
            {
                get { return _dictionary[index]; }
            }

            public int Count
            {
                get { return _dictionary.Count; }
            }

            public static implicit operator Dictionary<TKey, TValue>(Indexer<TKey, TValue> indexer)
            {
                return new Dictionary<TKey, TValue>(indexer._dictionary);
            }

            internal void Add(TKey key, TValue value)
            {
                _dictionary.Add(key, value);
            }

            internal bool TryAdd(TKey key, TValue value)
            {
                if (_dictionary.ContainsKey(key)) return false;
                _dictionary.Add(key, value);
                return true;
            }

            internal bool Remove(TKey key)
            {
                return _dictionary.Remove(key);
            }

            public bool ContainsKey(TKey key)
            {
                return _dictionary.ContainsKey(key);
            }

            public bool TryGetValue(TKey key, out TValue value)
            {
                return _dictionary.TryGetValue(key, out value);
            }

            public IEnumerable<TKey> Keys
            {
                get
                {
                    return _dictionary.Keys;
                }
            }

            public IEnumerable<TValue> Values
            {
                get
                {
                    return _dictionary.Values;
                }
            }

            /// <summary>
            /// Deep copy lookup as a dictionary
            /// </summary>
            /// <returns></returns>
            public Dictionary<TKey, TValue> ToDictionary()
            {
                return new Dictionary<TKey, TValue>(_dictionary);
            }

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _dictionary.GetEnumerator();
            }
        }
    }

    // From: https://discussions.unity.com/t/change-material-rendering-mode-in-runtime/143482
    public enum BlendMode
    {
        Opaque,
        Cutout,
        Fade,
        Transparent
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

        // From: https://discussions.unity.com/t/change-material-rendering-mode-in-runtime/143482
        public static void ChangeRenderMode(Material standardShaderMaterial, BlendMode blendMode)
        {
            switch (blendMode)
            {
                case BlendMode.Opaque:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    standardShaderMaterial.SetInt("_ZWrite", 1);
                    standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = -1;
                    break;
                case BlendMode.Cutout:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    standardShaderMaterial.SetInt("_ZWrite", 1);
                    standardShaderMaterial.EnableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = 2450;
                    break;
                case BlendMode.Fade:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    standardShaderMaterial.SetInt("_ZWrite", 0);
                    standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.EnableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = 3000;
                    break;
                case BlendMode.Transparent:
                    standardShaderMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    standardShaderMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    standardShaderMaterial.SetInt("_ZWrite", 0);
                    standardShaderMaterial.DisableKeyword("_ALPHATEST_ON");
                    standardShaderMaterial.DisableKeyword("_ALPHABLEND_ON");
                    standardShaderMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                    standardShaderMaterial.renderQueue = 3000;
                    break;
            }
        }

        public static void ResetRectTransform(RectTransform transform)
        {
                transform.localScale = Vector3.one;
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.anchoredPosition3D = Vector3.zero;
                transform.anchorMin = Vector2.zero;
                transform.anchorMax = Vector2.one;
                transform.offsetMin = Vector2.zero;
                transform.offsetMax = Vector2.zero;
        }
    }
}
