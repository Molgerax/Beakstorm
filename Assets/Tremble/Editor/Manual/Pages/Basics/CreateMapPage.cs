//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Basics/Create Map", "Create your first map")]
	public class CreateMapPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Number(
				"In the project window, right-click and select 'Create' > 'Tremble Map'. Give your map a name,",
				"then hit enter."
			);

			ActionBar_SingleAction("Create new map called 'Test' in Assets", () =>
			{
				MapFileAsset mapFileAsset = MapFileAsset.CreateInstance<MapFileAsset>();

				AssetDatabase.CreateAsset(mapFileAsset, "Assets/MAP_Test.asset");
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			});

			Number(
				"TrenchBroom will open the new map for you to edit, with your project's materials",
				"available to use."
			);

			Number(
				"Since you have not yet marked any Unity scripts as entities, you will not see any",
				"in TrenchBroom yet and can only create worldspawn brushes for now. See",
				"the 'Entities' section to get started!"
			);
			ActionBar_SingleAction("Read about Entities", () => GoToPage(typeof(EntitiesOverviewPage)));

			Number("When you're ready, save the map and return to Unity.");
			Number("You should see a prompt - click 'Yep!'. Your new map is added to your current Unity scene.");

			Image("T_Manual_CreateMap", width: 300f);

			Callout("If the prompt doesn't show, or you want to add it to another scene, manually drag your",
				"new map file into the scene.");
		}
	}
}