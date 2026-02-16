using Beakstorm.SceneManagement;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("scene_loader", "trigger")]
    public class TrembleSceneLoader : MonoBehaviour, ITriggerTarget, IOnImportFromMapEntity
    {
        [SerializeField, Tremble("scene")] private SceneLoadCollection scene;

        [SerializeField, NoTremble] private SceneLoadPoster sceneLoadPoster;

        private bool _triggered;
        
        public void Trigger(TriggerData data)
        {
            if (_triggered)
                return;
            _triggered = true;
            
            sceneLoadPoster.LoadScene();
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            sceneLoadPoster = gameObject.GetOrAddComponent<SceneLoadPoster>();
            sceneLoadPoster.SetCollection(scene);
            sceneLoadPoster.SetChannel();
        }
    }
}
