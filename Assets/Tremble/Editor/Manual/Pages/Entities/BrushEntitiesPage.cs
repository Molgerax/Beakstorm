//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Entities/Brush Entities")]
	public class BrushEntitiesPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Brush Entities are brushes (models/shapes) in the map, which you want to apply a",
				"MonoBehaviour script to. For example, a moving platform whose shape you design in",
				"your TrenchBroom. Or, a trigger box which kills players on entry (see below).");

			Image("T_Manual_BrushEntity");

			Text("The following properties are available:");

			PropertyDescription("name",
				"The name of the entity in TrenchBroom.",
				"If omitted, we convert your MonoBehaviour name according to your Naming Convention in settings."
			);

			PropertyDescription("category",
				"The menu category to show the entity under, for example 'solid'.",
				$"If omitted, we use the default prefix, '{FgdConsts.BRUSH_PREFIX}'."
			);

			PropertyDescription("type",
				"Which brush type to use, which defines how the brush appears and how its collision",
				"functions."
			);

			Foldout("Available Brush Types", () =>
			{
				PropertyDescription("Solid",
					"A Solid brush with a texture.",
					"Example: a moving platform.");
				PropertyDescription("Liquid",
					"A brush with a texture, but with a trigger collision instead of a solid one.",
					"Example: a volume of water/lava, which damages players when entering.");
				PropertyDescription("Trigger",
					"A completely invisible brush which has a trigger collision.",
					"Example: a volume which triggers an event when players walk into the area.");
				PropertyDescription("Invisible",
					"A completely invisible brush which has solid collision.",
					"Example: an invisible wall to stop players going out of bounds.");
			});

			H1("Brush Entity Wizard");
			Text(
				"Tremble can guide you through creating the correct attribute code for your entity,",
				"to help you get up and running quickly."
			);

			ActionBar_SingleAction("Build Brush Entity", () => GoToPage(typeof(BrushBuilderPage)));
		}
	}
}