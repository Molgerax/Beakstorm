using UnityEngine;

namespace Beakstorm.Settings
{
    [CreateAssetMenu(menuName = "Beakstorm/Settings/GraphicsSettings", fileName = "NewGraphicsSettings")]
    public class GraphicsSettings : AbstractSettingsData<GraphicsSettings>
    {
        public override string FileName => "GraphicsSettings";


        public bool FullScreen = true;
        public bool VSync = true;

        public void SetFullScreen(bool value) => FullScreen = value;
        public void SetVsync(bool value) => VSync = value;
        
        public override void Apply()
        {
            QualitySettings.vSyncCount = VSync ? 1 : 0;

            Screen.fullScreen = FullScreen;
        }
    }
}
