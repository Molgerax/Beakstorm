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
	public class Q3Map2OutputWindow : EditorWindow
	{
		private IList<string> m_Lines;
		private Vector2 m_Scroll;

		private bool m_ShowErrors = true;
		private bool m_ShowWarnings = true;
		private bool m_ShowInfo = false;

		private string m_Filter = "";
		private bool m_HasErrors;
		private bool m_HasWarnings;
		
		public static void Open(IList<string> output, bool showAllInfo = false)
		{
			Q3Map2OutputWindow window = GetWindow<Q3Map2OutputWindow>(true, "Map Conversion Output", true);
			Vector2 size = new(700f, 350f);
			window.minSize = size;
			window.position = new(EditorGUIUtility.GetMainWindowPosition().center - size / 2f, size);
			window.m_Lines = output;

			if (showAllInfo)
			{
				window.m_ShowInfo = true;
			}

			window.Show();
		}

		private bool IsWarning(string line) => line.ContainsInvariant("warning") && !line.ContainsInvariant("__TB_");
		private bool IsError(string line) => line.ContainsInvariant("error") && !line.ContainsInvariant("__TB_");

		private void OnGUI()
		{
			if (m_Lines == null)
			{
				Close();
				return;
			}

			if (m_HasErrors)
			{
				GUI.color = Color.red;
				GUILayout.Label("Q3Map2 produced error(s)!", EditorStyles.largeLabel);
				GUI.color = Color.white;
			}
			else if (m_HasWarnings)
			{
				GUI.color = Color.yellow;
				GUILayout.Label("Q3Map2 produced warning(s)!", EditorStyles.largeLabel);
				GUI.color = Color.white;
			}
			else
			{
				GUILayout.Label("Q3Map2 Output", EditorStyles.largeLabel);

			}

			GUILayout.Space(10f);
			GUILayout.Box(" ", GUILayout.ExpandWidth(true), GUILayout.Height(2f));

			using (EditorGUILayout.ScrollViewScope scroll = new(m_Scroll))
			{
				foreach (string line in m_Lines)
				{
					if (m_Filter.Length > 0 && !line.ContainsInvariant(m_Filter))
						continue;

					if (IsError(line))
					{
						if (!m_ShowErrors)
							continue;

						GUI.color = Color.red;
						m_HasErrors = true;
					}
					else if (IsWarning(line))
					{
						if (!m_ShowWarnings)
							continue;

						GUI.color = Color.yellow;
						m_HasWarnings = true;
					}
					else
					{
						if (!m_ShowInfo)
							continue;

						GUI.color = new Color(0.8f, 0.8f, 0.8f);
					}

					GUILayout.Label("   " + line, EditorStyles.wordWrappedLabel);
				}

				m_Scroll = scroll.scrollPosition;
			}

			GUI.color = Color.white;

			GUILayout.Box(" ", GUILayout.ExpandWidth(true), GUILayout.Height(2f));

			using (EditorGUILayout.HorizontalScope _ = new())
			{
				GUILayout.Label("Show:");

				GUI.color = Color.red;
				m_ShowErrors = GUILayout.Toggle(m_ShowErrors, "Errors");
				GUI.color = Color.yellow;
				m_ShowWarnings = GUILayout.Toggle(m_ShowWarnings, "Warnings");
				GUI.color = Color.white;
				m_ShowInfo = GUILayout.Toggle(m_ShowInfo, "Info");

				GUILayout.Label("\ud83d\udd0e", GUILayout.Width(40f));
				m_Filter = GUILayout.TextArea(m_Filter, GUILayout.Width(150f));

				GUI.enabled = m_Filter.Length > 0;

				if (GUILayout.Button("x", GUILayout.Width(20f)))
				{
					m_Filter = "";
				}

				GUI.enabled = true;
			}

			GUILayout.Space(10f);
		}
	}
}