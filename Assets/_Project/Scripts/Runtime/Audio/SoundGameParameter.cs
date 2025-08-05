using UnityEngine;

namespace Beakstorm.Audio
{
    public class SoundGameParameter : MonoBehaviour
    {
        [SerializeField] private string eventName = "player_Speed";


        public void SetParameter(float value)
        {
            AkUnitySoundEngine.SetRTPCValue(eventName, value);
        }
    }
}
