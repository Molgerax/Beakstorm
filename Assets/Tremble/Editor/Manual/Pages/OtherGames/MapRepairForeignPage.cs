//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Import from Other Games/Maps from other games")]
	public class MapRepairForeignPage : ManualPageBase
	{
		private static readonly string[] TAB_NAMES = new[]
		{
			"Materials/Textures", "Entities"
		};

		private int m_SelectedTabIdx;

		protected override void OnGUI()
		{
			Image("T_Manual_MapRepair_Required", width: 220f);

			Text("You can also use Map Repair to patch up what Tremble terms 'foreign maps' (maps",
				"from other games) to work with your Tremble/Unity project. This also allows you to",
				"import/export maps between your own projects, or even import maps from other games",
				"(such as Quake®!).");

			Text("For any unrecognised entities in maps, Map Repair will generate new MonoBehaviour classes",
				"to match them. This saves time creating C# classes for brand new entities you've",
				"added to the map.");

			Text("These generated classes will allow Tremble to import these new entities and will",
				"bring any values from the map into Unity - but of course, it won't provide any game",
				"logic such as movement or AI - you'd have to code that yourself.");


			H2("Available Tabs");
			{
				switch (Tabs(ref m_SelectedTabIdx, TAB_NAMES, width: 420f))
				{
					case 0: // Textures
						GUILayout.BeginHorizontal();
						{
							Image("T_Manual_MapRepair_Textures", width: 400f);
							GUILayout.BeginVertical();
							{
								Text("This tab shows a list of all of the map's textures, and Tremble’s best guess",
									"at which project Materials match those textures.");
								Bullet("You can then change which material each texture maps to manually, if you desire.");
								Bullet("You can click the small '!' button to apply a given material to all missing materials",
									"(for example, if you want to apply a single material to all unknown materials in a map)");
							}
							GUILayout.EndVertical();
						}
						GUILayout.EndHorizontal();
						break;

					case 1: // Entities
						GUILayout.BeginHorizontal();
						{
							Image("T_Manual_MapRepair_Entities", width: 400f);
							GUILayout.BeginVertical();
							{
								Text("This tab shows a list of all the map's point and brush entities, and Tremble’s best guess",
									"at which MonoBehaviour classes match those entities.");
								Bullet("You can then change which class each entity maps to manually, if you desire.");
								Bullet("Leave an unknown entity class as 'Generate new class' to ask Tremble to generate",
									"a new MonoBehaviour class that matches the entity. This may need manual tweaking",
									"afterwards, but will be valid and able to parse the map supplied.");
								Bullet("At the bottom of this tab, you are able to designate where new classes",
									"are generated and an optional namespace to use for them.");
							}
							GUILayout.EndVertical();
						}
						GUILayout.EndHorizontal();
						break;
				}
			}
		}
	}
}