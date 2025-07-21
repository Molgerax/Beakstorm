using UnityEngine;

namespace Beakstorm.Audio
{
    public class SoundEvent : MonoBehaviour
    {
        [SerializeField] private string eventName = "play_birdAttack";


        public void PostEvent()
        {
            AkUnitySoundEngine.PostEvent(eventName, this.gameObject);
        }
    }
}
