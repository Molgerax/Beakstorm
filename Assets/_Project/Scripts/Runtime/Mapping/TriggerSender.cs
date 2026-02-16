using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping
{
    public abstract class TriggerSender : MonoBehaviour, IOnImportFromMapEntity
    {
        [SerializeField, NoTremble] protected Component[] targets;
        
        [Tremble("target")] private ITriggerTarget[] _targets;

        [SerializeField, NoTremble] protected bool invertSignal;
        [Tremble, SpawnFlags(8)] private bool _invertSignal;

        public void SendTrigger(TriggerData data = default)
        {
            if (invertSignal)
                data.Activate = !data.Activate;
            
            targets.TryTrigger(data);
        }
        
        public virtual void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            targets = _targets.TriggerToComponent();
            invertSignal = _invertSignal;
        }
    }
}