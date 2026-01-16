using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Utility
{
    /// <summary>
    /// An array that maintains the location of its elements. Removed items free up space that can be filled by new ones.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoFilledArray<T> where T : class
    {
        private T[] _array;

        private Dictionary<int, T> _dictionary;
        private Queue<int> _freeIndices;
        private List<int> _fullIndices;
        private HashSet<T> _hashSet;

        private int _count;
        private int _size;

        private readonly bool _limitToSize;
        

        public int Count => _count;
        public int Size => _size;

        public int IterateCount => Mathf.Min(_count, _size);

        public bool NeedsResize => _count >= _size;

        public static implicit operator T[](AutoFilledArray<T> a) => a._array;

        public T this[int index] => _array[index];

        public bool TryGetIndex(T value, out int index)
        {
            index = 0;

            if (!_hashSet.Contains(value))
                return false;
            for (int i = 0; i < IterateCount; i++)
            {
                if (_array[i] == value)
                {
                    index = i;
                    return true;
                }
            }
            return false;
        }

        public AutoFilledArray(int size, bool limitToSize = false)
        {
            _size = size;
            _array = new T[size];
            _dictionary = new();
            _hashSet = new(size);

            _freeIndices = new(size);
            _fullIndices = new(size);

            _limitToSize = limitToSize;
        }

        public bool AddElement(T value)
        {
            if (_hashSet.Contains(value))
                return false;

            if (_limitToSize && _count >= _size)
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

        private void MoveUnusedElementToFreeSpot()
        {
            if (_freeIndices.Count == 0)
                return;

            if (_count <= _size)
                return;

            int index = _count - 1;
            if (_dictionary.TryGetValue(index, out T value))
            {
                if(_freeIndices.TryDequeue(out int freeIndex))
                {
                    if (_dictionary.TryGetValue(freeIndex, out T _))
                        _dictionary[freeIndex] = value;
                    
                    else
                        _dictionary.Add(freeIndex, value);

                    _dictionary.Remove(index);
                    _fullIndices.Remove(index);
                    _fullIndices.Add(freeIndex);
                    _count--;
                }
            }
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
                T element = _dictionary[fullIndex];
                if (element == value || element == null)
                {
                    _dictionary[fullIndex] = null;
                    _freeIndices.Enqueue(fullIndex);
                    _fullIndices.RemoveAt(i);

                    MoveUnusedElementToFreeSpot();
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
            else if (!_hashSet.Contains(element))
            {
                _dictionary.Remove(lastIndex);
                _fullIndices.Remove(lastIndex);
                _count--;
            }
        }
    }
}
