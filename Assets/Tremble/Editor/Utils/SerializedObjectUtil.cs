// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGoose.Tremble.Editor
{
	internal static class SerializedObjectUtil
	{
		internal static void Modify(this Component component, Action<SerializedObject> actions) => Modify(new SerializedObject(component), actions);

		internal static void Modify(this Object obj, Action<SerializedObject> actions) => Modify(new SerializedObject(obj), actions);


		private static void Modify(SerializedObject so, Action<SerializedObject> actions)
		{
			actions?.Invoke(so);
			so.ApplyModifiedPropertiesWithoutUndo();
		}

		internal static SerializedProperty FindBackedProperty(this SerializedObject obj, string name) => obj.FindProperty("m_" + name);
		internal static SerializedProperty FindBackedRelativeProperty(this SerializedProperty obj, string name) => obj.FindPropertyRelative("m_" + name);

		internal static SerializedProperty AppendArrayElement(this SerializedProperty prop)
		{
			int elemIdx = prop.arraySize;
			prop.InsertArrayElementAtIndex(elemIdx);
			return prop.GetArrayElementAtIndex(elemIdx);
		}

        internal static uint GetHashOfContent(this SerializedProperty prop)
        {
#if UNITY_2022_1_OR_NEWER
            if (prop.propertyType == SerializedPropertyType.Generic && !prop.isArray)
                return (uint)prop.boxedValue.GetHashCode();

            return prop.contentHash;
#else
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return (uint)prop.intValue.GetHashCode();
                case SerializedPropertyType.Boolean:
                    return (uint)prop.boolValue.GetHashCode();
                case SerializedPropertyType.Float:
                    return (uint)prop.floatValue.GetHashCode();
                case SerializedPropertyType.String:
                    return (uint)prop.stringValue.GetHashCode();
                case SerializedPropertyType.Color:
                    return (uint)prop.colorValue.GetHashCode();
                case SerializedPropertyType.ObjectReference:
                    return (uint)(prop.objectReferenceValue ? prop.objectReferenceValue.GetHashCode() : 0);
                case SerializedPropertyType.LayerMask:
                    return (uint)prop.intValue.GetHashCode();
                case SerializedPropertyType.Enum:
                    return (uint)prop.enumValueIndex.GetHashCode();
                case SerializedPropertyType.Vector2:
                    return (uint)prop.vector2Value.GetHashCode();
                case SerializedPropertyType.Vector3:
                    return (uint)prop.vector3Value.GetHashCode();
                case SerializedPropertyType.Vector4:
                    return (uint)prop.vector4Value.GetHashCode();
                case SerializedPropertyType.Rect:
                    return (uint)prop.rectValue.GetHashCode();
                case SerializedPropertyType.Character:
                    return (uint)prop.intValue.GetHashCode();
                case SerializedPropertyType.AnimationCurve:
                    return (uint)prop.animationCurveValue.GetHashCode();
                case SerializedPropertyType.Bounds:
                    return (uint)prop.boundsValue.GetHashCode();

                case SerializedPropertyType.Generic:
                    return (uint)(prop.isArray ? prop.arraySize : 0);

                default:
                    Debug.LogWarning("Unsupported property type: " + prop.propertyType + $" '{prop.name}'");
                    return 0u;
            }
#endif
        }

        internal static void SetTo(this SerializedProperty prop, SerializedProperty other)
        {
            switch (other.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = other.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    prop.boolValue = other.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    prop.floatValue = other.floatValue;
                    break;
                case SerializedPropertyType.String:
                    prop.stringValue = other.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    prop.colorValue = other.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    prop.objectReferenceValue = other.objectReferenceValue;
                    break;
                case SerializedPropertyType.LayerMask:
                    prop.intValue = other.intValue;
                    break;
                case SerializedPropertyType.Enum:
                    prop.enumValueIndex = other.enumValueIndex;
                    break;
                case SerializedPropertyType.Vector2:
                    prop.vector2Value = other.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    prop.vector3Value = other.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    prop.vector4Value = other.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    prop.rectValue = other.rectValue;
                    break;
                case SerializedPropertyType.Character:
                    prop.intValue = other.intValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    prop.animationCurveValue = other.animationCurveValue;
                    break;
                case SerializedPropertyType.Bounds:
                    prop.boundsValue = other.boundsValue;
                    break;
                case SerializedPropertyType.Generic:
                    if (prop.isArray)
                    {
                        prop.ClearArray();
                        for (int i = 0; i < other.arraySize; i++)
                        {
                            prop.AppendArrayElement().SetTo(prop.GetArrayElementAtIndex(i));
                        }
                    }
                    else
                    {
#if UNITY_2022_1_OR_NEWER
                        prop.boxedValue = other.boxedValue;
#endif
                    }

                    break;

#if UNITY_2022_1_OR_NEWER
                case SerializedPropertyType.Gradient:
                    prop.gradientValue = other.gradientValue;
                    break;
#endif

                default:
                    Debug.LogWarning("Unsupported property type: " + prop.propertyType + $" '{prop.name}'");
                    break;
            }
        }

        internal static bool EqualByValue(this SerializedProperty prop, SerializedProperty other)
            => prop.GetHashOfContent() == other.GetHashOfContent();
    }
}