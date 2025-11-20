//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	internal static class EditorGUILayoutUtil
	{
		internal static void Pad(float pad, Action content) => Pad(pad, pad, content);
		internal static void Pad(float hPad, float vPad, Action content)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(hPad);
				GUILayout.BeginVertical();
				{
					GUILayout.Space(vPad);

					try
					{
						content();
					}
					catch (ExitGUIException)
					{
						// ignore
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}

					GUILayout.Space(vPad);
				}
				GUILayout.EndVertical();
				GUILayout.Space(hPad);
			}
			GUILayout.EndHorizontal();
		}

		private static readonly Dictionary<string, bool> s_CollapsibleHeaderStates = new();

		internal static void Indent(Action inside)
		{
			EditorGUI.indentLevel++;
			inside();
			EditorGUI.indentLevel--;
		}


		internal static bool FoldoutArrow(bool isOpen)
		{
			bool toggle = GUILayout.Button(GUIContent.none, EditorStyles.label, GUILayout.Width(20f));

			Rect buttonRect = GUILayoutUtility.GetLastRect();
			buttonRect.x += 5f;

			if (Event.current.type == EventType.Repaint)
			{
				EditorStyles.foldout.Draw(buttonRect, false, false, isOpen, false);
			}

			return toggle;
		}

		internal static bool CollapsibleHeader(string header, bool isOpen, Action inside)
		{
			GUILayout.BeginHorizontal();
			{
				bool toggle = FoldoutArrow(isOpen);
				toggle |= GUILayout.Button(header, EditorStyles.label);

				if (toggle)
				{
					isOpen = !isOpen;
				}
			}
			GUILayout.EndHorizontal();

			if (isOpen)
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Space(20f);
					GUILayout.BeginVertical();
					{
						inside();
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();
			}

			return isOpen;
		}

		internal static void CollapsibleHeader(string header, string id, bool defaultOpen, Action inside)
		{
			bool wasOpen = s_CollapsibleHeaderStates.GetValueOrDefault(id, defaultOpen);
			s_CollapsibleHeaderStates[id] = CollapsibleHeader(header, wasOpen, inside);
		}

		internal static void CollapsibleHeader(string header, string id, Action inside)
			=> CollapsibleHeader(header, id, defaultOpen: false, inside);

		internal static void SetHeaderOpen(string id, bool isOpened)
		{
			s_CollapsibleHeaderStates[id] = isOpened;
		}
		internal static bool IsHeaderOpen(string id) => s_CollapsibleHeaderStates.GetValueOrDefault(id, false);
	}
}