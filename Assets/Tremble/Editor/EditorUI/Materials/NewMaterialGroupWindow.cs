// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	internal class NewMaterialGroupWindow : EditorWindow
	{
		private const string MAT_NAME_ID = "MatNameField";
		
		private MaterialGroup[] m_ExistingGroups;
		private Action<string> m_OnAdded;

		private string m_Name;
		
		internal void Show(Rect fromRect, MaterialGroup[] existingGroups, Action<string> onAdded)
		{
			position = fromRect;
			
			m_ExistingGroups = existingGroups;
			m_OnAdded = onAdded;

			m_Name = "Group " + m_ExistingGroups.Length;
			ShowModalUtility();
		}

		private void OnGUI()
		{
			GUI.SetNextControlName(MAT_NAME_ID);
			m_Name = EditorGUILayout.TextField("Name", m_Name);
			
			GUI.FocusControl(MAT_NAME_ID);

			bool escaped = Event.current.keyCode == KeyCode.Escape;
			bool entered = Event.current.keyCode == KeyCode.Return;
			
			GUILayout.BeginHorizontal();
			{
				escaped |= GUILayout.Button("Cancel");
				entered |= GUILayout.Button("Create");
			}
			GUILayout.EndHorizontal();

			
			if (Event.current.type is EventType.Repaint or EventType.Layout)
				return;
			
			if (escaped)
			{
				Close();
			}
			else if (entered)
			{
				TrembleSyncSettings trembleSyncSettings = TrembleSyncSettings.Get();
				if (trembleSyncSettings.MaterialGroups.Length == 0)
				{
					string text = "You are adding your first Material Group, which is a great idea to organise your materials. However, you will need to manually assign each material to a material group for it to be available in TrenchBroom. All good?";
					int selection = EditorUtility.DisplayDialogComplex("Material Groups", text, "Understood, Continue", "Oh, No Thanks!", "Okay but add all materials into a group");
					switch (selection)
					{
						case 0: //ok
						{
							break;
						}
						case 1: // cancel
						{
							return;
						}
						case 2: // ok but add
						{
							// Quickly add a "user" group with current mats, and then continue
							MaterialGroup defaultGroup = MaterialGroup.CreateDefault();
							MaterialGroupUtil.MoveMaterialsToGroup(defaultGroup.Materials, defaultGroup.Name, createIfNotExist: true);
							break;
						}
					}
				}
				
				// Return new name back
				m_OnAdded(m_Name);
				Close();
			}
		}
	}
}