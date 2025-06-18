using Beakstorm.Core.Events;
using Eflatun.SceneReference;
using UnityEngine;

namespace Beakstorm.SceneManagement
{
    [CreateAssetMenu(menuName = "Beakstorm/Scenes/SceneCollection", fileName = "SceneCollection")]
    public class SceneLoadCollection : ScriptableObject
    {
        [SerializeField] private SceneReference[] scenesToLoad;
        [SerializeField] private bool loadAdditively;
        [SerializeField] private bool setFirstSceneActive;

        public SceneLoadData SceneLoadData => new SceneLoadData(scenesToLoad, setFirstSceneActive, loadAdditively);
    }
}
