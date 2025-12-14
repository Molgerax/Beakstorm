using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble
{
    [BrushEntity("cloud", "misc", BrushType.Solid)]
    public class TrembleMarchingCubes : MonoBehaviour
    {
        [Tremble("surface")] public float surface = 0f;
        [Tremble("smoothing")] public float smoothing = 2f;
    }
}
