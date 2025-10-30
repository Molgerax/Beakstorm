//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public abstract class BaseClassPickerDrawer<TClassType> : PropertyDrawer
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Consts
		// -----------------------------------------------------------------------------------------------------------------------------
		private const string INDENT_STRING = "     ";
		private const string NONE_STRING = "(none)";

		protected abstract string ClassNamePropertyName { get; }

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private List<Type> m_AllClasses;
		private int m_SelectedClass;

		private bool m_Inited;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Lazy-load class catalog if needed
			if (!m_Inited)
			{
				m_Inited = true;
				GenerateClassCatalog();
			}

			// Draw label & icon
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
			EditorGUI.indentLevel = 0;

			// Generate class list
			string[] classDisplayNames = m_AllClasses.Select(t => GetHumanClassName(t)).ToArray();

			// Calculate current index
			SerializedProperty classProperty = property.FindPropertyRelative(ClassNamePropertyName);
			int prevSelectedClass = m_AllClasses.FindIndex(c => c.FullName == classProperty.stringValue);
			if (prevSelectedClass == -1)
			{
				prevSelectedClass = 0;
			}

			m_SelectedClass = EditorGUI.Popup(position, "", prevSelectedClass, classDisplayNames);

			EditorGUI.BeginProperty(position, GUIContent.none, property);
			classProperty.stringValue = m_AllClasses[m_SelectedClass].FullName;
			EditorGUI.EndProperty();

			// Draw icon overlay
			Rect iconRect = position;
			iconRect.width = position.height;
			iconRect.x += 2f;
			iconRect.y += 2f;
			iconRect.width -= 4f;
			iconRect.height -= 4f;

			position.x += position.height;
			position.width -= position.height;

			GUI.DrawTexture(iconRect, EditorGUIUtility.FindTexture("cs Script Icon"));
		}

		private void GenerateClassCatalog()
		{
			List<Type> allClasses = TypeCache.GetTypesDerivedFrom(typeof(TClassType)).ToList();
			m_AllClasses = new(allClasses.Count + 1);

			if (!typeof(TClassType).IsAbstract)
			{
				m_AllClasses.Add(typeof(TClassType));
			}
			m_AllClasses.AddRange(allClasses);
		}
		
		private static string GetHumanClassName(Type type, bool indent = true)
		{
			string fullClassName = type.FullName;
			if (fullClassName == null)
				return indent ? (INDENT_STRING + NONE_STRING) : NONE_STRING;
			
			int lastDot = fullClassName.LastIndexOf('.');

			// If it ends in dot (?!) or has no dot - return class as-is
			if (lastDot == -1 || lastDot == fullClassName.Length - 1)
				return fullClassName;

			string shortClassName = fullClassName.Substring(lastDot + 1);

			if (shortClassName.StartsWithInvariant("Null", caseSensitive: true))
			{
				return indent ? (INDENT_STRING + NONE_STRING) : NONE_STRING;
			}

			bool doneFirst = false;
			List<char> output = new(shortClassName.Length + 10);

			foreach (char c in shortClassName)
			{
				if (Char.IsUpper(c) && doneFirst)
				{
					output.Add(' ');
				}

				output.Add(c);
				doneFirst = true;
			}

			string humanName = new(output.ToArray());
			return indent ? (INDENT_STRING + humanName) : humanName;
		}
	}
}