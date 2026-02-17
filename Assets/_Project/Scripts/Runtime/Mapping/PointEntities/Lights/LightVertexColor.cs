using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities.Lights
{
    [PointEntity("vertex_color", category:"light")]
    public class LightVertexColor : MonoBehaviour
    {
        [SerializeField, Tremble] private Color color = Color.white;
        [SerializeField, Tremble] private float radius = 64;

        public Color Color => color;
        public float Radius => radius;
    }
}