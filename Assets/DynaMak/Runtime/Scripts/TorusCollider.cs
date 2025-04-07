using UnityEngine;

namespace DynaMak.Volumes
{
    public class TorusCollider : MonoBehaviour
    {
        
        public float radius = 2;
        public float thickness = 1;

        public Vector3 center { get => transform.position; }
        public Vector3 normal { get => transform.up; }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + transform.forward * radius, thickness);
            Gizmos.DrawWireSphere(transform.position - transform.forward * radius, thickness);
            Gizmos.DrawWireSphere(transform.position + transform.right * radius, thickness);
            Gizmos.DrawWireSphere(transform.position - transform.right * radius, thickness);
        }
    }
}