using System;
using UnityEditor;
using UnityEngine;

namespace Beakstorm
{
    public static class EditorList
    {
        public static void Show(SerializedProperty list, EditorListOption options = EditorListOption.Default)
        {
            bool showSize = (options & EditorListOption.ListSize) != 0;
            bool showLabel = (options & EditorListOption.ListLabel) != 0;

            if (showLabel)
            {
                EditorGUILayout.PropertyField(list);
                EditorGUI.indentLevel += 1;
            }
            if (list.isExpanded)
            {
                if (showSize)
                {
                    EditorGUILayout.PropertyField(list.FindPropertyRelative("Array.size"));
                }
                ShowElements(list, options);
            }

            if (showLabel)
            {
                EditorGUI.indentLevel -= 1;
            }
        }

        private static void ShowElements(SerializedProperty list, EditorListOption options)
        {
            bool showElementLabels = (options & EditorListOption.ElementLabels) != 0;
            
            for (int i = 0; i < list.arraySize; i++)
            {
                int indentLevel = EditorGUI.indentLevel;
                
                if (showElementLabels)
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i));
                else
                    EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), GUIContent.none);

                EditorGUI.indentLevel = indentLevel;
            }
        }
        
        [Flags]
        public enum EditorListOption {
            None = 0,
            ListSize = 1,
            ListLabel = 2,
            ElementLabels = 4,
            Default = ListSize | ListLabel | ElementLabels,
            NoElementLabels = ListSize | ListLabel
        }
    }
}
