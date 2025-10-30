//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("User Generated Content/Setup", title: "UGC Setup")]
	public class UgcSetupPage : ManualPageBase
	{
		private TrembleSyncSettings m_SyncSettings;

		private bool IsAddressablesInstalled =>
#if ADDRESSABLES_INSTALLED
			true;
#else
			false;
#endif

		private bool m_IsInstalling;

		protected override void OnInit()
		{
			m_SyncSettings = TrembleSyncSettings.Get();
		}

		protected override void OnGUI()
		{
			Experimental();

			Text(
				"Tremble now supports importing maps at runtime, which allows players to create their",
				"own maps in TrenchBroom, and import it into your game themselves."
			);

			Text(
				"You could use something like Unity's UGC package, or Steam Workshop to distribute",
				"maps to players (which is up to you to implement). Tremble just needs the map file",
				"to load on disk somewhere, and it will import it at runtime."
			);

			H1("Setup");
			Text(
				"There are a few things to set up first, before you can use this feature. This page",
				"will help you do that."
			);

			CheckListItem("Install the Addressables package from Unity", m_IsInstalling || IsAddressablesInstalled, "Install", () =>
			{
				m_IsInstalling = true;
				UnityEditor.PackageManager.Client.Add("com.unity.addressables");
			});

			CheckListItem("Enable the Tremble setting for UGC", m_SyncSettings.AllowStandaloneImport, "Enable", () =>
			{
				SerializedObject so = new(m_SyncSettings);
				so.FindBackedProperty(nameof(m_SyncSettings.AllowStandaloneImport)).boolValue = true;
				so.ApplyModifiedPropertiesWithoutUndo();

				TrembleEditorAPI.SyncToTrenchBroom();
			});

			H1("Initialising the system");
			{
				Text(
					"This step will prepare Tremble for loading maps at runtime.",
					"Unfortunately, it can lock your game up for about 200ms - so the idea is to do",
					"this during your game's startup code, to allow Tremble to initialise",
					"without causing your game to hitch during gameplay."
				);
				Code("await TrembleRuntimeAPI.Initialise();");
			}

			H1("Loading a map at runtime");
			{
				Text("Loading a map at runtime couldn't be simpler - simply call the following function:");

				Code(
					$"{nameof(MapDocument)} map = await {nameof(TrembleRuntimeAPI)}.{nameof(TrembleRuntimeAPI.LoadMapAsync)}(\"C:\\somemap.map\");",
					"",
					"// Your map is now in the current scene, if loading succeeded.",
					"// Check `map.Result` to see if it worked, and the `map.gameObject` is the root",
					"// of your new map!",
					"Debug.Log($\"Result: {map.Result}\");",
					"Debug.Log($\"Loaded map: {map.BspName}\");"
				);

				H2("Notes");
				{
					Bullet("You can also provide additional arguments, such as subdivision parameters and smoothing angles.");
					Bullet("Tremble respects Application.backgroundLoadingPriority, which you can use to signal how",
						"much time you want Unity to dedicate per frame to async loading. Higher priorities will load",
						"your map faster, but at the expense of dropping more frames during the load.");
				}
			}
		}
	}
}