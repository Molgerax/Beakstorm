using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Utility
{
    public class AutoFilledArray<T> where T : class
    {
        private T[] _array;

        private Dictionary<int, T> _dictionary;
        private Queue<int> _freeIndices;
        private List<int> _fullIndices;
        private HashSet<T> _hashSet;

        private int _count;
        private int _size;

        public int Count => _count;
        public int Size => _size;

        public bool NeedsResize => _count >= _size;

        public static implicit operator T[](AutoFilledArray<T> a) => a._array;

        public T this[int index] => _array[index];

        public AutoFilledArray(int size)
        {
            _size = size;
            _array = new T[size];
            _dictionary = new();
            _hashSet = new(size);

            _freeIndices = new(size);
            _fullIndices = new(size);
        }

        public bool AddElement(T value)
        {
            if (_hashSet.Contains(value))
                return false;

            FreeNull();
            
            
            if (_freeIndices.TryDequeue(out int index))
            {
                bool freeIndex = true;
                while (freeIndex && index >= _count)
                { 
                    freeIndex = _freeIndices.TryDequeue(out index);
                }

                if (freeIndex)
                {
                    _dictionary[index] = value;
                    _fullIndices.Add(index);
                    _hashSet.Add(value);
                    return true;
                }
            }

            index = _count++;
            _dictionary.Add(index, value);
            _fullIndices.Add(index);
            _hashSet.Add(value);
            return true;
        }

        public void UpdateArray()
        {
            for (int i = 0; i < Mathf.Min(_count, _size); i++)
            {
                _array[i] = _dictionary[i];
            }
            FreeNull();
            
            FreeLastElementIfNull();
        }

        public bool Contains(T value) => _hashSet.Contains(value);

        public void Resize(int newSize)
        {
            _array = new T[newSize];
            _size = newSize;
            UpdateArray();
        }
        
        public bool RemoveElement(T value)
        {
            if (!_hashSet.Remove(value))
                return false;

            for (int i = _fullIndices.Count - 1; i >= 0; i--)
            {
                int fullIndex = _fullIndices[i];
                if (_dictionary[fullIndex] == value)
                {
                    _dictionary[fullIndex] = null;
                    _freeIndices.Enqueue(fullIndex);
                    _fullIndices.RemoveAt(i);
                }
                else if (_dictionary[fullIndex] == null)
                {
                    _freeIndices.Enqueue(fullIndex);
                    _fullIndices.RemoveAt(i);
                }
            }
            
            return true;
        }
        
        
        private void FreeNull()
        {
            for (var i = _fullIndices.Count - 1; i >= 0; i--)
            {
                int fullIndex = _fullIndices[i];
                T item = _dictionary[fullIndex];
                if (item == null)
                {
                    _hashSet.Remove(null);
                    _freeIndices.Enqueue(fullIndex);
                    _fullIndices.RemoveAt(i);
                }
            }
        }

        private void FreeLastElementIfNull()
        {
            if (_count <= 0)
                return;
        
            int lastIndex = _count - 1;
            T element = _dictionary[lastIndex];
            if (element == null)
            {
                _dictionary.Remove(lastIndex);
                _fullIndices.Remove(lastIndex);
                _count--;
            }
        }
    }
}
