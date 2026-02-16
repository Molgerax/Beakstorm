using System;
using System.Collections.Generic;
using UnityEngine;

namespace TinyGoose.Tremble
{
    [CreateAssetMenu(fileName = "TrembleColorData", menuName = "Tremble/ColorData", order = 10)]
    public class TrembleColorData : ScriptableObject
    {
        [SerializeField] public List<DataPair> pairs = new();
        
        [Serializable]
        public class DataPair
        {
            public SerializableType Type;
            public Color Color;

            public DataPair(Type type, Color color)
            {
                Type = type;
                Color = color;
            }
        }
    }
}