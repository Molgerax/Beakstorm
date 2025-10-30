//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
// This class based on BSP Map Tools for Unity by John Evans (evans3d512@gmail.com)
//

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	public static class TBConsts
	{
		public const string TB_PREFIX = "_tb_";

		public const string ID = TB_PREFIX + "id";
		public const string NAME = TB_PREFIX + "name";
		public const string OMIT_FROM_EXPORT = TB_PREFIX + "layer_omit_from_export";

		public const string GROUP = TB_PREFIX + "group";
		public const string LAYER = TB_PREFIX + "layer";
		public const string TYPE = TB_PREFIX + "type";
		public const string TEXTURES = TB_PREFIX + "textures";

		public const string EMPTY_TEXTURE = "__TB_empty";
		public const string CLIP_TEXTURE = "clip";
		public const string SKIP_TEXTURE = "skip";
		public const string TRIGGER_TEXTURE = "trigger";
		public const string SPECIAL_TEXTURE = "special";
	}

	public class TrembleMapImportSettings
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Statics
		// -----------------------------------------------------------------------------------------------------------------------------
		private static TrembleMapImportSettings s_CurrentImportSettings;
		internal static void INTERNAL_SetCurrent(TrembleMapImportSettings settings) => s_CurrentImportSettings = settings;

		public static TrembleMapImportSettings Current
		{
			get
			{
				if (s_CurrentImportSettings == null)
				{
					Debug.LogError("Cannot get TrembleMapImportSettings when no import is in progress!");
				}
				return s_CurrentImportSettings;
			}
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private MapProcessorBase[] m_MapProcessors;
		private readonly Dictionary<string, List<GameObject>> m_EntityIDToGameObjects = new();
		private readonly Dictionary<int, GameObject> m_SerialNumberToGameObject = new();

		public MapProcessorClass[] ProcessorClasses { private get; init; }

		public bool SplitMesh { get; init; } = true;
		public float MaxMeshSurfaceArea { get; init; } = 3000;
		public float SmoothingAngle { get; init; } = 45;
		public string ExtraCommandLineArgs { get; init; } = "";

		public string AssetGUID { get; init; } = null;

		public Action<string, Object> OnObjectAdded { private get; init; } = null;
		public Action<Object> OnDependencyAdded { get; init; }
		public Action<GameObject, Type> OnEntityAdded { get; init; } = null;
		public Action<string[]> OnWadsFound { get; init; } = null;
		public Action<string> OnWarning { private get; init; } = null;
		public Action<string> OnError { private get; init; } = null;
		public Action<Q3Map2Result, List<string>> OnQ3Map2Result { get; init; } = null;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Getters
		// -----------------------------------------------------------------------------------------------------------------------------
		public static TrembleMapImportSettings Default => new();

		public MapProcessorBase[] Processors
		{
			get
			{
				if (m_MapProcessors == null)
				{
					m_MapProcessors = ProcessorClasses == null
						? Array.Empty<MapProcessorBase>()
						: ProcessorClasses
							.Where(pc => pc is { IsValid: true })
							.Select(pc => pc.CreateInstance())
							.ToArray();
				}

				return m_MapProcessors;
			}
		}

		public Dictionary<string, List<GameObject>> AllEntityIDs => m_EntityIDToGameObjects;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Public API
		// -----------------------------------------------------------------------------------------------------------------------------
		public void MapGameObjectToEntity(GameObject obj, BspEntity entity)
		{
			m_SerialNumberToGameObject[entity.SerialNumber] = obj;

			if (entity.HasID())
			{
				if (m_EntityIDToGameObjects.TryGetValue(entity.GetID(), out List<GameObject> list))
				{
					list.Add(obj);
				}
				else
				{
					m_EntityIDToGameObjects.Add(entity.GetID(), new() { obj });
				}
			}
		}

		public bool TryGetGameObjectsForID(string id, out List<GameObject> objs)
		{
			if (id == null)
			{
				objs = null;
				return false;
			}
			return m_EntityIDToGameObjects.TryGetValue(id, out objs);
		}

		public bool TryGetGameObjectForEntity(BspEntity entity, out GameObject obj) => m_SerialNumberToGameObject.TryGetValue(entity.SerialNumber, out obj);
		public bool TryGetComponentForEntity<TComponent>(BspEntity entity, out TComponent component)
			where TComponent : Component
		{
			if (!m_SerialNumberToGameObject.TryGetValue(entity.SerialNumber, out GameObject obj)
			    || !obj.TryGetComponent(out component))
			{
				component = default;
				return false;
			}

			return true;
		}


		public void Warn(string warning)
		{
			if (OnWarning != null)
				OnWarning.Invoke(warning);
			else
				Debug.LogWarning(warning);
		}

		public void Error(string error)
		{
			if (OnError != null)
				OnError.Invoke(error);
			else
				Debug.LogError(error);
		}

		public void AddDependency(Object obj)
		{
			if (!obj)
				return;

			OnDependencyAdded?.Invoke(obj);
		}

		public void SaveObjectInMap(string meshName, Object obj) => OnObjectAdded?.Invoke(meshName, obj);
	}

	public class TrembleMapImport
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly TrembleMapImportSettings m_ImportSettings;
		private TrembleSyncSettings m_SyncSettings;

		private static readonly Dictionary<string, Material> s_MaterialCache = new();

		private MeshBuilder m_MeshBuilder;
		private int m_GroupCounter;

		// Groups
		private Transform[] m_GroupTransforms;

		private MapTypeLookup m_MapTypeLookup;
		private PrefabNameLookup m_PrefabNameLookup;
		private MaterialNameLookup m_MaterialNameLookup;
		private TrembleFieldConverterCollection m_FieldConverter;

		public TrembleMapImport(TrembleMapImportSettings importSettings)
		{
			m_ImportSettings = importSettings;
		}

		private MapDocument CreateFailedMap(Q3Map2Result result, string reason)
		{
			GameObject nullObj = new($"Map: {result}");
			MapDocument doc = nullObj.AddComponent<MapDocument>();
			doc.INTERNAL_SetResult(result, reason);
			return doc;
		}

		public async Task<MapDocument> ImportMapAndCreatePrefab(string mapFilePath, MapBsp existingMapBsp = null)
		{
			if (mapFilePath == null || !File.Exists(mapFilePath))
				return CreateFailedMap(Q3Map2Result.Failed, $"Map file path '{mapFilePath}' was NULL or does not exist!");

			TrembleMapImportSettings.INTERNAL_SetCurrent(m_ImportSettings);
			m_SyncSettings = TrembleSyncSettings.Get();

			int task = ProgressUtil.Start($"Import {Path.GetFileName(mapFilePath)}");

			// -----------------------------------------------------------------------------------------------------------------------------
			//		Set up build folder, and discover groups
			// -----------------------------------------------------------------------------------------------------------------------------
			ProgressUtil.Report(task, 1, 2, "Setting up build");

			// -----------------------------------------------------------------------------------------------------------------------------
			//		Create lookups
			// -----------------------------------------------------------------------------------------------------------------------------
			using (TrembleTimerScope _ = new(TrembleTimer.Context.CreateTypeLookups))
			{
				m_MapTypeLookup = new(m_SyncSettings);
				m_PrefabNameLookup = new(m_SyncSettings);
				m_MaterialNameLookup = new(m_SyncSettings);
				m_FieldConverter = new();
			}

			MapBsp mapBsp = existingMapBsp;
			if (mapBsp == null)
			{
				// -----------------------------------------------------------------------------------------------------------------------------
				//		Run Q3Map2
				// -----------------------------------------------------------------------------------------------------------------------------
				ProgressUtil.Report(task, 2, 2, "Running Q3Map2");

				List<string> output = new(1024);

				mapBsp = MapCompiler.BuildBsp(
					m_SyncSettings,
					mapFilePath,
					$"{m_SyncSettings.ExtraCommandLineArgs} {m_ImportSettings.ExtraCommandLineArgs}",
					out Q3Map2Result result,
					onOutputMessage: m => output.Add(m));

				m_ImportSettings.OnQ3Map2Result?.Invoke(result, output);

				switch (result)
				{
					case Q3Map2Result.Failed:
						ProgressUtil.Fail(task);

						m_ImportSettings.Error("Map compiler did not run correctly or was not found! Try syncing and then reimporting your map.");
						return CreateFailedMap(Q3Map2Result.Failed, "Map compiler did not run correctly!");

					case Q3Map2Result.FailedWithMissingTextures:
						m_ImportSettings.Error("Map compiler did not find textures correctly!");

#if UNITY_EDITOR
						if (!Application.isBatchMode && !Application.isPlaying)
						{
							if (SceneView.lastActiveSceneView)
							{
								SceneView.lastActiveSceneView.ShowNotification(new("Tremble: Reimport this map to fix texture UVs!"));
							}
						}
#endif

						break;
				}

				if (mapBsp == null)
				{
					m_ImportSettings.Error("Map compiler did not generate a BSP file.");
					ProgressUtil.Fail(task);

					return CreateFailedMap(Q3Map2Result.Failed, "Map compiler did not generate a BSP file.");
				}
			}

			// Preload assets, if required
			if (TrembleAssetLoader.SupportsPreloading)
			{
				List<string> pathsToPreload = new(128);
				mapBsp.GatherResourceList(m_MaterialNameLookup, m_PrefabNameLookup, pathsToPreload);

				await TrembleAssetLoader.PreloadAssetsByPaths(pathsToPreload);
			}

			// Check if the map has a Wad file set
			BspEntity worldspawn = mapBsp.GetWorldspawnEntity();
			if (worldspawn.TryGetString("wad", out string wadEntry) && !wadEntry.IsNullOrEmpty())
			{
				m_ImportSettings.OnWadsFound?.Invoke(wadEntry.Split(";"));
			}

			// Finally create objects
			m_GroupTransforms = new Transform[mapBsp.Groups.Length];

			MapDocument mapDocument = ParseBspIntoGameObject(mapBsp);

			ProgressUtil.Succeed(task);

			TrembleMapImportSettings.INTERNAL_SetCurrent(null);
			return mapDocument;
		}

		private void ForeachProcessor(Action<MapProcessorBase> action)
		{
			using TrembleTimerScope __ = new(TrembleTimer.Context.RunMapProcessors);

			foreach (MapProcessorBase p in m_ImportSettings.Processors)
			{
				using TrembleTimerScope _ = new(p.GetType());
				action(p);
			}
		}

		private MapDocument ParseBspIntoGameObject(MapBsp mapBsp)
		{
			using TrembleTimerScope __ = new(TrembleTimer.Context.CreateGameObjects);
			// Thank you John Evans. This is quite close to the original UBSPMapTools code.

			// Create processor
			GameObject root = new(mapBsp.MapName, typeof(MapDocument));
			Transform rootTransform = root.transform;
			ForeachProcessor(processor => processor.RootTransform = rootTransform);

			GameObject looseEntities = new("entities");
			looseEntities.transform.SetParent(rootTransform);

			ForeachProcessor(processor => processor.OnProcessingStarted(root, mapBsp));

			// Build meshes from brushes
			int unityLayer = LayerMask.NameToLayer(m_SyncSettings.WorldspawnLayer);
			if (unityLayer < 0)
			{
				m_ImportSettings.Warn($"Could not found layer {m_SyncSettings.WorldspawnLayer} - using Default!");
				unityLayer = 0;
			}

			m_MeshBuilder = new(m_ImportSettings, FindMaterial, m_SyncSettings.MainMeshName, m_ImportSettings.SmoothingAngle, m_ImportSettings.MaxMeshSurfaceArea);

			GameObject worldspawnObject;

			if (!m_SyncSettings.PipelineSplitMesh || !m_ImportSettings.SplitMesh)
			{
				string mainName = m_SyncSettings.MainMeshName;
				worldspawnObject = m_MeshBuilder.BuildMesh(mapBsp, rootTransform, 0, true, mainName, false, Vector3.zero, true);
				if (worldspawnObject)
				{
					worldspawnObject.layer = unityLayer;
				}
				else
				{
					m_ImportSettings.Error("Missing worldspawn brush?!");
				}
			}
			else
			{
				using TrembleTimerScope _ = new(TrembleTimer.Context.SplitMesh);

				worldspawnObject = new(m_SyncSettings.MainMeshName);
				worldspawnObject.transform.SetParent(rootTransform);
				worldspawnObject.layer = unityLayer;

				HashSet<int> uniqueFaceList = new(2048);
				m_MeshBuilder.BuildMesh_Recursive(mapBsp, worldspawnObject.transform, 0, uniqueFaceList, m_SyncSettings.ImportScale, unityLayer);
			}

			// Add the worldspawn script
			Type worldspawnScriptType = m_SyncSettings.WorldspawnScript?.Class ?? typeof(Worldspawn);
			Worldspawn worldspawnScript = (Worldspawn)worldspawnObject.AddComponent(worldspawnScriptType);

			// Custom entities/prefabs/brushes/etc - create scripts and spawn
			foreach (BspEntity entity in mapBsp.Entities)
			{
				// Work out layer/group
				int groupIdx = 0;

				if (!m_SyncSettings.DiscardMapGroups)
				{
					if (entity.TryGetInt(TBConsts.GROUP, out int group))
					{
						groupIdx = group;
					}

					if (entity.TryGetInt(TBConsts.LAYER, out int layer))
					{
						groupIdx = layer;
					}

					if (entity.TryGetInt(TBConsts.ID, out int groupOrLayer))
					{
						groupIdx = groupOrLayer;
					}
				}

				// Process entity and store in group if needed
				Transform parent = FindOrCreateGroup(mapBsp, groupIdx, rootTransform, looseEntities.transform);
				if (!parent)
				{
					// Owning group/layer is not for export!
					continue;
				}

				try
				{
					GameObject entityGameObject = CreateEntityGameObject(mapBsp, root, worldspawnObject, parent, entity);
					if (!entityGameObject)
						continue;

					entityGameObject.transform.SetAsLastSibling();

					// Store IDs
					m_ImportSettings.MapGameObjectToEntity(entityGameObject, entity);
				}
				catch (Exception ex)
				{
					m_ImportSettings.Error($"Failed to create entity '{entity.GetID()}' - {ex.Message}");
					Debug.LogException(ex);
				}
			}

			// Now set script values on all entities
			foreach (BspEntity entity in mapBsp.Entities)
			{
				if (!m_ImportSettings.TryGetGameObjectForEntity(entity, out GameObject instance))
					continue;

				SetFieldValuesOnAllScripts(instance, entity);

				// Move to parent, if it has one
				if (entity.TryGetString(FgdConsts.PROPERTY_PARENT, out string parentTarget) &&
				    TryFindParent(parentTarget, entity, out Transform newParent))
				{
					instance.transform.SetParent(newParent, true);
				}
			}

			// Now fire OnImportFromMapEntity
			foreach (BspEntity entity in mapBsp.Entities)
			{
				if (!m_ImportSettings.TryGetGameObjectForEntity(entity, out GameObject instance))
					continue;

				foreach (IOnImportFromMapEntity pifme in instance.GetComponents<IOnImportFromMapEntity>())
				{
					pifme.OnImportFromMapEntity(mapBsp, entity);
				}
			}

			ForeachProcessor(processor => processor.OnProcessingCompleted(root, mapBsp));

			// Move loose entities to end
			looseEntities.transform.SetAsLastSibling();

			// Write data into the Worldspawn
			// (BSP name, original asset GUID, worldspawn root, and targetname lookup)
			List<IDMapping> mappings = new(256);

			foreach (KeyValuePair<string, List<GameObject>> kvp in m_ImportSettings.AllEntityIDs)
			{
				mappings.Add(IDMapping.Create(kvp.Key, kvp.Value.ToArray()));
			}

			MapDocument mapDocument = root.GetComponent<MapDocument>();
			mapDocument.INTERNAL_SetData(
				bspName: mapBsp.MapName,
				originalAssetGuid: m_ImportSettings.AssetGUID ?? Guid.NewGuid().ToString(),
				worldspawnObject: worldspawnScript.gameObject,
				mapImportedTime: (ulong)DateTime.UtcNow.Ticks,
				ids: mappings.ToArray());

			return mapDocument;
		}

		private bool TryFindParent(string parentTarget, BspEntity entity, out Transform parent)
		{
			if (!m_ImportSettings.TryGetGameObjectsForID(parentTarget, out List<GameObject> targets) || targets.Count == 0)
			{
				m_ImportSettings.Warn($"Could not find parent '{parentTarget}' for {entity.GetClassname()} entity '{entity.GetID()}'");
				parent = null;
				return false;
			}

			parent = targets[0].transform;
			return true;
		}

		private GameObject CreateEntityGameObject(MapBsp mapBsp, GameObject root, GameObject worldspawnObject, Transform parentTransform, BspEntity entity)
		{
			// Class name
			string classname = entity.GetClassname();

			if (classname.EqualsInvariant(FgdConsts.WORLDSPAWN, caseSensitive: true)) // "worldspawn" entities are just part of the world!
			{
				ForeachProcessor(processor => processor.ProcessWorldSpawnProperties(mapBsp, entity, root));
				return worldspawnObject;
			}

			// It's a prefab?
			if (m_PrefabNameLookup.IsValidAsset(classname) && m_PrefabNameLookup.TryGetPrefabPathFromMapName(classname, out string prefabPath))
			{
				GameObject prefab = TrembleAssetLoader.LoadPrefabByPath(prefabPath);
				m_ImportSettings.AddDependency(prefab);

				if (!prefab)
				{
					m_ImportSettings.Error($"Couldn't find prefab at path '{prefabPath}' for classname '{classname}'!");
					return null;
				}

				bool wasActivePrefab = prefab.activeSelf;
				prefab.SetActive(false);

				GameObject instance = PrefabUtil.InstantiatePrefab(prefab, parentTransform);

				if (wasActivePrefab)
				{
					prefab.SetActive(true);
				}

				SetObjectName(root, instance, entity);

				Vector3 offset = Vector3.zero;
				if (TrembleAssetLoader.TryGetAssetDataFromMapName(classname, out AssetMetadata metadata))
				{
					offset = metadata.SpawnOffset;
				}
				else
				{
					Debug.LogWarning($"Could not find spawn offset for prefab '{prefab.name}'!");
				}

				SetTransformValuesFromEntity(instance, entity, offset);
				ForeachProcessor(processor => processor.ProcessPrefabEntity(mapBsp, entity, instance));

				if (wasActivePrefab)
				{
					instance.SetActive(true);
				}

				return instance;
			}

			// It's a brush or point entity
			if (m_MapTypeLookup.TryGetEntityTypeFromMapName(classname, out EntityType type))
			{
				if (type == EntityType.Brush) // brush entity
				{
					if (!entity.TryGetString("model", out string modelIdxString) || !modelIdxString.StartsWith("*"))
						return null;

					int modelIdx = int.Parse(modelIdxString.Substring(1));
					string brushClass = entity.GetClassname();

					m_MapTypeLookup.TryGetClassFromMapName(brushClass, out Type componentType);
					BrushEntityAttribute	bea = componentType.GetCustomAttribute<BrushEntityAttribute>();

					Vector3 modelCentre = (mapBsp.Models[modelIdx].Min + mapBsp.Models[modelIdx].Max) * 0.5f;

					// Rotate back properly
					Quaternion? rotation = null;
					Quaternion? inverseRotation = null;
					if (entity.TryGetRotation(FgdConsts.PROPERTY_ANGLES, out Quaternion angles))
					{
						rotation = angles;
						inverseRotation = Quaternion.Inverse(rotation.Value);
					}

					int unityLayer = 0;
					if (!bea.LayerName.IsNullOrEmpty())
					{
						unityLayer = LayerMask.NameToLayer(bea.LayerName);
						if (unityLayer < 0)
						{
							m_ImportSettings.Warn($"Could not found layer {bea.LayerName} - using Default!");
							unityLayer = 0;
						}
					}

					GameObject brush = m_MeshBuilder.BuildMesh(mapBsp, parentTransform, modelIdx, false, brushClass, false, modelCentre, false, inverseRotation);
					if (!brush)
					{
						Debug.LogError($"Discarded brush entity of class {brushClass} due to its model being degenerate!");
						return null;
					}

					brush.transform.position = modelCentre;
					if (rotation.HasValue)
					{
						brush.transform.rotation = rotation.Value;
					}

					SetObjectName(root, brush, entity);

					// Find type
					brush.AddComponent(componentType);
					m_ImportSettings.OnEntityAdded?.Invoke(brush, componentType);

					// Modify components based on type
					if (bea.BrushType is BrushType.Trigger or BrushType.Liquid)
					{
						// Remove the mesh renderer/filter unless liquid
						if (bea.BrushType != BrushType.Liquid)
						{
							GameObject.DestroyImmediate(brush.GetComponent<MeshRenderer>());
							GameObject.DestroyImmediate(brush.GetComponent<MeshFilter>());
						}

						// Make it a trigger instead
						if (brush.TryGetComponent(out MeshCollider meshCollider))
						{
							meshCollider.convex = true;
							meshCollider.isTrigger = true;
						}
					}

					if (bea.BrushType == BrushType.Invisible)
					{
						// Remove the mesh renderer/filter
						GameObject.DestroyImmediate(brush.GetComponent<MeshRenderer>());
						GameObject.DestroyImmediate(brush.GetComponent<MeshFilter>());
					}

					// Change layer if needed
					if (!bea.LayerName.IsNullOrEmpty())
					{
						brush.layer = unityLayer;
					}

					ForeachProcessor(processor => processor.ProcessBrushEntity(mapBsp, entity, brush));

					return brush;
				}
				else // point entity
				{
					string pointClass = entity.GetClassname();
					if (pointClass.EqualsInvariant(TrembleConsts.MAPFIX_ENTITY_NAME, caseSensitive: true))
					{
						// This point entity is used to fix map import - ignore it!
						return null;
					}

					GameObject pointEntity = new(pointClass);
					pointEntity.transform.SetParent(parentTransform);
					SetTransformValuesFromEntity(pointEntity, entity);
					SetObjectName(root, pointEntity, entity);

					// Find type
					if (m_MapTypeLookup.TryGetClassFromMapName(pointClass, out Type componentType))
					{
						pointEntity.AddComponent(componentType);
						m_ImportSettings.OnEntityAdded?.Invoke(pointEntity, componentType);
					}
					else
					{
						m_ImportSettings.Warn($"No type found for Point entity class: {pointClass} in map! Try Map Repair.");
					}

					ForeachProcessor(processor => processor.ProcessPointEntity(mapBsp, entity, pointEntity));

					return pointEntity;
				}
			}

			if (!classname.EqualsInvariant(TrembleConsts.MAPFIX_ENTITY_NAME, caseSensitive: true))
			{
				m_ImportSettings.Warn($"No type found for entity class: {classname} in map! Try Map Repair.");
			}

			return null;
		}


		// -----------------------------------------------------------------------------------------------------------------------------
		//		Helpers
		// -----------------------------------------------------------------------------------------------------------------------------
		private Material FindMaterial(string mapName)
		{
			// Have we seen this material before?
			if (s_MaterialCache.TryGetValue(mapName, out Material foundMaterial) && foundMaterial)
				return foundMaterial;

			// Special case: "__TB_empty" to null
			if (mapName.EqualsInvariant(TBConsts.EMPTY_TEXTURE) ||
			    mapName.ContainsInvariant(TBConsts.SKIP_TEXTURE))
				return null;

			// Special case: map "clip" to "M_NullRender"
			if (mapName.ContainsInvariant(TBConsts.CLIP_TEXTURE))
			{
				Material clipMaterial = TrembleAssetLoader.LoadClipMaterial();
				s_MaterialCache[mapName] = clipMaterial;
				return clipMaterial;
			}

			// Look it up
			if (!m_MaterialNameLookup.TryGetMaterialPathFromMapName(mapName, out string materialPath))
			{
#if !UNITY_EDITOR && !UNITY_SERVER
				string texturePath = Path.Combine(TrembleConsts.BASEQ3_PATH, "textures", mapName + ".png");
				if (File.Exists(texturePath))
				{
					// Try to create a runtime material for loose PNG
					Material looseMaterial = CreateRuntimeMaterial(texturePath);
					s_MaterialCache[mapName] = looseMaterial;
					return looseMaterial;
				}
#endif

				if (!mapName.Contains(TBConsts.SPECIAL_TEXTURE))
				{
					m_ImportSettings.Warn($"Failed to find a Unity material for {mapName}!");
				}

				return null;
			}

			// Load it up
			Material material = TrembleAssetLoader.LoadAssetByPath<Material>(materialPath);
			if (!material)
			{
				m_ImportSettings.Warn($"Material at path '{materialPath}' could not be loaded!");
				return null;
			}

			s_MaterialCache[mapName] = material;
			return material;
		}

