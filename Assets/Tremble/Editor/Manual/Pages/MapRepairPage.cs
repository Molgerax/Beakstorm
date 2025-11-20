//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Map Repair")]
	public class MapRepairOverviewPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Sometimes, after renaming entities or materials, your map might import in a broken state,",
				"missing materials or functionality. In this case, you can right click the map in Unity,",
				"and select 'Repair Map' (or, go to Tools > Tremble > Repair Map)."
			);

			Image("T_Manual_MapRepair");

			ActionBar_SingleAction("Open Map Repair", MapRepairWindow.OpenMapRepair);

			Callout("Note, that you should ensure TrenchBroom does not have your map open while it is repaired.");

			H2("Other uses for map repair");
			Bullet(
				"Swap materials around, or reassign entities in the map wholesale - which can",
				"be far quicker than manually editing them."
			);
			Bullet(
				"Import maps from Quake®, or another game using .map files"
			);

			ActionBar_SingleAction("Show me how to import maps from other games", () => GoToPage(typeof(MapRepairForeignPage)));
		}
	}
}