// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using Color = UnityEngine.Color;
using Debug = UnityEngine.Debug;
using FileMode = System.IO.FileMode;

namespace TinyGoose.Tremble.Editor
{
	public static class TrembleSync
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private static readonly List<Task> s_BackgroundTasks = new(256);
		private static int s_ProgressTask;

		private static TrembleSyncSettings SyncSettings => TrembleSyncSettings.Get();


		internal static async Task PerformSync()
		{
			// Check for upgrades!
			VersionCheck.FetchLatestVersionsInBackground(() => VersionCheck.PromptToUpgradeIfAvailable());

			// Upgrade - check material export size is sane
			UpgradeMaterialExportSettingsIfRequired();

			TrembleTimer.BeginSession("Tremble Sync");

			try
			{
				// Clear cached asset lists
				UnityObjectFieldConverterStatics.s_CachedAssetLists.Clear();

				// Calculate working folders
				s_ProgressTask = ProgressUtil.Start("Trembling...");

				// Create lookups
				string baseq3 = TrembleConsts.BASEQ3_PATH;
				string baseq3Old = TrembleConsts.BASEQ3_PATH_OLD;
				string modelsFolder = TrembleConsts.BASE_MODELS_PATH;
				string baseMaterialsFolder = TrembleConsts.BASE_MATERIALS_PATH;

				TrembleTimerScope createLookupTimer = new(TrembleTimer.Context.CreateTypeLookups);

				PrefabNameLookup prefabNameLookup = new(SyncSettings);
				MaterialNameLookup materialNameLookup = new(SyncSettings);
				MapTypeLookup typeLookup = new(SyncSettings);
				ModTimeDB modTimeDB = new(TrembleConsts.MODTIME_DB_PATH);

				createLookupTimer.Dispose();

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Gather lists of resources
				// -----------------------------------------------------------------------------------------------------------------------------
				TrembleTimerScope exportEntitiesTimer = new(TrembleTimer.Context.ExportEntities);

				List<string> allPrefabPaths = prefabNameLookup.AllPrefabPaths;
				List<Type> allBrushTypes = TypeDatabase.GetTypesWithAttribute<BrushEntityAttribute>().ToList();
				List<Type> allPointTypes = TypeDatabase.GetTypesWithAttribute<PointEntityAttribute>().ToList();
				List<Type> allScriptableObjectTypes = new(128);

				// User supplied a list of materials - use that!
				List<MaterialGroup> allMaterialGroups = SyncSettings.GetMaterialGroupsOrDefault();
				int numMaterials = allMaterialGroups.Sum(mg => mg.Materials.Length);

				// Collect prefab materials and  transparent brushes
				List<string> prefabMaterialPaths = new(allPrefabPaths.Count * 2);
				List<string> transparentBrushes = new(allBrushTypes.Count);

				// Collect assets
				List<AssetMetadata> assets = new(allPrefabPaths.Count + numMaterials);

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Work out steps, create folders
				// -----------------------------------------------------------------------------------------------------------------------------
				int numSteps = numMaterials + allPrefabPaths.Count + allPointTypes.Count + allBrushTypes.Count;

				if (Directory.Exists(baseq3Old))
				{
					Directory.Delete(baseq3Old, true);
				}

				if (Directory.Exists(baseq3))
				{
					Directory.Move(baseq3, baseq3Old);
				}

				DirectoryUtil.EmptyAndCreateDirectory(baseq3);
				DirectoryUtil.CreateAllDirectories(modelsFolder);
				DirectoryUtil.CreateAllDirectories(baseMaterialsFolder);

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Export Prefabs
				// -----------------------------------------------------------------------------------------------------------------------------
				int step = 0;

				FgdWriter fgdWriter = GatherEntities(ref step, numSteps,
					prefabNameLookup, materialNameLookup, typeLookup, modTimeDB,
					modelsFolder, baseMaterialsFolder,
					prefabMaterialPaths, transparentBrushes,
					allBrushTypes, allPointTypes, allScriptableObjectTypes,
					assets);
				exportEntitiesTimer.Dispose();

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Data Assets
				// -----------------------------------------------------------------------------------------------------------------------------
				using (TrembleTimerScope _ = new(TrembleTimer.Context.GatherDataAssets))
				{
					numSteps += allScriptableObjectTypes.Count; // Add discovered data assets
					foreach (Type scriptableObjectType in allScriptableObjectTypes)
					{
						string[] paths = AssetDatabase
							.FindAssets($"t:{scriptableObjectType.Name}", new[] { "Assets" })
							.Select(AssetDatabase.GUIDToAssetPath)
							.ToArray();

						foreach (string path in paths)
						{
							string mapName = Path.GetFileNameWithoutExtension(path);

							ReportProgress(ref step, numSteps, mapName);
							assets.Add(new()
							{
								MapName = mapName,
								Path = path,

#if ADDRESSABLES_INSTALLED
								AddressableName = TrembleSyncAddressables.GetExistingEntryForPath(path)?.address ?? mapName,
#endif

								FullTypeName = scriptableObjectType.FullName
							});
						}
					}
				}

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Materials
				// -----------------------------------------------------------------------------------------------------------------------------
				List<string> allMaterialPaths = allMaterialGroups
					.SelectMany(mg => mg.Materials
						.Select(AssetDatabase.GetAssetPath)
						.Where(path => !path.Contains("M_Clip"))
					)
					.ToList();

				using (TrembleTimerScope _ = new(TrembleTimer.Context.ExportMaterials))
				{

					MapTextureSnapshotter textureSnapshotter = new();
					textureSnapshotter.Init(SyncSettings);

					try
					{
						numSteps += prefabMaterialPaths.Count;

						// Material groups
						foreach (string materialPath in allMaterialPaths)
						{
							// Look up material name with group
							if (!materialNameLookup.TryGetMapNameFromMaterialPath(materialPath, out string mapName))
							{
								mapName = Path.GetFileNameWithoutExtension(materialPath);
							}

							string outputPath = Path.Combine(baseMaterialsFolder, mapName + ".png");
							string description = Path.GetFileName(materialPath);

							ReportProgress(ref step, numSteps, description);
							ProcessMaterial(textureSnapshotter, materialPath, mapName, outputPath, modTimeDB, assets);
						}

						// Prefab materials
						foreach (string materialPath in prefabMaterialPaths)
						{
							string mapName = materialNameLookup.GetPrefabNameFromMaterialPath(materialPath);

							string outputPath = Path.Combine(modelsFolder, mapName + ".png");
							string description = Path.GetFileName(materialPath);

							ReportProgress(ref step, numSteps, description);
							ProcessMaterial(textureSnapshotter, materialPath, mapName, outputPath, modTimeDB, assets);
						}
					}
					finally
					{
						textureSnapshotter.DeInit();
					}

					// -----------------------------------------------------------------------------------------------------------------------------
					//		"clip" texture
					// -----------------------------------------------------------------------------------------------------------------------------
					if (SyncSettings.ExportClipSkipTextures)
					{
						string clipPath = Path.Combine(baseMaterialsFolder, "special", "clip.png");
						DirectoryUtil.CreateAllDirectories(clipPath);

						Texture2D clipTexture = TextureUtil.GenerateClipTexture(Color.white, Color.grey, Color.red);
						await File.WriteAllBytesAsync(clipPath, clipTexture.EncodeToPNG());

						string skipPath = Path.Combine(baseMaterialsFolder, "special", "skip.png");
						DirectoryUtil.CreateAllDirectories(clipPath);

						Texture2D skipTexture = TextureUtil.GenerateSkipTexture(Color.white, Color.grey, Color.blue);
						await File.WriteAllBytesAsync(skipPath, skipTexture.EncodeToPNG());
					}

					// -----------------------------------------------------------------------------------------------------------------------------
					//		Missing texture texture
					// -----------------------------------------------------------------------------------------------------------------------------
					{
						string emptyPath = Path.Combine(baseMaterialsFolder, "__TB_empty.png");
						DirectoryUtil.CreateAllDirectories(emptyPath);

						Texture2D emptyTexture = TextureUtil.GenerateColourTexture(Color.magenta);
						await File.WriteAllBytesAsync(emptyPath, emptyTexture.EncodeToPNG());
					}
				}

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Write configs
				// -----------------------------------------------------------------------------------------------------------------------------
				bool wasNewInstall = false;

				using (TrembleTimerScope _ = new(TrembleTimer.Context.WriteEntitiesFgdAndGameConfig))
				{
					WriteAllConfigFiles(fgdWriter, transparentBrushes);
					wasNewInstall = await TrenchBroomUtil.WriteEnginePathIntoTrenchBroomPrefs();
					modTimeDB.WriteToFile();

					if (assets != null && SyncSettings)
					{
						SyncSettings.AssetMetadatas = assets.ToArray();
						EditorUtility.SetDirty(SyncSettings);
						AssetDatabase.SaveAssetIfDirty(SyncSettings);
					}
				}


				// -----------------------------------------------------------------------------------------------------------------------------
				//		Cleanup backups, if wanted
				// -----------------------------------------------------------------------------------------------------------------------------
				using (TrembleTimerScope _ = new(TrembleTimer.Context.Cleanup))
				{
					if (SyncSettings.AutoDeleteMapBackups)
					{
						TrembleMenu.DeleteAutosaves();
					}
				}


				// -----------------------------------------------------------------------------------------------------------------------------
				//		Write Addressables data
				// -----------------------------------------------------------------------------------------------------------------------------
#if ADDRESSABLES_INSTALLED
				if (SyncSettings.AllowStandaloneImport)
				{
					await TrembleSyncAddressables.SetupAddressables(
						SyncSettings,
						materialNameLookup, allMaterialPaths,
						prefabNameLookup, allPrefabPaths,
						allScriptableObjectTypes);
				}
#endif

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Wait for any background tasks
				// -----------------------------------------------------------------------------------------------------------------------------
				await Task.WhenAll(s_BackgroundTasks);
				s_BackgroundTasks.Clear();

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Open settings if newly installed
				// -----------------------------------------------------------------------------------------------------------------------------
				if (wasNewInstall)
				{
					if (TrenchBroomUtil.IsTrenchBroomRunning)
					{
						EditorUtility.DisplayDialog(
							title: "TrenchBroom running!",
							message: "Hey, your new Tremble game was just installed, but TrenchBroom was running. " +
							         "You will need to quit TrenchBroom and re-open it before editing any maps!",
							"Oh, Okay!");
					}

					EditorApplication.delayCall += () =>
					{
						TrembleEditorAPI.SyncToTrenchBroom();
						TrembleEditorAPI.ReimportAllMaps(silent: true);

						try
						{
							Assembly assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
							Type type = assembly.GetType("UnityEditor.LogEntries");
							MethodInfo method = type.GetMethod("Clear");
							method?.Invoke(new object(), null);
						}
						finally
						{
							Debug.ClearDeveloperConsole();
						}
					};
				}
			}
			catch (Exception ex)
			{
				Debug.LogError("Project Sync failed!");
				Debug.LogException(ex);

				ProgressUtil.Fail(s_ProgressTask);
			}
			finally
			{
				ProgressUtil.Succeed(s_ProgressTask);

				string baseq3Old = Path.Combine(Directory.GetCurrentDirectory(), "Library", "baseq3_old");
				if (Directory.Exists(baseq3Old))
				{
					Directory.Delete(baseq3Old, true);
				}

				TrembleTimer.EndSession();
			}
		}

