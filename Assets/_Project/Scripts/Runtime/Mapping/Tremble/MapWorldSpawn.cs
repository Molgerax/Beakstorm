using System;
using Beakstorm.Core.Attributes;
using Beakstorm.Simulation.Collisions.SDF;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble
{
    public class MapWorldSpawn : Worldspawn
    {
        [SerializeField]
        [PowerOfTwo(4, 64)] private int sdfResolution = 32;
        [SerializeField] private SdfMaterialType sdfMaterialType = SdfMaterialType.None;
        [SerializeField] private float mapLowerBound = -256;
        
        [SerializeField, Range(1, 5)] private int peaceStartIndex = 1;

        public static MapWorldSpawn Instance;
        
        public int PeaceStartIndex => peaceStartIndex;
        public int SdfResolution => sdfResolution;
        public SdfMaterialType SdfMaterialType => sdfMaterialType;
        public float MapLowerBound => mapLowerBound;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
