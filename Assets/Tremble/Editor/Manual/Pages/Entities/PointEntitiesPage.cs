//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Entities/Point Entities")]
	public class PointEntitiesPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Point Entities are points/positions in space in the map, which you want to apply a",
				"MonoBehaviour script to. They do not have a visual representation (mesh).",
				"For example, a target position that an enemy should move to. Or, a position in space",
				"in which to spawn the player (see below).");

			Image("T_Manual_PointEntity");

			Text("The following properties are available:");

			PropertyDescription("name",
				"The name of the entity in TrenchBroom.",
				"If omitted, we convert your MonoBehaviour name according to your Naming Convention in settings."
			);

			PropertyDescription("category",
				"The menu category to show the entity under, for example 'solid'.",
				$"If omitted, we use the default prefix, '{FgdConsts.BRUSH_PREFIX}'."
			);

			PropertyDescription("colour, size",
				"Colour and Size define how to display this Point entity's preview cube in TrenchBroom."
			);

			PropertyDescription("sprite",
				"(optional) A sprite to show, instead of a coloured cube for this Point entity in TrenchBroom.",
				"This should be the name of a texture in your project, e.g. 'T_MyTexture'."
			);

			H1("Point Entity Wizard");
			Text(
				"Tremble can guide you through creating the correct attribute code for your entity,",
				"to help you get up and running quickly."
			);

			ActionBar_SingleAction("Build Point Entity", () => GoToPage(typeof(PointBuilderPage)));
		}
	}
}