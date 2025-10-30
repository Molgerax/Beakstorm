//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Import from Other Games/Texture WADs from other games")]
	public class ImportWadPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text("Unfortunately, you cannot supply a WAD file in your TrenchBroom map files for Tremble to use.");

			Text("However, Tremble does support extracting WAD files into your Unity project, which in turn",
				"makes them available to TrenchBroom.");

			Callout("Note: Only WAD2 (Quake) format WAD files are supported at the moment.",
				"WAD3 (Valve) are not supported (yet?)");

			Text("To get started, simply drop a WAD file into your Unity project.");

			Text("This process can take a while for a large WAD file - up to a minute or two.",
				"It only needs to be done once.");
			Image("T_Manual_WadImport2");
		}
	}
}