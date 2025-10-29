using UnityEngine;

namespace Beakstorm.Audio
{
    public class SoundEvent : MonoBehaviour
    {
        [SerializeField] private AK.Wwise.Event eventReference;
        [SerializeField] private GameObject target;

        [SerializeField] private bool autoStartStop = false;
        
        private uint _postId = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        
        private GameObject Target => target ? target : gameObject;

        private void OnEnable()
        {
            if (autoStartStop) PostEvent();
        }

        private void OnDisable()
        {
            if (autoStartStop) StopEvent();
        }

        public void PostEvent()
        {
            if (eventReference != null)
                _postId = eventReference.Post(Target);
        }

        
        public void StopEvent()
        {
            eventReference.Stop(Target);
            AkUnitySoundEngine.StopPlayingID(_postId);
        }
        
        public void StopEventFadeOut(float time)
        {
            eventReference.Stop(Target, Mathf.CeilToInt(time * 1000));
        }
    }
}
