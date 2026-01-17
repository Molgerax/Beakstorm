using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Beakstorm.SceneManagement.Editor
{
    [InitializeOnLoad]
    public static class PlayFromBootScene
    {
        private const string EDITOR_PREF_SCENE_COUNT = "OpenedSceneCount";
        private const string EDITOR_PREF_SCENE_ACTIVE = "OpenedSceneActiveIndex";
        private const string EDITOR_PREF_SCENE_PREFIX = "OpenedScenePath_";

        private static List<string> _openScenes;
        private static int _activeSceneIndex;

        
        private const string EDITOR_PREF_PLAY_FROM_BOOT = "PlayFromBootOption";
        private const string MENU_TOGGLE_PLAY_FROM_BOOT = "Play/Play from Boot Scene";
        private static bool _playFromBoot = true;


        static PlayFromBootScene()
        {
            if (EditorPrefs.HasKey(EDITOR_PREF_PLAY_FROM_BOOT))
                _playFromBoot = EditorPrefs.GetBool(EDITOR_PREF_PLAY_FROM_BOOT);
            Menu.SetChecked(MENU_TOGGLE_PLAY_FROM_BOOT, _playFromBoot);
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }


        [MenuItem(MENU_TOGGLE_PLAY_FROM_BOOT, true)]
        public static bool TogglePlayFromBootValidate()
        {
            return !EditorApplication.isPlaying;
        }
        
        [MenuItem(MENU_TOGGLE_PLAY_FROM_BOOT)]
        public static void TogglePlayFromBoot()
        {
            _playFromBoot = !_playFromBoot;
            EditorPrefs.SetBool(EDITOR_PREF_PLAY_FROM_BOOT, _playFromBoot);
            Menu.SetChecked(MENU_TOGGLE_PLAY_FROM_BOOT, _playFromBoot);
        }
        
        public static void Play()
        {
            _openScenes = new List<string>();
            string activeScene = SceneManager.GetActiveScene().path;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                string path = SceneManager.GetSceneAt(i).path;
                if (path == activeScene)
                    _activeSceneIndex = i;
                _openScenes.Add(path);
            }
            SaveToEditorPrefs();

            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(EditorBuildSettings.globalScenes[0].path);
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (!_playFromBoot)
                return;
            
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Play();
            }

            if (EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.update += ReturnToScenes;
            }
        }


        private static void ReturnToScenes()
        {
            if (!EditorApplication.isPlaying)
            {
                LoadFromEditorPrefs();
                
                for (int i = 0; i < _openScenes.Count; i++)
                {
                    Scene s;
                    string scene = _openScenes[i];
                    if (i == 0)
                        s = EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
                    else                
                        s = EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);

                    if (i == _activeSceneIndex)
                        SceneManager.SetActiveScene(s);
                }
                
                EditorApplication.update -= ReturnToScenes;
            }
        }

        private static void SaveToEditorPrefs()
        {
            if (EditorPrefs.HasKey(EDITOR_PREF_SCENE_COUNT))
            {
                int previousSceneCount = EditorPrefs.GetInt(EDITOR_PREF_SCENE_COUNT);

                for (int i = 0; i < previousSceneCount; i++)
                {
                    EditorPrefs.DeleteKey($"{EDITOR_PREF_SCENE_PREFIX}{i}");
                }
            }

            for (int i = 0; i < _openScenes.Count; i++)
            {
                EditorPrefs.SetString($"{EDITOR_PREF_SCENE_PREFIX}{i}", _openScenes[i]);
            }
            EditorPrefs.SetInt(EDITOR_PREF_SCENE_COUNT, _openScenes.Count);
            EditorPrefs.SetInt(EDITOR_PREF_SCENE_ACTIVE, _activeSceneIndex);
        }
        
        private static void LoadFromEditorPrefs()
        {
            _openScenes = new List<string>();
            if (EditorPrefs.HasKey(EDITOR_PREF_SCENE_COUNT))
            {
                int previousSceneCount = EditorPrefs.GetInt(EDITOR_PREF_SCENE_COUNT);
                
                for (int i = 0; i < previousSceneCount; i++)
                {
                    string scene = EditorPrefs.GetString($"{EDITOR_PREF_SCENE_PREFIX}{i}");
                    _openScenes.Add(scene);
                }

                _activeSceneIndex = EditorPrefs.GetInt(EDITOR_PREF_SCENE_ACTIVE);
            }
        }
    }
}
