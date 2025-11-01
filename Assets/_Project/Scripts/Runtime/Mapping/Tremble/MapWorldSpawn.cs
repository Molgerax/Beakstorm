using Beakstorm.Core.Attributes;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble
{
    public class MapWorldSpawn : Worldspawn
    {
        [SerializeField]
        [PowerOfTwo(4, 64)] private int sdfResolution = 32;
        [SerializeField, Range(1, 5)] private int peaceStartIndex = 1;

        
        public int PeaceStartIndex => peaceStartIndex;
        public int SdfResolution => sdfResolution;
    }
}
