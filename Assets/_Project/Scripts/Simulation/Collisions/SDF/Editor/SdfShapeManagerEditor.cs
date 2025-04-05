using UnityEditor;

namespace Beakstorm.Simulation.Collisions.SDF.Editor
{
    [CustomEditor(typeof(SdfShapeManager))]
    public class SdfShapeManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SdfShapeManager manager = (SdfShapeManager) target;

            EditorGUILayout.LabelField($"NodeCount: {manager.NodeCount}");
            EditorGUILayout.LabelField($"NodeList Length: {manager.NodeList?.Length}");
            EditorGUILayout.LabelField($"BufferSize: {manager.BufferSize}");
        }
    }
}
