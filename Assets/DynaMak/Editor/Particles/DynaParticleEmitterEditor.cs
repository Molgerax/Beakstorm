using System.Collections.Generic;
using DynaMak.Editors.Utility;
using DynaMak.Properties;
using UnityEditor;
using UnityEngine;


namespace DynaMak.Particles.Editor
{
    [CustomEditor(typeof(DynaParticleEmitter))]
    public class DynaParticleEmitterEditor : UnityEditor.Editor
    {
        #region GUI

        private SerializedProperty particleComponent;
        private SerializedProperty dynaProperties;
        
        private void OnEnable()
        {
            particleComponent = serializedObject.FindProperty("particleComponent");
            dynaProperties = serializedObject.FindProperty("dynaProperties");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DynaParticleEmitter particleEmitter = target as DynaParticleEmitter;
            if(particleEmitter is null) return;

            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Generate Properties from Particle File"))
            {
                Undo.RecordObject(particleEmitter, "Add new binders from file.");
                AddPropertyComponentsFromFile(particleEmitter.gameObject);
            }
            
            if (GUILayout.Button("Remove Properties", GUILayout.MaxWidth(150f), GUILayout.MinWidth(150f)))
            {
                Undo.RecordObject(particleEmitter.gameObject, "Remove binders from gameObject.");

                List<DynaPropertyBinderBase> properties = new List<DynaPropertyBinderBase>();
                particleEmitter.GetComponents(properties);
                
                for (int i = properties.Count - 1; i >= 0; i--) 
                { 
                    Undo.DestroyObjectImmediate(properties[i]);
                }
                RefreshDynaProperties(particleEmitter.gameObject);
            }
            
            GUILayout.EndHorizontal();
        }

        #endregion

        
        #region Compute Shader File Parsing

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

        /// <summary>
        /// Reads out the property declared in the Particle Define file.
        /// </summary>
        private void AddPropertyComponentsFromFile(GameObject gameObject)
        {
            DynaParticleComponent pc = particleComponent.objectReferenceValue as DynaParticleComponent;
            if(pc is null) return;
            
            if(pc.ParticleStructFile == null) return;


            DynaPropertyBinderBase[] properties = DynaParticleFileReader.AddPropertyBindersFromShaderFile(pc.ComputeShader, gameObject, true);
            
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

        

        #endregion
        

        #region Gizmos

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
        static void DrawGizmosNotSelected(DynaParticleEmitter emitter, GizmoType gizmoType)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.25f);
            Gizmos.DrawSphere(emitter.transform.position, 1f);
        }
        

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void DrawGizmosSelected(DynaParticleEmitter emitter, GizmoType gizmoType)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 1f);
            Gizmos.DrawSphere(emitter.transform.position, 0.25f);

            if (emitter.ParticleComponent)
            {
                Gizmos.DrawLine(emitter.transform.position, emitter.ParticleComponent.transform.position);
                Gizmos.DrawWireSphere(emitter.ParticleComponent.transform.position, 1f);
            }
        }
        #endregion
    }
}