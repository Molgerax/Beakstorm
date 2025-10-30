//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	// This wraps EditorGUI, EditorGUILayout, GUI, and GUILayout calls.
	// The purpose is that you can call them in "normal" mode, which forwards the calls on to Unity,
	// or "keywords capture" mode, which performs no GUI work at all but captures a list of
	// keywords to expose to the Project Settings window's search.
	public static class TrembleSettingsGUI
	{
		private static readonly List<string> s_Keywords = new(256);
		private static bool s_IsCapturingKeywords = false;

		private static readonly Dictionary<string, bool> s_FoldoutStates = new();

		public static void BeginKeywordCapture()
		{
			s_IsCapturingKeywords = true;
			s_Keywords.Clear();
		}

		public static List<string> EndKeywordCapture()
		{
			s_IsCapturingKeywords = false;
			return s_Keywords;
		}

		public static void Space(float size)
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.Space(size);
		}

		public static void FlexibleSpace()
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.FlexibleSpace();
		}

		public static void LabelledHyperlink(string label, string buttonText, string url)
			=> LabelledHyperlink(label, buttonText, () => Application.OpenURL(url));

		public static void LabelledHyperlink(string label, string buttonText, Action action)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(label);
				s_Keywords.Add(buttonText);
				return;
			}

			BeginHorizontal();
			{
				Label(label + ":");
				if (Button(buttonText, GUILayout.Width(250f)))
				{
					action();
				}
			}
			EndHorizontal();
		}

		public static void BeginHorizontal(GUIStyle style, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.BeginHorizontal(style, options);
		}
		public static void BeginHorizontal(params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.BeginHorizontal(options);
		}

		public static void EndHorizontal()
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.EndHorizontal();
		}

		public static void BeginVertical(GUIStyle style, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.BeginVertical(style, options);
		}

		public static void BeginVertical(params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.BeginVertical(options);
		}

		public static void EndVertical()
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.EndVertical();
		}

		public static void BeginScrollView(ref Vector2 scrollPosition, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
				return;

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, options);
		}

		public static void EndScrollView()
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.EndScrollView();
		}

		public static void MakeLastRectClickable(Action onClick)
		{
			if (s_IsCapturingKeywords)
				return;

			Rect verticalRect = GUILayoutUtility.GetLastRect();
			if (Event.current.type == EventType.MouseDown && verticalRect.Contains(Event.current.mousePosition))
			{
				onClick();
				Event.current.Use(); // consume
			}
		}

		public static void HelpBox(string message, MessageType type)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(message);
				return;
			}

			EditorGUILayout.HelpBox(message, type);
		}

		public static void Separator(float height = 2f)
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.Box(Texture2D.grayTexture, GUILayout.ExpandWidth(true), GUILayout.Height(height));
		}

		public static void Label(string text, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(text);
				return;
			}

			GUILayout.Label(text, GUI.skin.label, options);
		}

		public static void Label(string text, GUIStyle style, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(text);
				return;
			}

			GUILayout.Label(text, style, options);
		}

		public static bool Button(string text, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(text);
				return false;
			}

			return GUILayout.Button(text, options);
		}

		public static bool Button(GUIContent content, GUIStyle style, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(content.text);
				return false;
			}

			return GUILayout.Button(content, style, options);
		}

		public static bool Toggle(bool value, GUIContent content)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(content.text);
				return value;
			}

			return GUILayout.Toggle(value, content);
		}

		public static bool Toggle(bool value, GUIContent content, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(content.text);
				return value;
			}

			return GUILayout.Toggle(value, content, options);
		}

		public static float FloatField(float value)
		{
			if (s_IsCapturingKeywords)
				return value;

			return EditorGUILayout.FloatField(value);
		}


		public static void PropertyField(SerializedProperty sp, params GUILayoutOption[] options)
		{
			if (s_IsCapturingKeywords)
				return;

			EditorGUILayout.PropertyField(sp, GUIContent.none, options);
		}

		public static void Image(Texture2D texture, float width, float height)
		{
			if (s_IsCapturingKeywords)
				return;

			GUILayout.Box(texture, GUILayout.Width(width), GUILayout.Height(height));
		}

		public static bool Foldout(bool foldout, string content, bool toggleOnLabelClick)
			=> Foldout(foldout, content, toggleOnLabelClick, EditorStyles.foldout);

		public static bool Foldout(bool foldout, string content, bool toggleOnLabelClick, GUIStyle style)
		{
			if (s_IsCapturingKeywords)
				return foldout;

			return EditorGUILayout.Foldout(foldout, content, toggleOnLabelClick, style);
		}

		public static void Foldout(string title, Action contents)
			=> Foldout(title, contents, EditorStyles.foldout);

		public static void Foldout(string title, Action contents, GUIStyle style)
		{
			if (s_IsCapturingKeywords)
				return;

			bool foldout = s_FoldoutStates.GetValueOrDefault(title, false);
			foldout = Foldout(foldout, title, true, style);

			if (foldout)
			{
				Space(10f);

				BeginHorizontal();
				{
					Space(20f);

					BeginVertical();
					{
						contents();
					}
					EndVertical();
				}
				EndHorizontal();

				Space(10f);
			}

			s_FoldoutStates[title] = foldout;
		}

		public static void PrefixLabel(string text, string tooltip)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.Add(text);
				return;
			}

			EditorGUILayout.PrefixLabel(new GUIContent(text, tooltip));
		}

		public static int Popup(int value, string[] options)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.AddRange(options);
				return value;
			}

			return EditorGUILayout.Popup(value, options);
		}
		public static int Popup(int value, string[] options, GUIStyle style)
		{
			if (s_IsCapturingKeywords)
			{
				s_Keywords.AddRange(options);
				return value;
			}

			return EditorGUILayout.Popup(value, options, style);
		}

		public static void UseDisabled(bool disabled, Action inside)
		{
			bool prevDisabled = false;

			if (!s_IsCapturingKeywords)
			{
				prevDisabled = !GUI.enabled;
				GUI.enabled = !disabled;
			}

			inside();

			if (!s_IsCapturingKeywords)
			{
				GUI.enabled = !prevDisabled;
			}
		}

		public static void UseColour(Color c, Action inside)
		{
			Color previousColour = Color.white;

			if (!s_IsCapturingKeywords)
			{
				previousColour = GUI.color;
				GUI.color = c;
			}

			inside();

			if (!s_IsCapturingKeywords)
			{
				GUI.color = previousColour;
			}
		}

		public static void Indent(Action inside)
		{
			if (!s_IsCapturingKeywords)
			{
				EditorGUI.indentLevel++;
			}

			inside();

			if (!s_IsCapturingKeywords)
			{
				EditorGUI.indentLevel--;
			}
		}
	}
}