using System;

namespace Beakstorm.SceneManagement
{
    public interface IOnSceneLoad
    {
        public void OnSceneLoaded();
        public SceneLoadCallbackPoint SceneLoadCallbackPoint { get; }
    }

    public static class OnSceneLoadExtensions
    {
        public static int CompareTo(this IOnSceneLoad onSceneLoad, IOnSceneLoad obj)
        {
            IOnSceneLoad a = onSceneLoad;
            var b = obj;
            if (b == null)
                return 0;

            if (a.SceneLoadCallbackPoint > b.SceneLoadCallbackPoint)
                return 1;
            if (a.SceneLoadCallbackPoint < b.SceneLoadCallbackPoint)
                return -1;
            return 0;
        }
    }


    public enum SceneLoadCallbackPoint
    {
        First = 0,
        Second = 1,
        Third = 2,
        WhenLevelStarts = 8,
        AfterAll = 10,
    }
}