		private static void ReportProgress(ref int step, int numSteps, string assetName)
		{
			ProgressUtil.Report(s_ProgressTask, step++, numSteps, $"{assetName} ({step + 1}/{numSteps})");
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Entity
		// -----------------------------------------------------------------------------------------------------------------------------
		internal static FgdWriter GatherEntities()
		{
			List<Type> allBrushTypes = TypeDatabase.GetTypesWithAttribute<BrushEntityAttribute>().ToList();
			List<Type> allPointTypes = TypeDatabase.GetTypesWithAttribute<PointEntityAttribute>().ToList();
			List<Type> allScriptableObjectTypes = new(128);

			int step = 0;
			int numSteps = allBrushTypes.Count + allPointTypes.Count + allScriptableObjectTypes.Count;

			// These are not duplicates - they're only used from the entities manual page!
			MapTypeLookup mapTypeLookup = new(SyncSettings);
			PrefabNameLookup prefabNameLookup = new(SyncSettings);
			MaterialNameLookup materialNameLookup = new(SyncSettings);
			ModTimeDB modTimeDB = new(TrembleConsts.MODTIME_DB_PATH);

			List<string> prefabMaterialPaths = new(prefabNameLookup.AllPrefabPaths.Count * 2);
			List<string> transparentBrushes = new(allBrushTypes.Count);
			List<AssetMetadata> assets = new(prefabNameLookup.AllPrefabPaths.Count * 2);

			return GatherEntities(ref step, numSteps,
				prefabNameLookup, materialNameLookup, mapTypeLookup, modTimeDB,
				TrembleConsts.BASE_MODELS_PATH, TrembleConsts.BASE_MATERIALS_PATH,
				prefabMaterialPaths, transparentBrushes,
				allBrushTypes, allPointTypes, allScriptableObjectTypes,
				assets);
		}
		private static FgdWriter GatherEntities(ref int step, int numSteps,
			PrefabNameLookup prefabNameLookup, MaterialNameLookup materialNameLookup, MapTypeLookup typeLookup, ModTimeDB modTimeDB,
			string modelsFolder, string baseMaterialsFolder,
			List<string> prefabMaterialPaths, List<string> transparentBrushes,
			List<Type> allBrushTypes, List<Type> allPointTypes, List<Type> allScriptableObjectTypes,
			List<AssetMetadata> assets)
		{
			TrembleFieldConverterCollection fieldConverter = new();

			FgdWriter fgdWriter = new(TrembleConsts.VERSION_STRING, SyncSettings);

			DiscoverAndAddBaseClasses(fgdWriter, fieldConverter, allScriptableObjectTypes);
			{
				// Prefabs
				foreach (string prefabPath in prefabNameLookup.AllPrefabPaths)
				{
					string rawPrefabName = Path.GetFileNameWithoutExtension(prefabPath);
					prefabNameLookup.TryGetMapNameFromPrefabPath(prefabPath, out string prefabName);

					ReportProgress(ref step, numSteps, prefabName);
					ProcessPrefab(prefabName, rawPrefabName, prefabPath, prefabMaterialPaths, modelsFolder, fgdWriter, fieldConverter, materialNameLookup, modTimeDB, allScriptableObjectTypes, assets);
				}

				// Point MonoBehaviours
				foreach (Type pointType in allPointTypes)
				{
					ReportProgress(ref step, numSteps, pointType.Name);
					//ProcessPointEntity(pointType, modelsFolder, fgdWriter, fieldConverter, typeLookup, allScriptableObjectTypes);
					ProcessPointEntityWithPrefabs(pointType, modelsFolder, prefabMaterialPaths, fgdWriter, fieldConverter, typeLookup, materialNameLookup, modTimeDB, allScriptableObjectTypes, assets);
				}

				// Brush MonoBehaviours
				foreach (Type brushType in allBrushTypes)
				{
					ReportProgress(ref step, numSteps, brushType.Name);
					ProcessBrushEntity(brushType, transparentBrushes, baseMaterialsFolder, fgdWriter, fieldConverter, typeLookup, allScriptableObjectTypes);
				}
			}

			// Export worldspawn data
			Type worldspawnScriptType = SyncSettings.WorldspawnScript?.Class ?? typeof(Worldspawn);

			FgdClass worldspawn = new(FgdClassType.Brush, "worldspawn", "The root Worldspawn entity class");
			{
				AddExposedFieldsToEntity(worldspawnScriptType, worldspawn, fieldConverter, allScriptableObjectTypes);
			}
			fgdWriter.WorldspawnClass = worldspawn;

			return fgdWriter;
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Processing
		// -----------------------------------------------------------------------------------------------------------------------------
		private static bool ProcessMaterial(MapTextureSnapshotter snapshotter, string materialPath, string mapName, string outputPath, ModTimeDB modTimeDB, List<AssetMetadata> assets)
		{
			if (materialPath.IsNullOrEmpty())
				return false;

			Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
			if (!material)
			{
				//Debug.LogError($"Material {materialPath} not found!");
				return false;
			}

			try
			{
				ulong lastModified = modTimeDB.GetFileModifiedTime(material);
				ulong nowModified = ModTimeDB.CalcMaterialModifiedTime(material);
				modTimeDB.SetFileModifiedTime(material, nowModified);

				// Store metadata about material
				assets.Add(new()
				{
					MapName = mapName,
					Path = materialPath,
					FullTypeName = typeof(Material).FullName,

#if ADDRESSABLES_INSTALLED
					AddressableName = TrembleSyncAddressables.GetExistingEntryForPath(materialPath)?.address ?? material.name,
#endif
				});

				MaterialRenderData data = SyncSettings.GetMaterialRenderData(material);

				if (nowModified <= lastModified && !data.IsDirty && TryCopyLastVersion(outputPath))
				{
					// No modifications, old value copied!
					return true;
				}

				DirectoryUtil.CreateAllDirectories(outputPath);
				snapshotter.SnapshotMaterial(material, outputPath);

				if (data != null)
				{
					data.ClearDirty();
					EditorUtility.SetDirty(SyncSettings);
				}
			}
			catch (Exception ex)
			{
				Debug.LogError(ex);
				return false;
			}

			return true;
		}


		private static bool ProcessPrefab(string prefabName, string unityPrefabName, string prefabPath, List<string> materialsList, string modelsFolder, FgdWriter fgdWriter, TrembleFieldConverterCollection fieldConverter, MaterialNameLookup materialNameLookup, ModTimeDB modTimeDB, List<Type> scriptableObjectTypes, List<AssetMetadata> assets)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
			string modelPath = Path.Combine(modelsFolder, $"{prefabName}.md3");

			// Check if we have an up-to-date MD3 for this prefab already
			bool dontCreateMD3 = false;

			ulong lastModified = modTimeDB.GetFileModifiedTime(prefab);
			ulong nowModified = ModTimeDB.CalcModelModifiedTime(prefab);
			modTimeDB.SetFileModifiedTime(prefab, nowModified);

			if (nowModified <= lastModified && TryCopyLastVersion(modelPath))
			{
				dontCreateMD3 = true;
			}

			foreach (MonoBehaviour mb in prefab.GetComponents<MonoBehaviour>())
			{
				if (!mb)
				{
					Debug.LogWarning($"Prefab {prefab.name} has a broken/missing MonoBehaviour!");
					continue;
				}

				Type prefabType = mb.GetType();

				if (!prefabType.TryGetCustomAttribute(out PrefabEntityAttribute pea))
					continue;

				if (!pea.IsPrefabIncluded(unityPrefabName))
					continue;

				// Sanity check position + rotation
				if (prefab.transform.position.sqrMagnitude > 1f)
				{
					Debug.LogWarning($"WARNING: Prefab {prefab.name} has an position offset of {prefab.transform.position}. This is probably unintended and will affect how your prefab appears in TrenchBroom! Set the prefab's position to (0, 0, 0) for best results.", prefab);
				}

				if (prefab.transform.rotation.eulerAngles.magnitude > 1f)
				{
					Debug.LogWarning($"WARNING: Prefab {prefab.name} has a rotation of {prefab.transform.rotation.eulerAngles}. This is probably unintended and will affect how your prefab appears in TrenchBroom! Set the prefab's rotation to (0, 0, 0) for best results.", prefab);
				}

				// Write materials used by prefab into material list
				FindAndAddMaterialsUsedByPrefab(prefab, materialsList);

				// Write MD3 or use existing metadata
				bool hasModel = dontCreateMD3
					? MD3Util.HasModel(prefab, out Bounds modelBounds)
					: MD3Util.SaveMD3(prefab, modelPath, materialNameLookup, SyncSettings.ImportScale, out modelBounds);

				string overrideSprite = pea.OverrideSprite;
				if (mb is TrembleSpawnablePrefab tsp && tsp.OverrideSprite != null)
				{
					overrideSprite = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(tsp.OverrideSprite));
				}

				if (overrideSprite != null)
				{
					hasModel = false;

					string srcPngPath = TrembleAssetLoader.FindAssetPath<Texture2D>(overrideSprite);
					string destPngPath = Path.Combine(modelsFolder, $"{overrideSprite}.png");
					File.Copy(srcPngPath, destPngPath, true);
				}

				string prefabTypeDescription = prefabType == typeof(TrembleSpawnablePrefab)
					? $"Tremble Spawnable Prefab '{unityPrefabName}'"
					: $"[PrefabEntity] class {prefabType.Name}, from prefab '{Path.GetFileNameWithoutExtension(prefabPath)}'";

				FgdClass entityClass = new(FgdClassType.Point, prefabName, prefabTypeDescription)
				{
					HasModel = hasModel,
					Sprite = overrideSprite,
					Box = hasModel ? modelBounds : null,
				};

				entityClass.AddBaseClass(FgdConsts.CLASS_MAP_BASE);
				entityClass.AddBaseClass(FgdConsts.CLASS_MAP_PREFAB_BASE);
				entityClass.AddBaseClassInterfaces(prefabType);
				AddExposedFieldsToEntity(prefabType, entityClass, fieldConverter, scriptableObjectTypes, prefab.GetComponent(prefabType));

				fgdWriter.AddClass(entityClass);

				// Store metadata about prefab
				assets.Add(new()
				{
					MapName = prefabName,
					Path = prefabPath,
					FullTypeName = typeof(GameObject).FullName,

#if ADDRESSABLES_INSTALLED
					AddressableName = TrembleSyncAddressables.GetExistingEntryForPath(prefabPath)?.address ?? unityPrefabName,
#endif

					SpawnOffset = MD3Util.GetPrefabUnitySpawnOffset(prefab, SyncSettings.ImportScale)
				});

				return true;
			}

			return false;
		}

