using UnityEngine;

namespace Beakstorm.Settings
{
    [CreateAssetMenu(menuName = "Beakstorm/Settings/AudioSettings", fileName = "NewAudioSettings")]
    public class AudioSettings : AbstractSettingsData<AudioSettings>
    {
        public override string FileName => "AudioSettings";

        public float MasterVolume = 0.5f;
        public float MusicVolume = 1f;
        public float SfxVolume = 1f;

        public void SetMaster(float v) => MasterVolume = v;
        public void SetMusic(float v) => MusicVolume = v;
        public void SetSfx(float v) => SfxVolume = v;
        
        public override void Apply()
        {
            AkUnitySoundEngine.SetRTPCValue("volume_master", MasterVolume);
            AkUnitySoundEngine.SetRTPCValue("volume_music", MusicVolume);
            AkUnitySoundEngine.SetRTPCValue("volume_sfx", SfxVolume);
        }
    }
}
