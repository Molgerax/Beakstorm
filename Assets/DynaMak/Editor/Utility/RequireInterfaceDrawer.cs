using System;
using UnityEditor;
using UnityEngine;
using DynaMak.Utility;
using Object = UnityEngine.Object;


namespace DynaMak.Editors.Utility
{
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class RequireInterfaceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                var requiredAttribute = this.attribute as RequireInterfaceAttribute;

                
                EditorGUI.BeginProperty(position, label, property);

                Type type = requiredAttribute.requiredType;

                Object reference = EditorGUI.ObjectField(position, label, property.objectReferenceValue, 
                    type, true);

                if (DragAndDrop.objectReferences.Length > 0)
                {
                    if (DragAndDrop.objectReferences[0] is GameObject g)
                    {
                        if (g.TryGetComponent(type, out Component c))
                        {
                            Object obj = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(GameObject), true);
                            if (obj is GameObject go)
                            {
                                reference = go.GetComponent(requiredAttribute.requiredType);
                            }
                        }
                    }
                }

                property.objectReferenceValue = reference;
                EditorGUI.EndProperty();
            }
            else
            {
                // If field is not reference, show error message.
                // Save previous color and change GUI to red.
                var previousColor = GUI.color;
                GUI.color = Color.red;
                // Display label with error message.
                EditorGUI.LabelField(position, label, new GUIContent("Property is not a reference type"));
                // Revert color change.
                GUI.color = previousColor;
            }
        }
    }
}