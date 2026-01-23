using Beakstorm.Audio;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("peace_music", "misc", colour:"0.5 0.5 0.0", size:16)]
    public class PeaceMusicSetter : TriggerBehaviour
    {
        [SerializeField, Range(1, 5), Tremble] private int intensity = 1;
        
        public override void Trigger()
        {
            MusicStateManager.Instance.SetPeace(intensity - 1);
        }
    }
}
