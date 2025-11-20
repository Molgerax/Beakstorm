//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Import from Other Games/FGD Comparison", title: "The difference made by importing an FGD file", showInTree: false)]
	public class MapRepairFgdComparisonPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text("With an FGD file imported, you'll see that Tremble has the proper names and",
				"descriptions of all the fields of the map classes - rather than just the ones",
				"that happened to be set in the map.");

			Text("It's recommended to import the FGD whenever importing 'foreign' maps to make",
				"them easier to work with.");

			CustomPropertyDescription("No FGD File", () =>
			{
				Image("T_Manual_MapRepair_NoFGD", width: 700f, centred: false);
			});
			CustomPropertyDescription("With FGD File", () =>
			{
				Image("T_Manual_MapRepair_WithFGD", width: 700f, centred: false);
			});
		}
	}
}