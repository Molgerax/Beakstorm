//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Advanced/Map Processors")]
	public class MapProcessorPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				$"While {nameof(IOnImportFromMapEntity)} can cover a lot of bases for custom import logic,",
				"sometimes you need to either:"
			);

			Bullet(
				"look at the map as a whole - for example, to capture all",
				"entities of a certain class into a list, and pass this to some system"
			);

			Bullet(
				"or, you might want to use Editor-only functionality, which can be tricky",
				"to write inside an OnImportFromMapEntity callback."
			);

			Text(
				$"For this, you can create your own {nameof(MapProcessorBase)} subclass, and override the",
				"functions you need."
			);

			Text(
				"An example of this is provided in SampleMapProcessor.cs. The example finds all",
				"the Sheep Spawners in the map, and logs their position. However in a real game",
				"you might, for example, take the positions of these and write them into a",
				"ScriptableObject which could be later used by a minimap system."
			);

			Text(
				"Map Processors can either be set per-Map or in the Tremble Pipeline settings to affect every",
				"map in your project.");

			Number("To set a per-Map processor, click your map file in the",
				"Project tab, and ensure your map processor class(es) are set, like this:"
			);

			Image("T_Manual_MapProcessor");

			Number("To set Map processors globally for your project, open the Pipeline settings.");
			ActionBar_SingleAction("Open Pipeline Settings", () =>
			{
				const string TREMBLE_PIPELINE_PATH = "Project/Tremble/4 Pipeline";
				SettingsService.OpenProjectSettings(TREMBLE_PIPELINE_PATH);
			});

			H1("Sample Map Processor");
			Foldout("Show Code", () =>
			{
				Code(
					$"using {nameof(TinyGoose)}.{nameof(Tremble)};",
					"",
					$"public class MyCoolMapProcessor : {nameof(MapProcessorBase)}",
					"{",
					$"    public override void OnProcessingStarted({nameof(TrembleMapImportSettings)} context, GameObject root, {nameof(MapBsp)} mapBsp)",
					"    {",
					"        Debug.Log($\"Map processing started for {mapBsp.MapName}\");",
					"    }",
					$"    public override void OnProcessingCompleted({nameof(TrembleMapImportSettings)} context, GameObject root, {nameof(MapBsp)} mapBsp)",
					"    {",
					"        Debug.Log($\"Map processing completed for {mapBsp.MapName} - all entities spawned\");",
					"    }",
					"    ",
					$"    public override void ProcessWorldSpawnProperties({nameof(TrembleMapImportSettings)} context, {nameof(MapBsp)} mapBsp, {nameof(BspEntity)} entity, GameObject rootGameObject)",
					"    {",
					"        Debug.Log($\"Worldspawn created for {mapBsp.MapName} - map-wide properties can be read from 'entity'\");",
					"    }",
					"    ",
					$"    public override void ProcessPrefabEntity({nameof(TrembleMapImportSettings)} context, {nameof(MapBsp)} mapBsp, {nameof(BspEntity)} entity, GameObject prefab)",
					"    {",
					"        Debug.Log(\"Found a prefab!\");",
					"    }",
					$"    public override void ProcessBrushEntity({nameof(TrembleMapImportSettings)} context, {nameof(MapBsp)} mapBsp, {nameof(BspEntity)} entity, GameObject brush)",
					"    {",
					"        Debug.Log(\"Found a brush!\");",
					"    }",
					$"    public override void ProcessPointEntity({nameof(TrembleMapImportSettings)} context, {nameof(MapBsp)} mapBsp, {nameof(BspEntity)} entity, GameObject point)",
					"    {",
					"        Debug.Log(\"Found a point!\");",
					"    }",
					"}");
			});
		}
	}
}