		private static bool TryCopyLastVersion(string outputFile)
		{
			string oldVersion = outputFile.Replace("baseq3", "baseq3_old");
			if (!File.Exists(oldVersion))
				return false;

			ContinueInBackground(() =>
			{
				DirectoryUtil.CreateAllDirectories(outputFile);
				try
				{
					File.Copy(oldVersion, outputFile, true);
				}
				catch (IOException)
				{
					Debug.LogError($"Failed to copy old version of '{Path.GetFileName(outputFile)}'");
				}
			});

			return true;
		}

		private static bool ProcessPointEntity(Type pointType, string modelsFolder, FgdWriter fgdWriter, TrembleFieldConverterCollection fieldConverter, MapTypeLookup typeLookup, List<Type> scriptableObjectTypes)
		{
			if (!pointType.IsSubclassOf(typeof(MonoBehaviour)))
			{
				Debug.LogError($"[PointEntity] {pointType.Name} is NOT a MonoBehaviour or derived from a type that is a MonoBehaviour. This won't work! Your class should appear as e.g. public class {pointType.Name} : MonoBehaviour");
				return false;
			}

			typeLookup.TryGetMapNameFromClass(pointType, out string name);
			pointType.TryGetCustomAttribute(out PointEntityAttribute pea);

			float size = pea.Size;

			// Write sprite, if any
			if (!pea.Sprite.IsNullOrEmpty())
			{
				string spritePath = TrembleAssetLoader.FindAssetPath<Texture2D>(pea.Sprite);
				if (spritePath.IsNullOrEmpty())
				{
					Debug.LogWarning($"Couldn't find sprite '{pea.Sprite}' for Point Entity '{pointType.Name}'!");
				}
				else
				{
					Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
					if (texture)
					{
						size = (texture.width * SyncSettings.ImportScale) + 0.2f;
					}

					string spriteFilename = Path.GetFileName(spritePath);
					string outputPath = Path.Combine(modelsFolder, spriteFilename);

					ContinueInBackground(() => File.Copy(spritePath, outputPath, true));
				}
			}

			FgdClass entityClass = new(FgdClassType.Point, name, $"[PointEntity] class {pointType.Name} ('{name}')")
			{
				Colour = GetColor(pointType, pea.Colour),
				Box = new Bounds(Vector3.zero, Vector3.one * size),
				Sprite = pea.Sprite
			};

			entityClass.AddBaseClass(FgdConsts.CLASS_MAP_BASE);
			entityClass.AddBaseClass(FgdConsts.CLASS_MAP_POINT_BASE);
			entityClass.AddBaseClassInterfaces(pointType);
			AddExposedFieldsToEntity(pointType, entityClass, fieldConverter, scriptableObjectTypes);
			fgdWriter.AddClass(entityClass);

			return true;
		}
		
