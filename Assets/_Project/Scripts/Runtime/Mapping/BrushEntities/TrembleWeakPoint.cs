using System.Linq;
using Beakstorm.Mapping.PointEntities;
using Beakstorm.Mapping.Tremble;
using Beakstorm.Simulation.Collisions;
using Beakstorm.Simulation.Collisions.SDF.Shapes;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("weak_point", "func", BrushType.Solid)]
    public class TrembleWeakPoint : MonoBehaviour, IOnImportFromMapEntity
    {
        [SerializeField, Tremble("target")] private TriggerBehaviour[] targets;
        [SerializeField, Tremble("health")] private int health = 100;
        [SerializeField, Tremble("type")] private WeakPointData data;
        [SerializeField, Tremble("kill")] private bool kill = true;
        [SerializeField, Tremble("collision")] private bool collision = true;

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            SdfBox sdfBox = gameObject.AddComponent<SdfBox>();
            sdfBox.SetDimensions(boxCollider.center, boxCollider.size);
            CoreUtils.Destroy(boxCollider);

            if (kill)
            {
                targets ??= new TriggerBehaviour[0];
                var list = targets.ToList();
                list.Add(gameObject.AddComponent<TrembleKill>());
                targets = list.ToArray();
            }
            
            if (!collision)
                CoreUtils.Destroy(gameObject.GetComponent<MeshCollider>());

            var weakPoint = gameObject.AddComponent<WeakPoint>();
            weakPoint.SetFromTremble(targets, health, data);
        }
    }
}