using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping
{
    public abstract class TriggerSender : MonoBehaviour, IOnImportFromMapEntity
    {
        [SerializeField, NoTremble] protected Component[] targets;
        
        [Tremble("target")] private ITriggerTarget[] _targets;

        public void SendTrigger()
        {
            targets.TryTrigger();
        }
        
        public virtual void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            targets = _targets.TriggerToComponent();
        }
    }
}