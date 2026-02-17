using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("liquid", category:"func", BrushType.Liquid)]
    public class FuncLiquid : MonoBehaviour, IOnImportFromMapEntity
    { 
        [Tremble, SpawnFlags()] private bool _castShadows = false;

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = _castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }
    }
}