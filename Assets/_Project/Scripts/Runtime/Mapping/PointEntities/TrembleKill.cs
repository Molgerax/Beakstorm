using TinyGoose.Tremble;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble
{
    [PointEntity("kill", category:"func", size: 16f, colour:"0 0.5 1.0")]
    public class TrembleKill : TriggerBehaviour, IOnImportFromMapEntity
    {
        public override void Trigger()
        {
            CoreUtils.Destroy(gameObject);
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
        }
    }
}