//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public class TrembleSyncSettingsWindow : EditorWindow
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private TrembleSyncSettings m_TrembleSyncSettings;
		private UnityEditor.Editor m_Editor;

		public static void OpenSettings()
		{
			TrembleSyncSettings settings;
			string settingsPath = TrembleAssetLoader.FindAssetPath<TrembleSyncSettings>();

			if (!settingsPath.IsNullOrEmpty())
			{
				settings = AssetDatabase.LoadAssetAtPath<TrembleSyncSettings>(settingsPath);
			}
			else
			{
				settings = CreateInstance<TrembleSyncSettings>();
				AssetDatabase.CreateAsset(settings, "Assets/Tremble Settings.asset");
			}

			TrembleSyncSettingsWindow window = GetWindow<TrembleSyncSettingsWindow>(true, "Tremble Settings", true);
			window.Init(settings);

			Vector2 size = new(520f, 640f);
			window.minSize = size;
			window.maxSize = size;
			window.position = new(EditorGUIUtility.GetMainWindowPosition().center - size / 2f, size);
			window.Show();
		}

		private void Init(TrembleSyncSettings settings)
		{
			m_TrembleSyncSettings = settings;
		}

		private void OnGUI()
		{
			m_Editor ??= UnityEditor.Editor.CreateEditor(m_TrembleSyncSettings);
			m_Editor.OnInspectorGUI();
		}

		private void OnDestroy()
		{
			if (m_Editor is not TrembleSyncSettingsEditor { IsAnyPropDirty: true } )
				return;

			TrembleEditorAPI.SyncToTrenchBroom();
		}
	}
}