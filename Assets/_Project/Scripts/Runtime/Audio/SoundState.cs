using UnityEngine;

namespace Beakstorm.Audio
{
    public class SoundState : MonoBehaviour
    {
        [SerializeField] private string stateGroup = "wave_State";
        [SerializeField] private string stateValue = "war1";


        public void SetState()
        {
            AkUnitySoundEngine.SetState(stateGroup, stateValue);
        }
    }
}
