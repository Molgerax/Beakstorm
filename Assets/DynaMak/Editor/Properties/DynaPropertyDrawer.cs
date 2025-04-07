using UnityEditor;
using UnityEngine;

namespace DynaMak.Properties.Editor
{
    [CustomPropertyDrawer(typeof(DynaProperty), true)]
    public class DynaPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty propertyName = property.FindPropertyRelative("PropertyName");
            SerializedProperty value = property.FindPropertyRelative("Value");

            GUIContent labelName = new GUIContent("Name", propertyName.tooltip);
            GUIContent labelValue = new GUIContent(value.displayName, value.tooltip);


            position.height = GetPropertyHeight(property, label);
            Rect ogPosition = position;
            Rect propPosition = position;
            Rect valPosition = position;

            propPosition.width = Mathf.Min(150f, ogPosition.width * 0.5f);
            valPosition.width = ogPosition.width - propPosition.width;
            valPosition.x += propPosition.width;

            position = propPosition;
            EditorGUI.BeginProperty(propPosition, labelName, propertyName);
            position.width = 35;
            EditorGUI.PrefixLabel(position, labelName);
            position.x += position.width + 5;
            position.width = propPosition.width - (position.x - propPosition.x + 10);
            EditorGUI.PropertyField(position, propertyName, GUIContent.none);
            EditorGUI.EndProperty();
            position.x += position.width + 10;

            
            position = valPosition;
            EditorGUI.BeginProperty(valPosition, labelValue, value);
            position.width = 50;
            EditorGUI.PrefixLabel(position, labelValue);
            position.x += position.width + 5;
            position.width = valPosition.width - (position.x - valPosition.x + 5);
            EditorGUI.PropertyField(position, value, GUIContent.none);
            EditorGUI.EndProperty();
            
            
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty propertyName = property.FindPropertyRelative("PropertyName");
            SerializedProperty value = property.FindPropertyRelative("Value");
            
            
            float totalHeight = base.GetPropertyHeight(value, label);
            if (value.isExpanded)
            {
                foreach (SerializedProperty p in property)
                {
                    totalHeight += base.GetPropertyHeight(p, new GUIContent(p.name, p.tooltip));
                }    
            }
            return totalHeight;
        }
    }
}
