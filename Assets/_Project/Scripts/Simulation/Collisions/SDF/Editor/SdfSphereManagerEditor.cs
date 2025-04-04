using Beakstorm.Simulation.Collisions.SDF;
using UnityEditor;

namespace Beakstorm._Project.Scripts.Simulation.Collisions.SDF.Editor
{
    [CustomEditor(typeof(SdfSphereManager))]
    public class SdfSphereManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SdfSphereManager manager = (SdfSphereManager) target;

            EditorGUILayout.LabelField($"NodeCount: {manager.NodeCount}");
            EditorGUILayout.LabelField($"NodeList Length: {manager.NodeList?.Length}");
            EditorGUILayout.LabelField($"BufferSize: {manager.BufferSize}");
        }
    }
}
