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

        public void SetMaster(float v)
        {
            MasterVolume = v;
            ApplyMaster();
        }

        public void SetMusic(float v)
        {
            MusicVolume = v;
            ApplyMusic();
        }

        public void SetSfx(float v)
        {
            SfxVolume = v;
            ApplySfx();
        }

        public override void Apply()
        {
            ApplyMaster();
            ApplyMusic();
            ApplySfx();
        }
        
        private void ApplyMaster() => AkUnitySoundEngine.SetRTPCValue("volume_master", MasterVolume);
        private void ApplyMusic() => AkUnitySoundEngine.SetRTPCValue("volume_music", MusicVolume);
        private void ApplySfx() => AkUnitySoundEngine.SetRTPCValue("volume_sfx", SfxVolume);
    }
}
