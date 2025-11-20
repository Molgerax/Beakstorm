//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Materials/Material Groups")]
	public class MaterialGroupsPage : ManualPageBase
	{
		private TrembleSyncSettings m_SyncSettings;
		private MaterialGroup m_DefaultMaterialGroup;
		private Material[] m_UnexposedMaterials;

		protected override void OnInit()
		{
			m_SyncSettings = TrembleSyncSettings.Get();

			Material[] allMaterials = AssetDatabase
				.FindAssets("t:material", new[] { "Assets" })
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<Material>)
				.ToArray();

			m_DefaultMaterialGroup = MaterialGroup.CreateDefault();

			m_UnexposedMaterials = allMaterials
				.Where(m => !MaterialGroupUtil.IsMaterialPartOfAnyMaterialGroup(m))
				.ToArray();
		}

		protected override void OnGUI()
		{
			Text(
				"By default, Tremble will export all of the materials in your project into TrenchBroom.",
				"If your project has a lot of materials though, this can take some time,",
				"and there may be materials that you never want to use in TrenchBroom at all."
			);

			Text(
				"By using material groups, you can decide which materials to export to TrenchBroom,",
				"and even organise them into groups!"
			);

			H1("Assigning material groups");
			Text("There are 2 ways to set up material groups:");
			Bullet(
				"Click an individual material (or a group of materials) in Unity's Project window,",
				"and select a group from the dropdown at the top of the Inspector view."
			);
			Image("T_Manual_MaterialGroups");
			Bullet(
				"or, go into Tremble's settings (see Basics > Tremble's Settings), click 'Materials',",
				"and add one or more groups. Now you can assign materials to each group."
			);

			H1("How is my project set up?");

			if (m_SyncSettings.MaterialGroups is { Length: > 0 })
			{
				Text(
					$"Your project is using material groups, and you have {m_SyncSettings.MaterialGroups.Length}",
					"of them:"
				);

				foreach (MaterialGroup matGroup in m_SyncSettings.MaterialGroups)
				{
					Indent(() =>
					{
						Foldout($"{matGroup.Name}, {matGroup.Materials.Length} material(s)", () =>
						{
							foreach (Material mat in matGroup.Materials)
							{
								GUILayout.BeginHorizontal();
								{
									Bullet(mat.name);
									Button($"select {mat.name}", width: 200f, action: () => Selection.activeObject = mat);
								}
								GUILayout.EndHorizontal();
							}
						});
					});
				}

				if (m_UnexposedMaterials.Length == 0)
				{
					Text("...and that's all your materials!");
				}
				else
				{
					Text($"There are {m_UnexposedMaterials.Length} materials that are not exposed to TrenchBroom:");
					Indent(() =>
					{
						Foldout($"Unavailable material(s)", () =>
						{
							foreach (Material mat in m_UnexposedMaterials)
							{
								GUILayout.BeginHorizontal();
								{
									Bullet(mat.name);
									Button($"select {mat.name}", width: 200f, action: () => Selection.activeObject = mat);
								}
								GUILayout.EndHorizontal();
							}

							Text("To make a Material available for use, select it and use the Inspector to add it",
								"to a Material Group.");
						});
					});
				}
			}
			else
			{
				Text(
					$"Your project is not using material groups, and you have {m_DefaultMaterialGroup.Materials.Length}",
					"compatible material(s) in your project:"
				);
				Indent(() =>
				{
					Foldout("Show materials", () =>
					{
						foreach (Material material in m_DefaultMaterialGroup.Materials)
						{
							Bullet(material.name);
						}
					});
				});
			}

			string trembleMaterialsPath = "Project/Tremble/3 Materials";
			ActionBar_SingleAction("Edit Material Groups", () => SettingsService.OpenProjectSettings(trembleMaterialsPath));
		}
	}
}