		private static bool ProcessPointEntityWithPrefabs(Type pointType, string modelsFolder, List<string> materialsList, FgdWriter fgdWriter, TrembleFieldConverterCollection fieldConverter, MapTypeLookup typeLookup, MaterialNameLookup materialNameLookup, ModTimeDB modTimeDB, List<Type> scriptableObjectTypes, List<AssetMetadata> assets)
		{
			if (!pointType.IsSubclassOf(typeof(MonoBehaviour)))
			{
				Debug.LogError($"[PointEntity] {pointType.Name} is NOT a MonoBehaviour or derived from a type that is a MonoBehaviour. This won't work! Your class should appear as e.g. public class {pointType.Name} : MonoBehaviour");
				return false;
			}

			typeLookup.TryGetMapNameFromClass(pointType, out string name);
			pointType.TryGetCustomAttribute(out PointEntityAttribute pea);

			float size = pea.Size;

			// Write sprite, if any
			if (!pea.Sprite.IsNullOrEmpty())
			{
				string spritePath = TrembleAssetLoader.FindAssetPath<Texture2D>(pea.Sprite);
				if (spritePath.IsNullOrEmpty())
				{
					Debug.LogWarning($"Couldn't find sprite '{pea.Sprite}' for Point Entity '{pointType.Name}'!");
				}
				else
				{
					Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spritePath);
					if (texture)
					{
						size = (texture.width * SyncSettings.ImportScale) + 0.2f;
					}

					string spriteFilename = Path.GetFileName(spritePath);
					string outputPath = Path.Combine(modelsFolder, spriteFilename);

					ContinueInBackground(() => File.Copy(spritePath, outputPath, true));
				}
			}