#if UNITY_EDITOR
		public static Material CreateEditorMaterial(string textureAssetPath, float smoothness = -1f, float metallic = -1)
		{
			Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
			string textureName = Path.GetFileNameWithoutExtension(textureAssetPath);

			return CreateMaterial(textureName, texture, smoothness, metallic);
		}
#endif

		public static Material CreateRuntimeMaterial(string pngPath, float smoothness = -1f, float metallic = -1)
		{
			// Load texture
			byte[] textureData = File.ReadAllBytes(pngPath);
			Texture2D texture = new(1, 1);
			texture.LoadImage(textureData, markNonReadable: true);

			string textureName = Path.GetFileNameWithoutExtension(pngPath);

			return CreateMaterial(textureName, texture, smoothness, metallic);
		}

		public static Material CreateMaterial(string textureName, Texture2D texture, float smoothness = -1f, float metallic = -1)
		{
			if (smoothness == -1)
			{
				smoothness = GetParamValue(textureName, "s", 0.5f);
			}

			if (metallic == -1)
			{
				metallic = GetParamValue(textureName, "m", 0f);
			}

			// Create material
#if URP_INSTALLED
			Material m = new(Shader.Find("Universal Render Pipeline/Lit"));
			m.mainTexture = texture;
			m.SetFloat("_Smoothness", smoothness);
			m.SetFloat("_Metallic", metallic);
			return m;
#elif HDRP_INSTALLED
			Material m = new(Shader.Find("HDRP/Lit"));
			m.SetTexture("_BaseColorMap", texture);  // Use _BaseColorMap instead of _MainTex
			m.SetFloat("_Smoothness", smoothness);
			m.SetFloat("_Metallic", metallic);
			return m;
#else
			Material m = new(Shader.Find("Standard"));
			m.mainTexture = texture;
			m.SetFloat("_Glossiness", smoothness);  // Use _Glossiness instead of _Smoothness
			m.SetFloat("_Metallic", metallic);
			return m;
#endif
		}

		private static float GetParamValue(string input, string param, float defaultValue)
		{
			string marker = $"_{param}";

			// Find marker!
			int index = input.IndexOfInvariant(marker);
			if (index == -1)
				return defaultValue;

			index += marker.Length;

			// Collect numbers into array
			char[] buffer = new char[8];
			int bufPtr = 0;

			while (index < input.Length && bufPtr < buffer.Length && Char.IsNumber(input[index]))
			{
				buffer[bufPtr++] = input[index++];
			}

			// Parse to float
			ReadOnlySpan<char> strBuffer = new(buffer);
			if (!strBuffer.TryParseFloat(out float value))
				return defaultValue;

			return value / 100f;
		}

		private readonly StringBuilder m_NameBuilder = new(128);
		private void SetObjectName(GameObject root, GameObject go, BspEntity entity)
		{
			m_NameBuilder.Clear();

			string classname = entity.GetClassname();
			m_NameBuilder.Append(classname);
			m_NameBuilder.Append(' ');

			bool appendSerialNumber = true;

			string id = entity.GetID();
			if (id != null)
			{
				m_NameBuilder.Append('\'');
				m_NameBuilder.Append(id);
				m_NameBuilder.Append('\'');

				GameObject existing = root.Find_Recursive(m_NameBuilder.ToString());
				if (existing)
				{
					m_NameBuilder.Append(' ');
				}
				else
				{
					appendSerialNumber = false;
				}
			}

			if (appendSerialNumber)
			{
				// Append global entity counter, e.g. #12 if not named
				m_NameBuilder.Append('#');
				m_NameBuilder.Append(entity.SerialNumber);
			}

			go.name = m_NameBuilder.ToString();
		}

		private void SetTransformValuesFromEntity(GameObject instance, BspEntity entity) => SetTransformValuesFromEntity(instance, entity, Vector3.zero);
		private void SetTransformValuesFromEntity(GameObject instance, BspEntity entity, Vector3 offset)
		{
			Transform instanceTransform = instance.transform;

			// Set position
			instanceTransform.position = entity.GetPosition();

			// Set scale
			if (entity.TryGetUntransformedVector(FgdConsts.PROPERTY_SCALE, out Vector3 scale))
			{
				instanceTransform.localScale = scale.Q3ToUnityVector();
			}

			// Set rotation
			if (entity.TryGetRotation(FgdConsts.PROPERTY_ANGLES, out Quaternion angles))
			{
				instanceTransform.rotation = angles;
			}

			// Finally move by offset, based on scaling and rotation
			Vector3 scaledOffset = offset;
			scaledOffset.Scale(instanceTransform.localScale);
			instanceTransform.position += instanceTransform.rotation * scaledOffset;

#if !UNITY_EDITOR
			if (instance.TryGetComponent(out Rigidbody rb))
			{
#if UNITY_2022_1_OR_NEWER
				rb.Move(instanceTransform.position, instanceTransform.rotation);
#else
				rb.position = instanceTransform.position;
				rb.rotation = instanceTransform.rotation;
#endif
			}
#endif
		}

		private void SetFieldValuesOnAllScripts(GameObject instance, BspEntity entity)
		{
			bool hasSetFieldValues = false; // Only set fields on first EntityAttributeBase found

			foreach (MonoBehaviour mb in instance.GetComponents<MonoBehaviour>())
			{
				if (!mb)
				{
					Debug.LogWarning($"Prefab '{instance.name}' has a missing script!'");
					continue;
				}

				Type scriptType = mb.GetType();
				if (!hasSetFieldValues && scriptType.IsSubclassOf(typeof(Worldspawn)) || scriptType.HasCustomAttribute<EntityAttributeBase>())
				{
					foreach (FieldInfo fi in scriptType.GetAllInstanceFieldsIncludingPrivateAndBaseClasses())
					{
						SetFieldValue(entity, mb, fi);
					}

					hasSetFieldValues = true;
				}
			}
		}

		private void SetFieldValue(BspEntity entity, MonoBehaviour mb, FieldInfo target)
		{
			target.GetCustomAttributes(out TrembleAttribute ta, out NoTrembleAttribute nta, out SerializeField sfa, out FormerlySerializedAsAttribute fsa);

			// Work out names for this field
			List<string> knownFieldNames = new() { target.Name.GetFieldNameInMap(m_SyncSettings.FieldNamingConvention) };
			if (ta?.OverrideName != null)
			{
				knownFieldNames.Insert(0, ta.OverrideName);
			}
			if (fsa != null)
			{
				knownFieldNames.Add(fsa.oldName.GetFieldNameInMap(m_SyncSettings.FieldNamingConvention));
			}

			if (ta == null && !(m_SyncSettings.SyncSerializedFields && sfa != null) && !target.IsPublic)
			{
				// If not public, and not [Tremble], skip!
				return;
			}

			if (nta != null)
			{
				// Specifically NOT serialised in maps - skip!
				return;
			}

			// Special case: Prefabs and Materials
			bool isMaterialOrPrefabField = target.FieldType == typeof(Material) || target.FieldType == typeof(GameObject);
			if (isMaterialOrPrefabField && ta == null && !m_SyncSettings.SyncMaterialsAndPrefabs)
			{
				// This is a Material or Prefab but we've turned off the setting
				return;
			}

			// Work out which name to use
			int matchingFieldIdx = 0;
			for (int knownFieldIdx = 0; knownFieldIdx < knownFieldNames.Count; knownFieldIdx++)
			{
				string knownFieldName = knownFieldNames[knownFieldIdx];
				if (!entity.HasKey(knownFieldName))
					continue;

				matchingFieldIdx = knownFieldIdx;
				break;
			}

			// Try to convert value!
			string fieldName = knownFieldNames[matchingFieldIdx];
			TrembleFieldConverter converter = m_FieldConverter.GetConverterForType(target.FieldType);

			if (converter != null)
			{
				if (target.FieldType.IsArray)
				{
					// Use a converter to convert the map value
					if (!converter.TryGetValuesFromMap(entity, fieldName, mb.gameObject, target, out object[] values))
						return;

					Type targetElementType = target.FieldType.GetElementType()!;

					Array filteredArray = Array.CreateInstance(targetElementType, values.Length);
					for (int i = 0; i < values.Length; i++)
					{
						object valueAtIndex = values[i];
						if (valueAtIndex == null)
							continue;

						if (targetElementType.IsAssignableFrom(valueAtIndex.GetType()))
						{
							filteredArray.SetValue(valueAtIndex, i);
						}
						else
						{
							Debug.LogWarning($"Element {i} of {mb.name}::{target.Name} is NOT compatible with an array of {targetElementType}s (it's a {valueAtIndex.GetType().Name}) and was skipped!");
						}
					}

					target.SetValue(mb, filteredArray);
				}
				else
				{
					// Use a converter to convert the map value
					if (!converter.TryGetValueFromMap(entity, fieldName, mb.gameObject, target, out object value))
						return;

					if (target.FieldType == typeof(Transform) && value is Component c)
					{
						value = c.transform;
					}

					target.SetValue(mb, value);
				}
			}
			else if (ta != null)
			{
				// Nothing worked - log an import warning
				m_ImportSettings.Warn($"Saw field '{target.Name}' of type {target.FieldType}, which is not yet supported. Try adding a custom {nameof(TrembleFieldConverter)} for it, if you like!");
			}
		}

		private Transform FindOrCreateGroup(MapBsp mapBsp, int groupIdx, Transform rootTransform, Transform otherEntitiesTransform)
		{
			// Root group, or we have no matching group
			if (groupIdx == 0 || m_GroupTransforms == null || groupIdx >= m_GroupTransforms.Length)
				return otherEntitiesTransform;

			// Existing - return it!
			if (m_GroupTransforms[groupIdx])
				return m_GroupTransforms[groupIdx];

			if (mapBsp.Groups[groupIdx].IsNoExport)
				return null;

			// New group, create and return - but be careful not to name the same as others
			string newName = $"{mapBsp.Groups[groupIdx].LabelName} #{++m_GroupCounter}";

			GameObject group = new(newName);
			group.transform.SetParent(rootTransform, false);
			m_GroupTransforms[groupIdx] = group.transform;

			return m_GroupTransforms[groupIdx];
		}
	}
}