using Beakstorm.SceneManagement;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("kill", category:"func")]
    public class FuncKill : MonoBehaviour, ITriggerTarget, IOnImportFromMapEntity, IOnSceneLoad
    {
        [SerializeField, NoTremble] private Component[] targets;
        [Tremble("target")] private ITriggerTarget[] _targets;
        
        public SceneLoadCallbackPoint SceneLoadCallbackPoint => SceneLoadCallbackPoint.WhenLevelStarts;
    
        public void Trigger(TriggerData data)
        {
            SetTargetsActive(false);
        }

        private void SetTargetsActive(bool value)
        {
            foreach (Component target in targets)
            {
                target.gameObject.SetActive(value);
            }
        }

        public void OnSceneLoaded()
        {
            SetTargetsActive(true);
        }


        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            targets = _targets.TriggerToComponent();
        }
    }
}