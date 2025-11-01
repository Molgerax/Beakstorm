using Beakstorm.SceneManagement;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Triggers
{
    [PointEntity("on_load", "trigger", colour:"1.0, 0.5, 0.0", size:16)]
    public class TriggerOnSceneLoaded : MonoBehaviour, IOnSceneLoad
    {
        [SerializeField] private Component target;

        public SceneLoadCallbackPoint SceneLoadCallbackPoint => SceneLoadCallbackPoint.AfterAll;
        
        private void Awake()
        {
            GlobalSceneLoader.ExecuteWhenLoaded(this);
        }

        public void OnSceneLoaded()
        {
            target.TryTrigger();
        }

    }
}
