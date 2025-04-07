using System.Collections.Generic;
using DynaMak.Properties;
using UnityEditor;
using UnityEngine;
using DynaMak.Editors.Utility;


namespace DynaMak.Particles.Editor
{
    [CustomEditor(typeof(DynaParticleComponent))]
    public class DynaParticleComponentEditor : UnityEditor.Editor
    {
        #region GUI
        
        private SerializedProperty particleStructFile;
        private SerializedProperty structLengthBytes;
        private SerializedProperty dynaProperties;

        private void GetSerializedProperties()
        {
            structLengthBytes = serializedObject.FindProperty("structLengthBytes");
            particleStructFile = serializedObject.FindProperty("particleStructFile");
            dynaProperties = serializedObject.FindProperty("dynaProperties");
        }
        
        
        private void OnEnable()
        {
            GetSerializedProperties();
        }

        private void OnValidate()
        {
            GetSerializedProperties();
            DynaParticleComponent particleComponent = target as DynaParticleComponent;
            ReadStructLength(particleComponent!.ComputeShader);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DynaParticleComponent particleComponent = target as DynaParticleComponent;
            if(particleComponent is null) return;

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Generate Properties from Particle File"))
            {
                Undo.RecordObject(particleComponent, "Add new binders from file.");
                AddPropertyComponentsFromFile(particleComponent.ComputeShader, particleComponent.gameObject);
            }
            
            if (GUILayout.Button("Remove Properties", GUILayout.MaxWidth(150f), GUILayout.MinWidth(150f)))
            {
                Undo.RecordObject(particleComponent.gameObject, "Remove binders from gameObject.");

                List<DynaPropertyBinderBase> properties = new List<DynaPropertyBinderBase>();
                particleComponent.GetComponents(properties);
                
                for (int i = properties.Count - 1; i >= 0; i--) 
                { 
                    Undo.DestroyObjectImmediate(properties[i]);
                }
                RefreshDynaProperties(particleComponent.gameObject);
            }
            
            GUILayout.EndHorizontal();

            if (true)
            {
                ReadStructLength(particleComponent.ComputeShader);
            }
            
            if (particleStructFile.objectReferenceValue != null)
            {
                GUIContent structLabel = new GUIContent("Particle Struct Length:",
                    "Length of the Particle Data Struct in Bytes.");

                GUIContent bytesLabel = new GUIContent(structLengthBytes.intValue.ToString() + "B",
                    structLengthBytes.intValue.ToString() + " Bytes");
                EditorGUILayout.LabelField(structLabel,  bytesLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No Particle Data struct definition found!");
            }
        }

        #endregion

        #region Compute Shader File Parsing

        /// <summary>
        /// Reads out the struct length from the Particle Define file.
        /// </summary>
        private void ReadStructLength(ComputeShader computeShader)
        {
            if(computeShader == null) return;
            
            EditorGUI.BeginChangeCheck();
            
            particleStructFile.objectReferenceValue = DynaParticleFileReader.ComputeShaderToParticleStruct(computeShader);
            structLengthBytes.intValue = DynaParticleFileReader.GetStructLengthFromFile(computeShader);

            EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Reads out the properties declared in the Compute Shader file, and adds them to the game object.
        /// </summary>
        /// <param name="computeShader">Compute Shader to parse</param>
        /// <param name="gameObject">Game Object to add to</param>
        private void AddPropertyComponentsFromFile(ComputeShader computeShader, GameObject gameObject)
        {
            DynaPropertyBinderBase[] properties = DynaParticleFileReader.AddPropertyBindersFromShaderFile(computeShader, gameObject);
            
            EditorGUI.BeginChangeCheck();
            
            dynaProperties.ClearArray();
            dynaProperties.arraySize = properties.Length;
            
            for (int i = 0; i < dynaProperties.arraySize; i++)
            {
                var property = dynaProperties.GetArrayElementAtIndex(i);
                property.objectReferenceValue = properties[i];
            }
            
            EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// Get present properties from the gameObject. Used in the Editor class
        /// </summary>
        /// <returns></returns>
        private int RefreshDynaProperties(GameObject gameObject)
        {
            DynaPropertyBinderBase[] properties = gameObject.GetComponents<DynaPropertyBinderBase>();
            
            EditorGUI.BeginChangeCheck();
            
            dynaProperties.ClearArray();
            dynaProperties.arraySize = properties.Length;
            
            for (int i = 0; i < dynaProperties.arraySize; i++)
            {
                var property = dynaProperties.GetArrayElementAtIndex(i);
                property.objectReferenceValue = properties[i];
            }
            
            EditorGUI.EndChangeCheck();
            serializedObject.ApplyModifiedProperties();
            
            return dynaProperties.arraySize;
        }

        #endregion
        
        #region Gizmos

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void DrawGizmosNotSelected(DynaParticleComponent particleComponent, GizmoType gizmoType)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
            Gizmos.DrawSphere(particleComponent.transform.position, 1f);
        }
        

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(DynaParticleComponent particleComponent, GizmoType gizmoType)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(particleComponent.transform.position, 0.25f);
            
            
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

            if (particleComponent.SubscribedProperties is not null)
            {
                foreach (var propertyBinder in particleComponent.SubscribedProperties)
                {
                    if(propertyBinder is null) continue;
                    Gizmos.DrawLine(particleComponent.transform.position, propertyBinder.transform.position);
                }
            }
        }
        #endregion
    }
}