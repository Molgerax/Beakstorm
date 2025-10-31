using Beakstorm.SceneManagement;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Triggers
{
    [PointEntity("on_load", "trigger", colour:"1.0, 0.5, 0.0", size:16)]
    public class TriggerOnSceneLoaded : MonoBehaviour
    {
        [SerializeField] private Component target;

        private void OnLoad()
        {
            target.TryTrigger();
        }
        
        private void Awake()
        {
            if (GlobalSceneLoader.IsLoaded(OnLoad))
                OnLoad();
        }
    }
}
