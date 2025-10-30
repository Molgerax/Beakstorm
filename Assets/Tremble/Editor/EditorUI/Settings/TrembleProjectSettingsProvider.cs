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
	public static class TrembleProjectSettingsProvider
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Entry Points
		// -----------------------------------------------------------------------------------------------------------------------------
		[SettingsProvider] public static SettingsProvider ProvideAboutGUI()
            => ProvideTrembleTab(s => s.OnAboutGUI());

		[SettingsProvider] public static SettingsProvider ProvideSettingsGUI()
            => ProvideTrembleTab(order: 1, "Basic Settings", s => s.OnSettingsGUI(isEmbedded: true));

		[SettingsProvider] public static SettingsProvider ProvideNamingConventionsGUI()
            => ProvideTrembleTab(order: 2, "Naming Conventions", s => s.OnNamingConventionsGUI(isEmbedded: true));

		[SettingsProvider] public static SettingsProvider ProvideMaterialsGUI()
            => ProvideTrembleTab(order: 3, "Materials", s => s.OnMaterialsGUI(isEmbedded: true));

		[SettingsProvider] public static SettingsProvider ProvidePipelineGUI()
			=> ProvideTrembleTab(order: 4, "Pipeline", s => s.OnPipelineGUI(isEmbedded: true));

		[SettingsProvider] public static SettingsProvider ProvideAdvancedGUI()
			=> ProvideTrembleTab(order: 5, "Advanced", s => GUILayout.Label("Here be dragons! Most users should not need to change these options. But, if you're brave - select a subcategory on the left...", EditorStyles.wordWrappedLabel), dontCaptureKeywords: true);

		[SettingsProvider] public static SettingsProvider ProvideAdvancedGUI_Features()
            => ProvideTrembleTab(order: 5, "Advanced", subOrder: 0, "Features", s => s.OnAdvancedGUI_Features());
		[SettingsProvider] public static SettingsProvider ProvideAdvancedGUI_Debugging()
            => ProvideTrembleTab(order: 5, "Advanced", subOrder: 1, "Debugging", s => s.OnAdvancedGUI_Debugging());
		[SettingsProvider] public static SettingsProvider ProvideAdvancedGUI_SyncImport()
            => ProvideTrembleTab(order: 5, "Advanced", subOrder: 2, "Sync & Import", s => s.OnAdvancedGUI_SyncImport());
		[SettingsProvider] public static SettingsProvider ProvideAdvancedGUI_Materials()
            => ProvideTrembleTab(order: 5, "Advanced", subOrder: 3, "Materials", s => s.OnAdvancedGUI_Materials());
		[SettingsProvider] public static SettingsProvider ProvideAdvancedGUI_ImportScale()
            => ProvideTrembleTab(order: 5, "Advanced", subOrder: 4, "Import Scale", s => s.OnAdvancedGUI_ImportScale());

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private static TrembleSyncSettingsEditor s_SettingsEditor;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Internals
		// -----------------------------------------------------------------------------------------------------------------------------
		private static SettingsProvider ProvideTrembleTab(Action<TrembleSyncSettingsEditor> onGui, bool dontCaptureKeywords = false)
			=> ProvideTrembleTab(null, "Tremble", onGui, dontCaptureKeywords);
		private static SettingsProvider ProvideTrembleTab(int order, string categoryName, Action<TrembleSyncSettingsEditor> onGui, bool dontCaptureKeywords = false)
			=> ProvideTrembleTab($"{order} {categoryName}", categoryName, onGui, dontCaptureKeywords);

		private static SettingsProvider ProvideTrembleTab(int order, string categoryName, int subOrder, string subCategoryName, Action<TrembleSyncSettingsEditor> onGui, bool dontCaptureKeywords = false)
			=> ProvideTrembleTab($"{order} {categoryName}/{subOrder} {subCategoryName}", subCategoryName, onGui, dontCaptureKeywords);

		private static SettingsProvider ProvideTrembleTab(string path, string label, Action<TrembleSyncSettingsEditor> onGui, bool dontCaptureKeywords = false)
		{
			// Create an editor (or used cached)
			TrembleSyncSettings settings = TrembleSyncSettings.Get();

			if (!s_SettingsEditor)
			{
				s_SettingsEditor = (TrembleSyncSettingsEditor)UnityEditor.Editor.CreateEditor(settings);
			}

			// Prepend root to path
			string fullPath = "Project/Tremble";
			if (path != null)
			{
				fullPath += $"/{path}";
			}

			// Capture keywords
			List<string> keywords = new(64) { "Tremble", label };

			if (!dontCaptureKeywords)
			{
				s_SettingsEditor.EnsureInit(onlyKeywordsCapture: true);

				TrembleSettingsGUI.BeginKeywordCapture();
				{
					try
					{
						onGui(s_SettingsEditor);
					}
					catch (Exception ex)
					{
						Debug.LogException(ex);
					}
				}
				keywords.AddRange(TrembleSettingsGUI.EndKeywordCapture());
			}

			// Register path with order embedded :)
			return new(path: fullPath, SettingsScope.Project)
			{
				label = label,
				guiHandler = (searchContext) =>
				{
					s_SettingsEditor.EnsureInit();
					EditorGUILayoutUtil.Pad(10f, () =>
					{
						try
						{
							onGui(s_SettingsEditor);
						}
						catch (Exception ex)
						{
							Debug.LogException(ex);
							GUILayout.Label($"An error occurred rendering this page: {ex.Message}", EditorStyles.wordWrappedLabel);
						}
					});
					s_SettingsEditor.ResetLabelWidth();
				},
				keywords = keywords
			};
		}
	}
}