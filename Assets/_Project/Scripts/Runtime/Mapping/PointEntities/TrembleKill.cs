using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("kill", category:"func", TrembleColors.TrembleKill, size: 16f)]
    public class TrembleKill : MonoBehaviour, ITriggerTarget, IOnImportFromMapEntity
    {
        public void Trigger()
        {
            CoreUtils.Destroy(gameObject);
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
        }
    }
}