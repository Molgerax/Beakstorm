using Beakstorm.Gameplay.Encounters.Procedural;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Beakstorm.Gameplay.Encounters
{
    [CustomEditor(typeof(EncounterList))]
    public class EncounterListEditor : Editor
    {
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
        }

        private void DuringSceneGUI(SceneView sceneView)
        {
            EncounterList list = (EncounterList) target;

            foreach (var wave in list.Waves)
            {
                if (wave.WaveData)
                {
                    WaveDataSoEditor.DrawPreview(wave.WaveData);
                }
            }
        }
    }
}
