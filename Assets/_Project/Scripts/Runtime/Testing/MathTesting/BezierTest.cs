using Beakstorm.Utility;
using UnityEngine;

namespace Beakstorm.Testing.MathTesting
{
    public class BezierTest : MonoBehaviour
    {
        [SerializeField] private Transform target;

        [SerializeField] private float multA = 1;
        [SerializeField] private float multB = 1;
        
        private void OnDrawGizmos()
        {
            if (!target)
                return;

            Transform trans = transform;

            int resolution = 32;

            var pos = trans.position;
            Vector3 a = pos;
            Vector3 b = pos + trans.forward * multA;
            var targetPos = target.position;
            Vector3 c = targetPos + target.forward * multB;
            Vector3 d = targetPos;
            
            Vector3 oldPos = pos;
            
            Gizmos.color = Color.yellow;
            
            Gizmos.DrawSphere(a, 0.5f);
            for (int i = 0; i < resolution; i++)
            {
                float t = (i + 1f) / (resolution);
                
                Vector3 p = BezierMath.BezierPos(a, b, c, d, t);
                
                Gizmos.DrawSphere(p, 0.1f);
                Gizmos.DrawLine(oldPos, p);
                oldPos = p;
            }
        }
    }
}
