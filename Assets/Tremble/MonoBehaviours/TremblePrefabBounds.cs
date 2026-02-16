using UnityEngine;

namespace TinyGoose.Tremble
{
    public class TremblePrefabBounds : MonoBehaviour
    {
        [SerializeField] private Bounds bounds;

        public Bounds Bounds => bounds;


        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Bounds.center, Bounds.size);
        }
    }
}