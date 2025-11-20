//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Troubleshooting/'dylib can't be opened' (macOS only)")]
	public class TroubleshootingDLLPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Image("T_Manual_Trouble_DLL1", width: 200f);

			Text(
				"This can sometimes occur because Q3Map2, the converter which transforms your .map",
				"file into something Tremble can understand, is not currently signed.",
				"(this may change in future!)"
			);

			Text(
				"It's a warning from macOS that you may not wish to trust the Q3Map2 binary."
			);

			Foldout("Why should I trust it?", () =>
			{
				Bullet("Q3Map2 is distributed with Tremble from the Asset Store and is safe to use");
				Bullet("You can verify the source code OR build your own Q3Map2 library, if you prefer");

				ActionBar_SingleAction("Verify Q3Map2 source on Github", () => Application.OpenURL("https://github.com/tinygooseuk/q3map2/tree/libq3map2"));
			});

			H1("To fix the error, do the following:");

			Number("Click 'Done' to close the 'can't be opened' dialogue");
			Number("Open 'System Settings' on your Mac, and scroll down to 'Privacy & Security' in the left sidebar");
			Number("Scroll down to 'Security', and click 'Allow Anyway'");

			Image("T_Manual_Trouble_DLL2");

			Number("You may be prompted to enter your password or use Touch ID. Do so");
			Number("Close System Settings and return to Unity");
			Number("Try to import the map again. You will see the same error, but this time there will be an 'Open' button. Click 'Open'");

			Text("This should now work!");
		}
	}
}