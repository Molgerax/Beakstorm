//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Entities/Prefabs/Prefab Entities")]
	public class PrefabEntitiesPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Prefab Entities are 1:1 representations of your Unity prefabs, which can be dropped into a map.",
				$"All you have to do is mark up your scripts with '[{FormatAttributeName(typeof(PrefabEntityAttribute))}]' to synchronise properties and more!");

			Image("T_Manual_PrefabEntity");

			Text("The following properties are available:");

			PropertyDescription("category",
				"The menu category to show the entity under, for example 'player'.",
				$"If omitted, we use the default prefix, '{FgdConsts.PREFAB_PREFIX}'."
			);

			PropertyDescription("excludePrefab(s)",
				"Exclude prefabs with the given name. For example, if your",
				"script appears on multiple prefabs 'P_Coin' and 'P_CoinSpecial', and you only",
				" want to show 'P_Coin' in TrenchBroom, you can pass 'P_CoinSpecial' in here."
			);

			PropertyDescription("includePrefab(s)",
				"Only include prefabs with the given name. For example, if your",
				"script appears on multiple prefabs 'P_Coin' and 'P_CoinSpecial', and you only",
				" want to show 'P_Coin' in TrenchBroom, you can pass 'P_Coin' in here."
			);

			PropertyDescription("onlyVariants",
				"Only include variants of this prefab, not the base prefab itself.",
				"For example, imagine your game has a 'P_Sign' prefab, which is an empty signpost,",
				"but also has Prefab Variants 'P_Sign_Info' and 'P_Sign_QuestionMark' - ticking",
				"this box will export those variants but not the base 'P_Sign'."
			);

			H1("Prefab Entity Wizard");
			Text(
				"Tremble can guide you through creating the correct attribute code for your entity,",
				"to help you get up and running quickly."
			);

			ActionBar_SingleAction("Build Prefab Entity", () => GoToPage(typeof(PrefabBuilderPage)));
		}
	}
}