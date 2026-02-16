using Beakstorm.SceneManagement;
using TinyGoose.Tremble;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("on_load", "trigger")]
    public class TriggerOnSceneLoaded : TriggerSender, IOnSceneLoad
    {
        public SceneLoadCallbackPoint SceneLoadCallbackPoint => SceneLoadCallbackPoint.AfterAll;
        
        private void Awake()
        {
            GlobalSceneLoader.ExecuteWhenLoaded(this);
        }

        public void OnSceneLoaded()
        {
            SendTrigger();
        }

    }
}
