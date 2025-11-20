//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Troubleshooting/Pink materials in TrenchBroom")]
	public class TroubleshootingPinkMaterialsPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text("Pink materials can appear in either Unity or TrenchBroom, and they indicate that",
				"a Material or Shader could not be found.");

			Image("T_Manual_Trouble_PinkMaterials");

			H1("For your own materials/textures not rendering");
			Text("Try to run Map Repair on the map with pink materials. You will be able to swap the",
				"missing materials for valid ones.");
			ActionBar_SingleAction("Open Map Repair", MapRepairWindow.OpenMapRepair);

			H1("For Tremble sample materials not rendering, under URP or HDRP");
			Text("Tremble's sample project materials are built for the Built-In Render Pipeline.",
				"This means, if you're using URP or HDRP, you should manually upgrade them to your pipeline.");

			Text("There's a great tutorial for this by SpawnCampGames on YouTube:");
			Image("T_Manual_Trouble_FixPinkMaterialsVideo");
			ActionBar_SingleAction("Open Video", () => Application.OpenURL("https://www.youtube.com/watch?v=V_EGF1M3fgY"));
		}
	}
}