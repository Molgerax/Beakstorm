using Beakstorm.Gameplay.Encounters.Procedural;
using UnityEditor;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters
{
    [CustomEditor(typeof(WaveDataSO))]
    public class WaveDataSoEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            WaveDataSO dataSo = (WaveDataSO) target;

            for (var index = 0; index < dataSo.SpawnDataEntries.Count; index++)
            {
                EnemySpawnDataEntry entry = dataSo.SpawnDataEntries[index];

                if (entry.IsValid)
                {
                    DrawHandle(ref entry);
                    dataSo.SpawnDataEntries[index] = entry;
                }
            }
        }

        private void DrawHandle(ref EnemySpawnDataEntry entry)
        {
            int danger = entry.enemy.DangerRating;
            Color c = Color.HSVToRGB(danger, 1f, 1f);
            
            Handles.color = c;

            EditorGUI.BeginChangeCheck();
            Vector3 pos = entry.transformData.Position;
            Quaternion rot = entry.transformData.Rotation;

            float radius = 10f;
            float thickness = 2f;

            if (Tools.current == Tool.Move)
                pos = Handles.PositionHandle(entry.transformData.Position, entry.transformData.Rotation);
            
            if (Tools.current == Tool.Rotate)
                rot = Handles.RotationHandle(entry.transformData.Rotation, entry.transformData.Position);

            Handles.DrawWireDisc(pos, rot * Vector3.up, radius, thickness);
            Handles.DrawLine(pos, pos + (rot * Vector3.forward) * radius, thickness);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed WaveDataSO");
                entry.transformData = new TransformData(pos, rot);
            }
        }
        
        
        
        [MenuItem("CONTEXT/" + nameof(EncounterWave) + "/Convert to WaveData Asset")]
        private static void CreateWaveDataAsset(MenuCommand command)
        {
            EncounterWave wave = (EncounterWave)command.context;

            string path = EditorUtility.SaveFilePanelInProject("Save WaveData As Object", "NewWaveData", 
                "asset", "Save selected EncounterWave as an asset.");
            
            if (string.IsNullOrEmpty(path))
                return;

            WaveDataSO so = ScriptableObject.CreateInstance<WaveDataSO>();

            foreach (var data in wave.SpawnData)
            {
                EnemySpawnDataEntry entry = new(data.spawner, data.spawnDelay, (EnemySpawnDataEntry.WaitCondition)(int)data.waitCondition);
                so.SpawnDataEntries.Add(entry);
            }
            
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
        }
    }
}
