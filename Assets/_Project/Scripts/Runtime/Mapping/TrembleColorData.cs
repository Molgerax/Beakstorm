using System;
using System.Collections.Generic;
using Beakstorm.Core.Interfaces;
using UnityEngine;

namespace Beakstorm.Mapping
{
    [CreateAssetMenu(fileName = "TrembleColorData", menuName = "Beakstorm/Tremble/ColorData", order = 0)]
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