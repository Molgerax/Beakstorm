using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("illusionary", category:"func")]
    public class FuncIllusionary : MonoBehaviour, IOnImportFromMapEntity
    {
        [Tremble, SpawnFlags()] private bool _castShadows = false;
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            CoreUtils.Destroy(meshCollider);

            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = _castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
    }
}