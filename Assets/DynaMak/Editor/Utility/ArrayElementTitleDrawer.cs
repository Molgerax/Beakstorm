// Source from this thread:
// https://forum.unity.com/threads/how-to-change-the-name-of-list-elements-in-the-inspector.448910/

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace DynaMak.Utility
{
    [CustomPropertyDrawer(typeof(ArrayElementTitleAttribute))]
    public class ArrayElementTitleDrawer : PropertyDrawer
    {
        SerializedProperty _titleNameProp;
 
        protected virtual ArrayElementTitleAttribute Attribute => (ArrayElementTitleAttribute)attribute;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
          return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        { 
#if UNITY_2022_1_OR_NEWER
          if (property.boxedValue is IArrayElementTitle titled) 
          { 
              label = new GUIContent(label) { text = titled.Name };
          }
#else
          if (property.objectReferenceValue is IArrayElementTitle titled)
          {
            label = new GUIContent(label) { text = titled.Name };
          }
#endif
          else
          { 
              string fullPathName = property.propertyPath + "." + Attribute.VarName;
              SerializedProperty nameProp = property.serializedObject.FindProperty(fullPathName);
              if (nameProp != null)
                label = new GUIContent(label) { text = GetTitle(nameProp) };
              else
              {
                //Debug.LogWarning($"Could not get name for property path {fullPathName}, did you define a path or inherit from IArrayElementTitle?");
              }
          }
        
          EditorGUI.PropertyField(position, property, label, true);
        }
        
        string GetTitle(SerializedProperty prop)
        {
          switch (prop.propertyType)
          {
            case SerializedPropertyType.Generic:
              break;
            case SerializedPropertyType.Integer:
              return prop.intValue.ToString();
            case SerializedPropertyType.Boolean:
              return prop.boolValue.ToString();
            case SerializedPropertyType.Float:
              return prop.floatValue.ToString("G");
            case SerializedPropertyType.String:
              return prop.stringValue;
            case SerializedPropertyType.Color:
              return prop.colorValue.ToString();
            case SerializedPropertyType.ObjectReference:
              return prop.objectReferenceValue.ToString();
            case SerializedPropertyType.LayerMask:
              break;
            case SerializedPropertyType.Enum:
              return prop.enumNames[prop.enumValueIndex];
            case SerializedPropertyType.Vector2:
              return prop.vector2Value.ToString();
            case SerializedPropertyType.Vector3:
              return prop.vector3Value.ToString();
            case SerializedPropertyType.Vector4:
              return prop.vector4Value.ToString();
          }
        
          return "";
        }
  }
}