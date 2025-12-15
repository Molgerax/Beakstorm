using System;
using UnityEngine;

namespace Beakstorm.Simulation.Settings
{
    public class ParticleSimulationBorders : MonoBehaviour
    {
        [SerializeField] private Vector3 size = Vector3.one * 1024;

        public Vector3 Center => transform.position;
        public Vector3 Size => size;
        
        public static ParticleSimulationBorders Instance { get; private set; }

        public Bounds Bounds => new Bounds(Center, Size);

        public Vector3 SnapCenterToBounds(Vector3 inCenter, Vector3 inSize)
        {
            Vector3 inMin = inCenter - inSize * 0.5f;
            Vector3 inMax = inCenter + inSize * 0.5f;

            Vector3 min = Bounds.min;
            Vector3 max = Bounds.max;

            Vector3 resultMin = Vector3.Max(inMin, min);
            Vector3 resultMax = Vector3.Min(inMax, max);

            Vector3 centerOffset = resultMax - inSize * 0.5f;

            if (resultMin.x > inMin.x) centerOffset.x = resultMin.x + inSize.x * 0.5f;
            if (resultMin.y > inMin.y) centerOffset.y = resultMin.y + inSize.y * 0.5f;
            if (resultMin.z > inMin.z) centerOffset.z = resultMin.z + inSize.z * 0.5f;

            return centerOffset;
        }
        
        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.bisque;
            Gizmos.DrawWireCube(Center, Size);
        }
    }
}