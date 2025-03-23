using System;
using DynaMak.Particles;
using UnityEditor;
using UnityEngine;

namespace DynaMak.Properties.Editor
{
    [CustomEditor(typeof(DynaPropertyBinderBase<>), true)]
    public class DynaPropertyBinderBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            OnInspectorGUI( (dynamic) target);
        }

        private bool _foldoutMoreProperties;
        
        private void OnInspectorGUI<T>(DynaPropertyBinderBase<T> t)
        {
            var dynaPropertyBinder = t;
            if(dynaPropertyBinder is null) return;
            
            SerializedProperty value = serializedObject.FindProperty("Value");
            SerializedProperty propertyName = serializedObject.FindProperty("PropertyName");

            GUIContent valueLabel = new GUIContent("Value");
            GUIContent propertyNameLabel = new GUIContent("Name");

            bool hasMoreProperties = value.NextVisible(false);
            value.Reset();
            value = serializedObject.FindProperty("Value");
            
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 40;
            Rect rect = EditorGUILayout.BeginHorizontal();

            
            if (hasMoreProperties)
            {
                Rect foldOutRect = rect;
                foldOutRect.width = 2;
                //_foldoutMoreProperties = EditorGUI.Foldout(foldOutRect, _foldoutMoreProperties, new GUIContent(String.Empty, "Additional Settings"));
                //EditorGUILayout.Space(foldOutRect.height, false);

                _foldoutMoreProperties = true;
            }
            else _foldoutMoreProperties = false;
            
            EditorGUILayout.PropertyField(propertyName, propertyNameLabel, true, GUILayout.MaxWidth(150));
            
            EditorGUILayout.Space(20, false);

            
            EditorGUILayout.PropertyField(value, valueLabel, true);
            EditorGUILayout.EndHorizontal();
            
            EditorGUIUtility.labelWidth = labelWidth;
            serializedObject.ApplyModifiedProperties();

            SerializedProperty iterator = value;
            bool visitChildren = true;
            //iterator.NextVisible(visitChildren);
            visitChildren = false;

            if (_foldoutMoreProperties)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                while (iterator.NextVisible(visitChildren))
                {
                    GUIContent iteratorLabel = new GUIContent(iterator.displayName, iterator.tooltip);
                    EditorGUILayout.PropertyField(iterator, iteratorLabel, true);
                }
                
                EditorGUILayout.EndVertical();
                
                EditorGUI.indentLevel--;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            
            // SUBSCRIPTION

            SerializedProperty subscribeToParticles = serializedObject.FindProperty("subscribeToParticles");
            SerializedProperty particlesToSubScribe = serializedObject.FindProperty("particlesToSubscribe");

            GUIContent subscribeLabel = new GUIContent("Subscribe");
            GUIContent particlesLabel = new GUIContent("DynaParticles");


            bool currentSubscriptionValue = subscribeToParticles.boolValue;
            
            
            
            // Disable subscription, when there already is a particle system present
            bool hasDynaPropertyUserOnGameObject =
                dynaPropertyBinder.TryGetComponent(out IDynaPropertyUser propertyUser);


            if (hasDynaPropertyUserOnGameObject)
            {
                subscribeToParticles.boolValue = false;
                return;
            }
            
            
            EditorGUILayout.Separator();
            
            EditorGUIUtility.labelWidth =70;
            
            EditorGUILayout.BeginHorizontal();
            
            //EditorGUILayout.PropertyField(subscribeToParticles, subscribeLabel, false, GUILayout.MaxWidth(150));

            subscribeToParticles.boolValue = EditorGUILayout.Toggle(subscribeLabel, subscribeToParticles.boolValue, GUILayout.MaxWidth(110));

            if (currentSubscriptionValue != subscribeToParticles.boolValue)
            {
                if (Application.isPlaying && Application.isEditor)
                {
                    if (subscribeToParticles.boolValue)
                        dynaPropertyBinder.SubscribeToParticle();
                    
                    else 
                        dynaPropertyBinder.UnsubscribeFromParticle();
                }
            }
            
            EditorGUILayout.Space(10, false);

            
            EditorGUIUtility.labelWidth =90;
            
            EditorGUI.BeginDisabledGroup(!subscribeToParticles.boolValue);
            
            EditorGUILayout.PropertyField(particlesToSubScribe, particlesLabel, true);
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndHorizontal();
            

            EditorGUIUtility.labelWidth = labelWidth;

            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.Selected)]
        static void DrawGizmos(DynaPropertyBinderBase binder, GizmoType gizmoType)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

            if (binder.SubscribeToParticles && binder.ParticlesToSubscribe)
            {
                Gizmos.DrawLine(binder.transform.position, binder.ParticlesToSubscribe.transform.position);
            }


            if (binder.ValueAsComponent)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
                Gizmos.DrawLine(binder.transform.position, binder.ValueAsComponent.transform.position);
                Gizmos.DrawWireSphere(binder.ValueAsComponent.transform.position, 0.5f);
            }
        }
    }
}