			Bounds modelBounds = new Bounds();
			bool hasPrefab = false;
			
			if (!pea.Prefab.IsNullOrEmpty())
			{
				string prefabPath = TrembleAssetLoader.FindAssetPath<GameObject>(pea.Prefab);
				if (prefabPath.IsNullOrEmpty())
				{
					Debug.LogWarning($"Couldn't find prefab '{pea.Prefab}' for Point Entity '{pointType.Name}'!");
				}
				else
				{
					GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
					string prefabName = Path.GetFileNameWithoutExtension(prefabPath);
					prefabName = name;
					string modelPath = Path.Combine(modelsFolder, $"{prefabName}.md3");

					// Check if we have an up-to-date MD3 for this prefab already
					bool dontCreateMD3 = false;

					ulong lastModified = modTimeDB.GetFileModifiedTime(prefab);
					ulong nowModified = ModTimeDB.CalcModelModifiedTime(prefab);
					modTimeDB.SetFileModifiedTime(prefab, nowModified);

					if (nowModified <= lastModified && TryCopyLastVersion(modelPath))
					{
						dontCreateMD3 = true;
					}
					
					// Sanity check position + rotation
					if (prefab.transform.position.sqrMagnitude > 1f)
					{
						Debug.LogWarning(
							$"WARNING: Prefab {prefab.name} has an position offset of {prefab.transform.position}. This is probably unintended and will affect how your prefab appears in TrenchBroom! Set the prefab's position to (0, 0, 0) for best results.",
							prefab);
					}

					if (prefab.transform.rotation.eulerAngles.magnitude > 1f)
					{
						Debug.LogWarning(
							$"WARNING: Prefab {prefab.name} has a rotation of {prefab.transform.rotation.eulerAngles}. This is probably unintended and will affect how your prefab appears in TrenchBroom! Set the prefab's rotation to (0, 0, 0) for best results.",
							prefab);
					}

					// Write materials used by prefab into material list
					FindAndAddMaterialsUsedByPrefab(prefab, materialsList);

					// Write MD3 or use existing metadata
					bool hasModel = dontCreateMD3
						? MD3Util.HasModel(prefab, out modelBounds)
						: MD3Util.SaveMD3(prefab, modelPath, materialNameLookup, SyncSettings.ImportScale,
							out modelBounds);

					// Store metadata about prefab
					assets.Add(new()
					{
						MapName = prefabName,
						Path = prefabPath,
						FullTypeName = typeof(GameObject).FullName,

#if ADDRESSABLES_INSTALLED
					AddressableName = TrembleSyncAddressables.GetExistingEntryForPath(prefabPath)?.address ?? prefabName,
#endif
						SpawnOffset = MD3Util.GetPrefabUnitySpawnOffset(prefab, SyncSettings.ImportScale)
					});


					hasPrefab = true;
					
					PrefabUtility.UnloadPrefabContents(prefab);
				}
			}
			
