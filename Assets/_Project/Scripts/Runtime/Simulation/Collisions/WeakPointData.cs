using UnityEngine;

namespace Beakstorm.Simulation.Collisions
{
    [CreateAssetMenu(fileName = "WeakPointData", menuName = "Beakstorm/WeakPoint/WeakPointData")]
    public class WeakPointData : ScriptableObject
    {
        [Header("Sound")]
        [SerializeField] private AK.Wwise.Event damageSound;
        [SerializeField] private AK.Wwise.Event destroySound;

        public void OnDamage(WeakPoint wp, int damage = 0)
        {
            if (damageSound != null)
                damageSound.Post(wp.SoundSource);
        }

        public void OnDefeat(WeakPoint wp)
        {
            if (destroySound != null)
                destroySound.Post(wp.SoundSource);
        }
    }
}