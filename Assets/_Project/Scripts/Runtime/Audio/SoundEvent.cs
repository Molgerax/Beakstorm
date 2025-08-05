using UnityEngine;

namespace Beakstorm.Audio
{
    public class SoundEvent : MonoBehaviour
    {
        [SerializeField] private string eventName = "play_birdAttack";

        private uint _postId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;

        public void PostEvent()
        {
            _postId = AkUnitySoundEngine.PostEvent(eventName, this.gameObject);
        }

        public void StopEvent()
        {
            AkUnitySoundEngine.StopPlayingID(_postId);
        }
    }
}
