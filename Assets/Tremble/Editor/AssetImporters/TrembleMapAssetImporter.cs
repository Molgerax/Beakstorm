//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
// This class based on BSP Map Tools for Unity by John Evans (evans3d512@gmail.com)
//

using System;
using System.Collections.Generic;
using UnityEditor.AssetImporters;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ScriptedImporter(1, "map", importQueueOffset: 20000)]
	public class TrembleMapAssetImporter : ScriptedImporter
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Exposed
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("The Map Processors to use (or none). Used to perform logic for individual entities from the map or on the map as a whole.")]
		private MapProcessorClass[] m_Processors;

		[SerializeField, Tooltip("Whether to split the mesh when surface area is too large?")]
		private bool m_SplitMesh = true;

		[SerializeField, Tooltip("Maximum surface area (in m^2) before splitting meshes")]
		private float m_MaxMeshSurfaceArea = 3000;

		[SerializeField, Tooltip("The angle above which to smooth edges.")]
		private float m_SmoothingAngle = 45;

		[SerializeField, Tooltip("Extra commandline to pass to Q3Map2 - see https://en.wikibooks.org/wiki/Q3Map2 for more details")]
		private string m_ExtraCommandLineArgs = "";

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Entry Point
		// -----------------------------------------------------------------------------------------------------------------------------
		public override async void OnImportAsset(AssetImportContext ctx)
		{
			// Check autosave and bail
			if (String.Equals(Path.GetFileName(Path.GetDirectoryName(ctx.assetPath)), "autosave", StringComparison.InvariantCultureIgnoreCase))
			{
				return;
			}

			// Check we have a functioning setup (not first import, for example)
			if (!Directory.Exists(TrembleConsts.BASEQ3_PATH))
			{
				ctx.LogImportWarning($"Can't import map {Path.GetFileName(ctx.assetPath)}. Please sync first.");
				return;
			}

			// Check we have our Q3Map2 DLL
			if (Q3Map2DllDownloaderWindow.OpenIfRequired())
			{
				ctx.LogImportError($"Can't import map {Path.GetFileName(ctx.assetPath)}. Please install Q3Map2 first.");
				return;
			}

			if (MapRepairWindow.IsMapForeign(ctx.assetPath, out string gameName))
			{
				gameName ??= "Quake(?)";

				bool repairNow = EditorUtility.DisplayDialog(
					title: "Map Repair Required",
					message: $"You are trying to import a map from '{gameName}'. Before you can use this map, " +
					         "we'll need to repair it. This converts it for use with Tremble " +
					         $"and '{TrembleConsts.GAME_NAME}'. Shall we?",
					"Repair and Resave", "Not Now");

				if (repairNow)
				{
					MapRepairWindow.OpenMapRepair(ctx.assetPath);
				}

				ctx.LogImportError($"Map '{Path.GetFileNameWithoutExtension(ctx.assetPath)}' needs repair to function with Tremble. Right-click the map and select 'Repair Map'.");
				return;
			}

			// Clear our cached asset lists
			UnityObjectFieldConverterStatics.s_CachedAssetLists.Clear();

			MapProcessorClass[] allProcessors = TrembleSyncSettings.Get().PipelineRunMapProcessors  // global processors
				? TrembleSyncSettings.Get().EnabledMapProcessors
				: Array.Empty<MapProcessorClass>();

			if (m_Processors != null)
			{
				allProcessors = allProcessors.Concat(m_Processors).Distinct().ToArray(); // map-based processors
			}

			TrembleMapImport importer = new(new()
			{
				ProcessorClasses = allProcessors,
				SplitMesh = m_SplitMesh,
				MaxMeshSurfaceArea = m_MaxMeshSurfaceArea,
				SmoothingAngle = m_SmoothingAngle,
				ExtraCommandLineArgs = m_ExtraCommandLineArgs,
				AssetGUID = AssetDatabase.GUIDFromAssetPath(ctx.assetPath).ToString(),

				OnObjectAdded = ctx.AddObjectToAsset,
				OnDependencyAdded = o =>
				{
					if (TrembleSyncSettings.Get().AutomaticallyReimportWhenDependencyChanges)
					{
						ctx.DependsOnSourceAsset(AssetDatabase.GetAssetPath(o));
					}
				},
				OnWadsFound = OnWadsFound,
				OnEntityAdded = OnEntityAdded,

				OnError = err => ctx.LogImportError(err),
				OnWarning = warn => ctx.LogImportWarning(warn),
				OnQ3Map2Result = OnQ3Map2Result
			});

			TrembleTimer.BeginSession($"Tremble Map Import for {Path.GetFileNameWithoutExtension(ctx.assetPath)}");
			MapDocument document = await importer.ImportMapAndCreatePrefab(ctx.assetPath);
			TrembleTimer.EndSession();

			ctx.AddObjectToAsset("map", document.gameObject);
			ctx.SetMainObject(document.gameObject);
		}

		private void OnEntityAdded(GameObject gameObject, Type entityType)
		{
			if (entityType == null)
				return;

			// Point entity with explicit icon?
			entityType.GetCustomAttributes(out PointEntityAttribute pea);

			if (pea is { Sprite: not null })
			{
				string spritePath = TrembleAssetLoader.FindAssetPath<Texture2D>(pea.Sprite);
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);

				EditorGUIUtility.SetIconForObject(gameObject, texture);
				return;
			}

			// Implicit
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			if (syncSettings.EntityIconStyle == EntityIconStyle.Nothing)
				return;

			if (entityType.FullName == null)
				return;

			int hash = Math.Abs(entityType.FullName.GetHashCode());

			switch (syncSettings.EntityIconStyle)
			{
				case EntityIconStyle.ColouredDiamondShapes:
					int shapeIdx = hash % (int)EditorGameObjectIconUtil.EditorIcon.MAX;
					gameObject.SetEditorIcon((EditorGameObjectIconUtil.EditorIcon)shapeIdx);
					break;
				case EntityIconStyle.ColouredLabels:
					int labelIdx = hash % (int)EditorGameObjectIconUtil.EditorLabel.MAX;
					gameObject.SetEditorIcon((EditorGameObjectIconUtil.EditorLabel)labelIdx);
					break;
			}
		}

		private void OnWadsFound(string[] allWads)
		{
			string[] validWads = allWads.Where(File.Exists).ToArray();

			foreach (string wad in validWads)
			{
				Debug.LogWarning($"Wad file found in map: {Path.GetFileName(wad)}. You should drop this file into your Unity project in order to use its materials!");
			}

			// bool copyThem = false;
			//
			// if (validWads.Length == 0)
			// {
			// 	EditorUtility.DisplayDialog(
			// 		title: "Wad Files",
			// 		message: $"Your map contains {allWads.Length} unknown wad file reference(s). " +
			// 		         "These are not directly supported by Tremble. If you want to use the textures " +
			// 		         "from your wad file(s), drag them into your Unity project and extract them.", "Ahh, okay");
			// }
			// else if (validWads.Length == allWads.Length)
			// {
			// 	copyThem = EditorUtility.DisplayDialog(
			// 		title: "Wad Files",
			// 		message: $"Your map contains {validWads.Length} wad file reference(s). " +
			// 		         "These are not directly supported by Tremble. If you want to use the textures " +
			// 		         "from your wad file(s), they need extracting into your Unity project. Want to import them now?", "Yup!", "Nope");
			// }
			// else
			// {
			// 	int numMissing = allWads.Length - validWads.Length;
			//
			// 	copyThem = EditorUtility.DisplayDialog(
			// 		title: "Wad Files",
			// 		message: $"Your map contains {validWads.Length} valid wad file reference(s), and {numMissing} which Tremble couldn't locate. " +
			// 		         "These are not directly supported by Tremble. If you want to use the textures " +
			// 		         "from your wad file(s), they need extracting into your Unity project. Want to import the ones we could find now?", "Yup!", "Nope");
			// }
			//
			// if (copyThem)
			// {
			// 	string folderName = Path.GetDirectoryName(assetPath);
			//
			// 	foreach (string existingPath in validWads)
			// 	{
			// 		string filename = Path.GetFileName(existingPath);
			// 		File.Copy(existingPath, Path.Combine(folderName, filename));
			// 	}
			//
			// 	EditorApplication.delayCall += () => AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
			// }
		}

		private void OnQ3Map2Result(Q3Map2Result result, List<string> output)
		{
			// Show results?
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			bool alwaysShowResult = syncSettings.Q3Map2ResultDisplayType == Q3Map2ResultDisplayType.AlwaysShow;
			bool showWarnings = syncSettings.Q3Map2ResultDisplayType == Q3Map2ResultDisplayType.ShowWhenWarningsOccur;

			bool showWindow = result is Q3Map2Result.Failed or Q3Map2Result.FailedWithMissingTextures;
			showWindow |= (result is Q3Map2Result.SucceededWithWarnings && showWarnings);
			showWindow |= alwaysShowResult;

			if (showWindow)
			{
				Q3Map2OutputWindow.Open(output, alwaysShowResult);
			}
		}
	}
}