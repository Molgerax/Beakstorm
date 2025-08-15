using Beakstorm.Gameplay.Encounters.Procedural;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace Beakstorm.Gameplay.Encounters
{
    [CustomEditor(typeof(WaveDataSO))]
    public class WaveDataSoEditor : Editor
    {
        private int _selectedIndex = -1;

        private SerializedProperty _listProperty;

        private ReorderableList _list;

        private bool _resetBounds;
        private Bounds _totalBounds;

        private Vector3 _totalCenter;

        private Quaternion _previousRotation;

        private int _hotControl;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            _list?.DoLayoutList();
            
            if (GUILayout.Button("Select All"))
            {
                _list?.ClearSelection();
                _selectedIndex = -1;
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            _selectedIndex = -1;
            
            _previousRotation = Quaternion.identity;

            _listProperty = serializedObject.FindProperty("spawnDataEntries");
            
            //_list = ReorderableList.GetReorderableListFromSerializedProperty(_listProperty);
            _list = new ReorderableList(serializedObject, _listProperty);
            _list.drawElementCallback += DrawListItems;
            _list.drawHeaderCallback += DrawListHeader;
            _list.elementHeightCallback += ElementHeightCallback;
        }

        private float ElementHeightCallback(int index)
        {
            SerializedProperty element = _list.serializedProperty.GetArrayElementAtIndex(index); //The element in the list
            return EditorGUI.GetPropertyHeight(element, true);
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            WaveDataSO dataSo = (WaveDataSO) target;

            if (GUIUtility.hotControl != _hotControl)
            {
                _hotControl = GUIUtility.hotControl;
                _previousRotation = Quaternion.identity;
            }

            _resetBounds = true;
            _totalCenter = Vector3.zero;
            int count = dataSo.SpawnDataEntries.Count;
            for (var index = 0; index < count; index++)
            {
                EnemySpawnDataEntry entry = dataSo.SpawnDataEntries[index];

                DrawHandle(ref entry, index, sceneView);
                dataSo.SpawnDataEntries[index] = entry;
            }
            _totalCenter /= Mathf.Max(1, count);
            
            DrawHandleAll(dataSo, sceneView);
        }

        private void DrawHandle(ref EnemySpawnDataEntry entry, int index, SceneView sceneView)
        {
            int danger = entry.enemy.DangerRating;
            float selectedTint = (index == _selectedIndex) ? 1f : 0.5f;
            Color c = Color.HSVToRGB(danger/10f, 1f, 1f);
            c.a = selectedTint;
            
            Handles.color = c;
            
            Vector3 pos = entry.transformData.Position;
            Quaternion rot = entry.transformData.Rotation;

            Bounds modelBounds = new Bounds(Vector3.zero, Vector3.one * 5);
            
            if (entry.IsValid)
                modelBounds = entry.enemy.Prefab.Bounds;
            Bounds bounds = new Bounds(pos, modelBounds.size);

            if (_resetBounds)
            {
                _totalBounds = bounds;
                _resetBounds = false;
            }
            _totalBounds.Encapsulate(bounds);
            _totalCenter += pos;
            
            float radius = 10f;
            float thickness = 2f;

            if (entry.IsValid)
                radius = entry.enemy.Prefab.Bounds.extents.magnitude;

            GUIContent label = new GUIContent($"{index}: <Invalid>");
            if (entry.IsValid)
                label.text = $"{index}: {entry.enemy.name}";
            Handles.Label(pos, label);
            Handles.DrawWireDisc(pos, rot * Vector3.up, radius, thickness);
            Handles.DrawLine(pos, pos + (rot * Vector3.forward) * radius, thickness);

            Matrix4x4 mat = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(pos, rot, bounds.size);
            Handles.DrawWireCube(Vector3.zero, Vector3.one);
            Handles.matrix = mat;
            
            if (index != _selectedIndex)
                return;

            EditorGUI.BeginChangeCheck();
            if (Tools.current == Tool.Move)
                pos = Handles.PositionHandle(entry.transformData.Position, entry.transformData.Rotation);
            
            if (Tools.current == Tool.Rotate)
                rot = Handles.RotationHandle(entry.transformData.Rotation, entry.transformData.Position);

            if (Event.current.isKey && Event.current.keyCode == KeyCode.F && Event.current.shift == false)
            {
                sceneView.Frame(bounds, false);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed WaveDataSO");
                entry.transformData = new TransformData(pos, rot);
            }
        }


        private void DrawHandleAll(WaveDataSO dataSo, SceneView sceneView)
        {
            if (dataSo.SpawnDataEntries.Count == 0)
                return;

            if ((_selectedIndex == -1 || Event.current.shift) && 
                Event.current.isKey && Event.current.keyCode == KeyCode.F)
            {
                sceneView.Frame(_totalBounds, false);
            }
            
            if (_selectedIndex != -1)
                return;
            
            Vector3 pos = _totalCenter;
            Vector3 startPos = pos;
            Quaternion rot = Quaternion.identity;

            EditorGUI.BeginChangeCheck();
            if (Tools.current == Tool.Move)
                pos = Handles.PositionHandle(pos, rot);

            if (Tools.current == Tool.Rotate)
                rot = Handles.RotationHandle(rot, pos);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(target, "Changed WaveDataSO");
                ApplyPositionAndRotation(dataSo, startPos, pos, rot * Quaternion.Inverse(_previousRotation));
                _previousRotation = rot;
            }
        }

        private void ApplyPositionAndRotation(WaveDataSO dataSo, Vector3 startPos, Vector3 endPos, Quaternion rot)
        {
            for (var index = 0; index < dataSo.SpawnDataEntries.Count; index++)
            {
                EnemySpawnDataEntry entry = dataSo.SpawnDataEntries[index];
                TransformData tf = entry.transformData;
                tf.Position += (endPos - startPos);
                tf.Position = rot * (tf.Position - startPos) + startPos;
                tf.Rotation = rot * tf.Rotation;
                
                entry.transformData = tf;
                dataSo.SpawnDataEntries[index] = entry;
            }
        }


        private void DrawListHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Wave");
        }
        
        void DrawListItems(Rect rect, int index, bool isActive, bool isFocused)
        {        
            SerializedProperty element = _list.serializedProperty.GetArrayElementAtIndex(index); //The element in the list

            // Create a property field and label field for each property. 

            // The 'mobs' property. Since the enum is self-evident, I am not making a label field for it. 
            // The property field for mobs (width 100, height of a single line)
            EditorGUI.PropertyField(new Rect(rect.x + 10, rect.y, rect.width - 10, rect.height), element, true);

            if (isFocused || isActive)
                _selectedIndex = index;
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
