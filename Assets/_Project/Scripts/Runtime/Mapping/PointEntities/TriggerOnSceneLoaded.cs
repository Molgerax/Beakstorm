using Beakstorm.SceneManagement;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("on_load", "trigger", TrembleColors.TriggerOnSceneLoaded, size:16)]
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
