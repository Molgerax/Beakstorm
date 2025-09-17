using UnityEditor;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes.Editor
{
    [CustomEditor(typeof(SdfTextureField))]
    public class SdfTextureFieldEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            SdfTextureField field = target as SdfTextureField;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SDF Texture");

            Object sdf = field.SdfTexture;

            GUIContent button = new GUIContent("null");
            if (sdf)
                button.text = "Open Properties...";
            
            EditorGUI.BeginDisabledGroup(!sdf);
            if (GUILayout.Button(button))
            {
                EditorUtility.OpenPropertyEditor(field.SdfTexture);
            }
            EditorGUI.EndDisabledGroup();
            
            //EditorGUILayout.ObjectField(field.SdfTexture, typeof(RenderTexture), false);
            EditorGUILayout.EndHorizontal();
        }
    }
}