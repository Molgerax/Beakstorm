// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;

namespace TinyGoose.Tremble.Editor
{
#if UNITY_2022_1_OR_NEWER
	[Overlay(typeof(SceneView), "Tremble Sync", true, id = ID)]
#else
	[Overlay(typeof(SceneView), "Tremble Sync", true)]
#endif
	public class TrembleSyncOverlay : ToolbarOverlay
	{
		private const string ID = "TrembleSync";

		public TrembleSyncOverlay() : base(ReimportMapButton.ID, SyncButton.ID, ConfigButton.ID, ManualButton.ID)
		{
		}

		protected override Layout supportedLayouts => Layout.HorizontalToolbar | Layout.VerticalToolbar;
	}

	[EditorToolbarElement(ID, typeof(SceneView))]
	public class ReimportMapButton : EditorToolbarButton
	{
		public const string ID = "Tremble/Reimport";
		
		public ReimportMapButton()
		{
			icon = TrembleIcons.IMPORT_PNG.Get();
			tooltip = "Reimport Current Map";
			clicked += TrembleEditorAPI.ReimportCurrentMap;
		}
	}
	
	[EditorToolbarElement(ID, typeof(SceneView))]
	public class SyncButton : EditorToolbarButton
	{
		public const string ID = "Tremble/Sync";

		public SyncButton()
		{
			icon = TrembleIcons.SYNC_PNG.Get();
			tooltip = "Sync to TrenchBroom";
			clicked += TrembleEditorAPI.SyncToTrenchBroom;
		}
	}
	
	[EditorToolbarElement(ID, typeof(SceneView))]
	public class ConfigButton : EditorToolbarButton
	{
		public const string ID = "Tremble/Config";

		public ConfigButton()
		{
			icon = TrembleIcons.CONFIG_PNG.Get();
			tooltip = "Tremble Settings";
			clicked += TrembleEditorAPI.OpenSettings;
		}
	}

	[EditorToolbarElement(ID, typeof(SceneView))]
	public class ManualButton : EditorToolbarButton
	{
		public const string ID = "Tremble/Manual";

		public ManualButton()
		{
			icon = TrembleIcons.MANUAL_PNG.Get();
			tooltip = "Tremble Manual";
			clicked += () => TrembleEditorAPI.OpenManual();
		}
	}
}