//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("User Generated Content/For Players")]
	public class UgcPlayerHintsPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Experimental();

			Text("This page gives a bit more detail on what players need, in order to import maps");

			H2("How do players get the FGD file/textures/etc?");
			Text(
				"These are installed onto the user's computer when you call",
				$"{nameof(TrembleRuntimeAPI)}.{nameof(TrembleRuntimeAPI.Initialise)},",
				"so there is no need to distribute these yourself."
			);

			H2("Do players need a copy of my Unity project to import maps?");
			Text(
				"Absolutely not! Just a copy of your game with Tremble installed and configured."
			);

			H2("Do players need to purchase a licence for Tremble, on the Asset Store?");
			Text(
				"Again - absolutely not! Just a copy of your game with Tremble installed and configured."
			);

			H2("What if players want to use their own textures?");
			Text(
				"Generally this is discouraged, especially when distributing user-generated maps.",
				"However - it is possible! Users should place their textures (PNG only!) into your game's",
				"Library/baseq3/Textures folder (this will be created on first run). When Tremble",
				"finds these unexpected textures referenced in a map, it will create Unity",
				"materials on-the-fly to display them."
			);
			Text(
				"Note, that you will need to distribute these textures with the maps, and place",
				"them into the correct folder at runtime for this to work when sharing maps between",
				"players."
			);

			H2("What about if these textures should be PBR materials?");
			Text(
				"Yup - you might want to change the metallic and smoothness parameters when",
				"importing a user's custom texture. To do that, Tremble uses a naming convention."
			);
			PropertyDescription("s[0-100]", "Smoothness percent");
			PropertyDescription("m[0-100]", "Metallic/metalness percent");
			Text(
				"For example, a texture which should be 60% smooth and 100% metal could be named: ",
				"'MyCoolTexture_s60_m100.png'."
			);
		}
	}
}