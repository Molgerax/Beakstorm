using UnityEngine;

namespace Beakstorm.Gameplay.Encounters
{
    [CreateAssetMenu(fileName = "EncounterProfile", menuName = "Beakstorm/Encounter/EncounterProfile")]
    [System.Serializable]
    public class EncounterProfile : ScriptableObject
    {
        [SerializeField] public AK.Wwise.State MusicState;
        [SerializeField] public AK.Wwise.State WaveState;


        public void ApplyProfile()
        {
            if (MusicState.IsValid())
                MusicState.SetValue();

            if (WaveState.IsValid())
                WaveState.SetValue();
        }
    }
}
