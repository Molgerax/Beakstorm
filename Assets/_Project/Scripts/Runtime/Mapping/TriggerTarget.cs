using UnityEngine;

namespace Beakstorm.Mapping
{
    public interface ITriggerTarget
    {
        public void Trigger(TriggerData data);
    }

    public struct TriggerData
    {
        private bool _deactivate;

        public bool Activate
        {
            get => !_deactivate;
            set => _deactivate = !value;
        }

        public TriggerData(bool activate)
        {
            _deactivate = !activate;
        }

        public static TriggerData Active = new TriggerData(true);
        public static TriggerData Deactive = new TriggerData(false);
    }
}
