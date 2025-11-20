//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Troubleshooting/Missing Geometry")]
	public class TroubleshootingMissingGeoPage : ManualPageBase
	{
		private const float IMAGE_WIDTH = 300f;

		protected override void OnGUI()
		{
			Text("If you create an area in a map that is completely enclosed (known in the mapping",
				"community as being 'sealed'), you might find that Tremble culls the",
				"inside of these areas entirely!");

			GUILayout.BeginHorizontal();
			{
				Bold("TrenchBroom (before):", width: IMAGE_WIDTH);
				Bold("Unity (before):", width: IMAGE_WIDTH);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				Image("T_Manual_MissingGeo_TB1", width: IMAGE_WIDTH, centred: false);
				Image("T_Manual_MissingGeo_Unity1", width: IMAGE_WIDTH, centred: false);
			}
			GUILayout.EndHorizontal();

			Text("This is because Q3Map2 (the map compiler) determines that these areas can never",
				"be reached, and thus is unnecessary geometry.");

			Text("In order to keep this geometry around, you need to place a Point or Brush entity",
				"(or a prefab) inside this area. Tremble will no longer cull the area, as it",
				"determines that there are gameplay elements inside the area and thus it is important.");

			GUILayout.BeginHorizontal();
			{
				Bold("TrenchBroom (after):", width: IMAGE_WIDTH);
				Bold("Unity (after):", width: IMAGE_WIDTH);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				Image("T_Manual_MissingGeo_TB2", width: IMAGE_WIDTH, centred: false);
				Image("T_Manual_MissingGeo_Unity2", width: IMAGE_WIDTH, centred: false);
			}
			GUILayout.EndHorizontal();
		}
	}
}