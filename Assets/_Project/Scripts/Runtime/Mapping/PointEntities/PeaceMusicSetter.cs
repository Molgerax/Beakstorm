using Beakstorm.Audio;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("peace_music", "misc")]
    public class PeaceMusicSetter : MonoBehaviour, ITriggerTarget
    {
        [SerializeField, Range(1, 5), Tremble] private int intensity = 1;
        
        public void Trigger(TriggerData data)
        {
            MusicStateManager.Instance.SetPeace(intensity - 1);
        }
    }
}