			FgdClass entityClass = new(FgdClassType.Point, name, $"[PointEntity] class {pointType.Name} ('{name}')")
			{
				HasModel = hasPrefab,
				Colour = GetColor(pointType, pea.Colour),
				Box = hasPrefab ? modelBounds : new Bounds(Vector3.zero, Vector3.one * size),
				Sprite = hasPrefab ? null : pea.Sprite,
			};

			entityClass.AddBaseClass(FgdConsts.CLASS_MAP_BASE);
			entityClass.AddBaseClass(FgdConsts.CLASS_MAP_POINT_BASE);
			entityClass.AddBaseClassInterfaces(pointType);
			AddExposedFieldsToEntity(pointType, entityClass, fieldConverter, scriptableObjectTypes);
			fgdWriter.AddClass(entityClass);
			
			return true;
		}

		private static bool ProcessBrushEntity(Type brushType, List<string> transparentBrushes, string baseMaterialsFolder, FgdWriter fgdWriter, TrembleFieldConverterCollection fieldConverter, MapTypeLookup typeLookup, List<Type> scriptableObjectTypes)
		{
			if (!brushType.IsSubclassOf(typeof(MonoBehaviour)))
			{
				Debug.LogError($"[BrushEntity] {brushType.Name} is NOT a MonoBehaviour or derived from a type that is a MonoBehaviour. This won't work! Your class should appear as e.g. public class {brushType.Name} : MonoBehaviour");
				return false;
			}

			typeLookup.TryGetMapNameFromClass(brushType, out string name);
			brushType.TryGetCustomAttribute(out BrushEntityAttribute bea);

			// Add to transparent brushes if needed
			if (bea.BrushType is BrushType.Invisible or BrushType.Trigger)
			{
				transparentBrushes.Add(name);
			}

			// Write texture, if present
			if (bea.Colour != null)
			{
				string specialPath = Path.Combine(baseMaterialsFolder, "special", $"{name}.png");
				DirectoryUtil.CreateAllDirectories(specialPath);

				if (bea.CheckerStyle == CheckerboardStyle.None)
				{
					Texture2D specialTexture = TextureUtil.GenerateColourTexture(bea.Colour.Value);
					File.WriteAllBytes(specialPath, specialTexture.EncodeToPNG());
				}
				else
				{
					bool useDarkCheckerboard = bea.CheckerStyle == CheckerboardStyle.Dark;
					Color checkerColour = useDarkCheckerboard ? Color.black : Color.white;

					Texture2D specialTexture = TextureUtil.GenerateCheckerTexture(bea.Colour.Value, checkerColour);
					File.WriteAllBytes(specialPath, specialTexture.EncodeToPNG());
				}
			}

			FgdClass entityClass = new(FgdClassType.Brush, name, $"[BrushEntity] class {brushType.Name} ('{name}')");
			entityClass.AddBaseClass(FgdConsts.CLASS_MAP_BASE);
			entityClass.AddBaseClassInterfaces(brushType);
			AddExposedFieldsToEntity(brushType, entityClass, fieldConverter, scriptableObjectTypes);
			fgdWriter.AddClass(entityClass);

			return true;
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Helpers
		// -----------------------------------------------------------------------------------------------------------------------------
		private static void FindAndAddMaterialsUsedByPrefab(GameObject prefab, List<string> intoList)
		{
			foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>())
			{
				if (renderer is not MeshRenderer and not SkinnedMeshRenderer)
					continue;

				IEnumerable<string> usedMaterials = renderer.sharedMaterials
					.Select(AssetDatabase.GetAssetPath)
					.Where(p => !p.IsNullOrEmpty());

				foreach (string usedMaterial in usedMaterials)
				{
					if (intoList.Contains(usedMaterial))
						continue;

					intoList.Add(usedMaterial);

				}
			}
		}

		private static Component CreateComponentOnTesterObject(Type scriptType)
		{
			GameObject tester = new("Tester");

			foreach (RequireComponent requiredComponent in scriptType.GetCustomAttributes<RequireComponent>())
			{
				MaybeAddComponent(requiredComponent.m_Type0);
				MaybeAddComponent(requiredComponent.m_Type1);
				MaybeAddComponent(requiredComponent.m_Type2);
			}

			Component testerScript = tester.AddComponent(scriptType);
			return testerScript;

			void MaybeAddComponent(Type type)
			{
				// Null, no need to add!
				if (type == null)
					return;

				// Collider - add a box collider
				if (type == typeof(Collider))
					type = typeof(BoxCollider);

				tester.AddComponent(type);
			}
		}

		internal static void AddExposedFieldsToEntity(Type scriptType, FgdClass entityClass, TrembleFieldConverterCollection fieldConverter, List<Type> scriptableObjectTypes, Component testerComponent = null)
		{
			bool addComments = SyncSettings.FgdFormattingStyle == FgdFormattingStyle.VeryVerbose;
			bool alsoHandleSerializedFields = SyncSettings.SyncSerializedFields;

			// Create a "tester" - an instance to read the default values, add any pre-requisites
			Component testerScript = testerComponent ?? CreateComponentOnTesterObject(scriptType);

			foreach (FieldInfo target in scriptType.GetAllInstanceFieldsIncludingPrivateAndBaseClasses())
			{
				target.GetCustomAttributes(out TrembleAttribute ta, out NoTrembleAttribute nta, out SerializeField sfa);

				string overrideName = ta?.OverrideName;

				if (ta == null && !(sfa != null && alsoHandleSerializedFields) && !target.IsPublic)
				{
					// If not public, and not [MapSerialized], skip!
					if (addComments)
					{
						entityClass.AddComment($"Skipped {target.FieldType.Name} {target.Name} - missing [Tremble] or [SerializedField]!");
					}
					continue;
				}

				if (nta != null)
				{
					// Specifically NOT serialised in maps - skip!
					if (addComments)
					{
						entityClass.AddComment($"Skipped {target.FieldType.Name} {target.Name} - saw [NoTremble].");
					}
					continue;
				}

				// Special case: Prefabs and Materials
				bool isMaterialOrPrefabField = target.FieldType == typeof(Material) || target.FieldType == typeof(GameObject);
				if (isMaterialOrPrefabField && ta == null && !SyncSettings.SyncMaterialsAndPrefabs)
				{
					// This is a Material or Prefab but we've turned off the setting
					if (addComments)
					{
						entityClass.AddComment($"Skipped {target.FieldType.Name} {target.Name} - Material or Prefab, but the SyncMaterialsAndPrefabs setting is off.");
					}
					continue;
				}

				string fieldName = overrideName ?? target.Name.GetFieldNameInMap(SyncSettings.FieldNamingConvention);
				TrembleFieldConverter converter = fieldConverter.GetConverterForType(target.FieldType);

				if (target.FieldType.IsSubclassOf(typeof(ScriptableObject)))
				{
					scriptableObjectTypes.Add(target.FieldType);
				}

				if (converter != null)
				{
					// Use a converter
					object defaultValue = target.GetValue(testerScript);
					if (defaultValue is Array defaultArray)
					{
						defaultValue = defaultArray.Length > 0 ? defaultArray.GetValue(0) : null;
					}
					converter.AddFieldToFgd(entityClass, fieldName, defaultValue, target);
				}
				else if (ta != null)
				{
					// Nothing worked - warning!
					if (addComments)
					{
						entityClass.AddComment($"Skipped {target.FieldType.Name} {target.Name} - no field converter for a {target.FieldType}!");
					}

					Debug.LogWarning($"Saw field '{target.Name}' of type {target.FieldType}, which is not yet supported. Consider creating a TrembleFieldConverter for it!");
				}
			}

			if (testerScript != testerComponent)
			{
				GameObject.DestroyImmediate(testerScript.gameObject);
			}
		}

		private static void DiscoverAndAddBaseClasses(FgdWriter fgd, TrembleFieldConverterCollection fieldConverter, List<Type> scriptableObjectTypes)
		{
			TypeCache.TypeCollection types = TypeCache.GetTypesDerivedFrom<ITrembleBaseClass>();
			foreach (Type baseType in types)
			{
				if (!baseType.IsInterface)
					continue;

				string baseClassName = baseType.Name.Substring(1); // Remove 'I'
				string baseClassDesc = $"Base Class from C# Interface '{baseType.Name}'";
				FgdClass baseClass = new(FgdClassType.Base, baseClassName, baseClassDesc);

				foreach (PropertyInfo target in baseType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
				{
					target.GetCustomAttributes(out TrembleAttribute ta, out NoTrembleAttribute nta);

					string overrideName = ta?.OverrideName;

					if (nta != null)
					{
						// Specifically NOT serialised in maps - skip!
						continue;
					}

					// Special case: Prefabs and Materials
					bool isMaterialOrPrefabField = target.PropertyType == typeof(Material) || target.PropertyType == typeof(GameObject);
					if (isMaterialOrPrefabField && ta == null && !SyncSettings.SyncMaterialsAndPrefabs)
					{
						// This is a Material or Prefab but we've turned off the setting
						continue;
					}

					ITrembleBaseClass defaultBaseType = TrembleBaseClassDefault.CreateDefault(baseType);
					string fieldName = overrideName ?? target.Name.GetFieldNameInMap(SyncSettings.FieldNamingConvention);
					TrembleFieldConverter converter = fieldConverter.GetConverterForType(target.PropertyType);

					if (target.PropertyType.IsSubclassOf(typeof(ScriptableObject)))
					{
						scriptableObjectTypes.Add(target.PropertyType);
					}

					if (converter != null)
					{
						// Use a converter
						converter.AddFieldToFgd(baseClass, fieldName, target.GetValue(defaultBaseType), target);
					}
					else if (ta != null)
					{
						// Nothing worked - warning!
						Debug.LogWarning($"Saw field '{target.Name}' of type {target.PropertyType}, which is not yet supported. Consider creating a TrembleFieldConverter for it!");
					}
				}

				fgd.AddClass(baseClass);
			}
		}

		// Continue a task on a worker thread. Don't touch Unity API in these callbacks!!
		// These are awaited right at the end of the process.
		private static void ContinueInBackground(Action action)
		{
			s_BackgroundTasks.Add(Task.Run(action));
		}

		private static void WriteAllConfigFiles(FgdWriter fgd, List<string> invisibleBrushes)
		{
			// Write Files
			string gameFolder = TrenchBroomUtil.GetGameFolder();
			DirectoryUtil.EmptyAndCreateDirectory(gameFolder);

			string gameConfigPath = Path.Combine(gameFolder, "gameconfig.cfg");
			string entitiesPath = Path.Combine(gameFolder, "entities.fgd");
			string iconPath = Path.Combine(gameFolder, "Icon.png");

			// Write entities.fgd
			using FileStream entityFile = new(entitiesPath, FileMode.Create, FileAccess.Write);
			using StreamWriter sw = new(entityFile);
			fgd.WriteFgd(sw, SyncSettings.FgdFormattingStyle is FgdFormattingStyle.HumanReadable or FgdFormattingStyle.VeryVerbose);

			// Write gameconfig.cfg
			string version = SyncSettings.TrenchBroomGameConfigVersion;

			StringBuilder brushes = new();
			
			brushes.AppendLine("{");
			brushes.AppendLine($"\t\t\t\t\"name\": \"Func\",");
			brushes.AppendLine($"\t\t\t\t\"attribs\": [],");
			brushes.AppendLine($"\t\t\t\t\"match\": \"classname\",");
			brushes.AppendLine($"\t\t\t\t\"pattern\": \"func*\"");
			brushes.Append("\t\t\t}");
			
			brushes.AppendLine(",");
			brushes.Append("\t\t\t");
			
			brushes.AppendLine("{");
			brushes.AppendLine($"\t\t\t\t\"name\": \"Trigger\",");
			brushes.AppendLine($"\t\t\t\t\"attribs\": [ \"transparent\" ],");
			brushes.AppendLine($"\t\t\t\t\"match\": \"classname\",");
			brushes.AppendLine($"\t\t\t\t\"pattern\": \"trigger*\"");
			brushes.Append("\t\t\t}");
			
			brushes.AppendLine(",");
			brushes.Append("\t\t\t");
			
			brushes.AppendLine("{");
			brushes.AppendLine($"\t\t\t\t\"name\": \"Cloud\",");
			brushes.AppendLine($"\t\t\t\t\"attribs\": [ ],");
			brushes.AppendLine($"\t\t\t\t\"match\": \"classname\",");
			brushes.AppendLine($"\t\t\t\t\"pattern\": \"misc_cloud\"");
			brushes.Append("\t\t\t}");
			
			brushes.AppendLine(",");
			brushes.Append("\t\t\t");
			
			brushes.AppendLine("{");
			brushes.AppendLine($"\t\t\t\t\"name\": \"Separate\",");
			brushes.AppendLine($"\t\t\t\t\"attribs\": [ ],");
			brushes.AppendLine($"\t\t\t\t\"match\": \"classname\",");
			brushes.AppendLine($"\t\t\t\t\"pattern\": \"misc_separate\"");
			brushes.Append("\t\t\t}");

			
			for (int brushIdx = 0; brushIdx < invisibleBrushes.Count; brushIdx++)
			{
				string invisibleBrush = invisibleBrushes[brushIdx];

				if (brushIdx > 0 || true)
				{
					brushes.AppendLine(",");
					brushes.Append("\t\t\t");
				}

				brushes.AppendLine("{");
				brushes.AppendLine($"\t\t\t\t\"name\": \"{invisibleBrush}\",");
				brushes.AppendLine($"\t\t\t\t\"attribs\": [ \"transparent\" ],");
				brushes.AppendLine($"\t\t\t\t\"match\": \"classname\",");
				brushes.AppendLine($"\t\t\t\t\"pattern\": \"{invisibleBrush}\",");
				brushes.AppendLine($"\t\t\t\t\"texture\": \"special/{invisibleBrush}\"");
				brushes.Append("\t\t\t}");
			}

			string gameConfigFilename = $"gameconfig-{SyncSettings.TrenchBroomGameConfigVersion}.cfg";
			string gameConfigTemplatePath = Path.Combine(TrembleConsts.EDITOR_GetTrembleInstallFolder(), "Editor", "Templates", gameConfigFilename);
			string gameConfigTemplate = File.ReadAllText(gameConfigTemplatePath);
			string gameConfig = gameConfigTemplate
				.Replace("$GAME_NAME", TrembleConsts.GAME_NAME)
				.Replace("$BRUSHES", brushes.ToString());
			File.WriteAllText(gameConfigPath, gameConfig);
				
			// Copy Icon.png
			bool copiedIcon = false;
			Texture2D[] icons = PlayerSettings.GetIcons(NamedBuildTarget.Standalone, IconKind.Any);
			for (int iconIdx = icons.Length - 1; iconIdx >= 0; iconIdx--)
			{
				if (!icons[iconIdx])
					continue;

				string sourceIconPath = Path.GetFullPath(AssetDatabase.GetAssetPath(icons[iconIdx]));

				ContinueInBackground(() =>
				{
					File.Copy(sourceIconPath, iconPath, true);
					File.SetAttributes(iconPath, FileAttributes.Normal);
				});

				copiedIcon = true;
				break;
			}

			if (!copiedIcon)
			{
				// No Icon!
			}
		}

		private static void UpgradeMaterialExportSettingsIfRequired()
		{
			// Older versions of Tremble encode this as an int, so we need to check that first
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			MaterialExportSize mes = syncSettings.MaterialExportSize;

			if ((int)mes <= 8)
				return;

			int oldSize = (int)mes;

			bool hasResolvedSize = false;
			for (int sizeIdx = 0; sizeIdx < MaterialExportSizeUtils.Names.Length; sizeIdx++)
			{
				int newSize = int.Parse(MaterialExportSizeUtils.Names[sizeIdx]);

				if (oldSize > newSize)
					continue;

				mes = (MaterialExportSize)sizeIdx;
				hasResolvedSize = true;
				break;
			}

			if (!hasResolvedSize)
			{
				// No matching size found - use default
				mes = MaterialExportSizeUtils.GetDefault();
			}

			EditorUtility.DisplayDialog(
				title: "Tremble Material Upgrade",
				message: $"This new version of Tremble handles texture sizes differently. Your old size of {oldSize} has " +
				         $"been mapped to {mes}. Please verify this size is what you were expecting.",
				ok: "Thanks!");

			SerializedObject settingsObj = new(syncSettings);
			SerializedProperty sizeProp = settingsObj.FindBackedProperty(nameof(syncSettings.MaterialExportSize));
			sizeProp.enumValueIndex = (int)mes;
			settingsObj.ApplyModifiedPropertiesWithoutUndo();

			EditorUtility.SetDirty(syncSettings);
			AssetDatabase.SaveAssets();

			TrembleEditorAPI.InvalidateMaterialAndPrefabCache(silent: true);
			TrembleEditorAPI.SyncToTrenchBroom();
		}

		private static TrembleColorData _cachedColorData;
		
		private static Color GetColor(Type type, Color? attColour)
		{
			if (attColour.HasValue)
				return attColour.Value;
			
			if (!_cachedColorData)
				_cachedColorData =
					TrembleAssetLoader.LoadAssetByPath<TrembleColorData>(TrembleAssetLoader
						.FindAssetPath<TrembleColorData>());

			if (!_cachedColorData)
				return Color.white;

			foreach (TrembleColorData.DataPair pair in _cachedColorData.pairs)
			{
				if (pair.Type.Type == type)
					return pair.Color;
			}
			
			return Color.white;
		}
	}
}