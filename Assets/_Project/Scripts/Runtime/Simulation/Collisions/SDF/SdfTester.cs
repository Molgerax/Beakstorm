using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    public class SdfTester : MonoBehaviour
    {
        [SerializeField] private bool gradientNormal = false;
        [SerializeField] private int hits;
        private void OnDrawGizmos()
        {
            Vector3 normal;
            float dist;
            
            if (!SdfShapeManager.Instance)
                return;

            hits = SdfShapeManager.Instance.TestBvh(transform.position, 0, out dist, out normal, gradientNormal); 
            if (hits < 0)
                return;
            
            Gizmos.color = Color.HSVToRGB(hits / 4f, 1f, 1f);
            if (hits == 0) Gizmos.color = Color.black;
            
            Gizmos.DrawRay(transform.position, -normal * dist);
        }
    }
}
