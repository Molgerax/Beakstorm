//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public class MapRepairWindow : EditorWindow
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Menu Items
		// -----------------------------------------------------------------------------------------------------------------------------
		[MenuItem("Tools/Tremble/Repair Map...")]
		public static void OpenMapRepair() => OpenMapRepairImpl();
		public static void OpenMapRepair(string mapPath) => OpenMapRepairImpl(mapPath);

		[MenuItem("Assets/Repair Map")]
		private static void RepairMap()
		{
			string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			OpenMapRepairImpl(assetPath);
		}
		[MenuItem("Assets/Repair Map", true)]
		private static bool RepairMap_Validation()
		{
			string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			return AssetDatabaseUtil.GetAllTrembleMapPaths().Contains(assetPath);
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Utility
		// -----------------------------------------------------------------------------------------------------------------------------
		public static string GetGameNameFromMap(string mapPath)
		{
			Dictionary<string, string> metadata = MapParser.ParseMetadataOnly(mapPath);
			return metadata.GetValueOrDefault("Game");
		}

		public static bool IsMapForeign(string mapPath, out string gameName)
		{
			gameName = GetGameNameFromMap(mapPath);
			if (gameName == null)
				return true;

			return !gameName.EqualsInvariant(TrembleConsts.GAME_NAME, caseSensitive: true) &&
			       !gameName.EqualsInvariant(TrembleConsts.ASSET_STORE_GAME_NAME, caseSensitive: true);
		}

		private static void OpenMapRepairImpl(string mapPath = null)
		{
			MapRepairWindow window = GetWindow<MapRepairWindow>(true, "Map Repair", true);
			window.m_MapPath = mapPath;

			Vector2 size = new(520f, 640f);
			window.minSize = size;
			window.position = new(EditorGUIUtility.GetMainWindowPosition().center - size / 2f, size);
			window.Show();
		}


		// -----------------------------------------------------------------------------------------------------------------------------
		//		Consts
		// -----------------------------------------------------------------------------------------------------------------------------
		record ClassData(string MappedClassname, FgdClass GeneratedClass);

		private const float PADDING_SIZE = 10f;
		private GUIStyle m_TitleStyle;


		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly List<string> m_AvailableMapPaths = new();
		private readonly List<string> m_AvailableTrembleEntityClassnames = new();
		private readonly List<string> m_AvailableTextureNames = new();

		private readonly Dictionary<string, ClassData> m_MapClassToData = new();
		private readonly Dictionary<string, string> m_MapTextureNameToMappedName = new();

		private TrembleSyncSettings m_SyncSettings;
		private MapTypeLookup m_MapTypeLookup;
		private PrefabNameLookup m_PrefabNameLookup;
		private MaterialNameLookup m_MaterialNameLookup;

		private int m_EditingMapIdx;
		private string m_MapPath;
		private Map m_EditingMap;
		private bool m_ClassSettingsFoldOutOpen;

		private string m_OutputPath= "Code/Entities";
		private string m_GeneratedNamespace = "TrembleEntities";

		private readonly string[] m_Tabs = { "Textures/Materials", "Entities" };
		private int m_SelectedTab;
		private Vector2 m_ScrollPosition = Vector2.zero;

		private void OnEnable()
		{
			m_SyncSettings = TrembleSyncSettings.Get();
			m_MapTypeLookup = new(m_SyncSettings);
			m_PrefabNameLookup = new(m_SyncSettings);
			m_MaterialNameLookup = new(m_SyncSettings);

			m_AvailableTrembleEntityClassnames.Clear();
			foreach (Type entityType in m_MapTypeLookup.AllTypes)
			{
				if (!m_MapTypeLookup.TryGetMapNameFromClass(entityType, out string mapName))
					continue;

				m_AvailableTrembleEntityClassnames.Add(mapName);
			}

			foreach (string prefabPath in m_PrefabNameLookup.AllPrefabPaths)
			{
				if (!m_PrefabNameLookup.TryGetMapNameFromPrefabPath(prefabPath, out string prefabName))
					continue;

				m_AvailableTrembleEntityClassnames.Add(prefabName);
			}

			m_AvailableTextureNames.Clear();
			foreach (MaterialGroup matGroup in m_SyncSettings.GetMaterialGroupsOrDefault())
			{
				foreach (Material material in matGroup.Materials)
				{
					string matPath = AssetDatabase.GetAssetPath(material);

					if (matPath.IsNullOrEmpty() || !m_MaterialNameLookup.TryGetMapNameFromMaterialPath(matPath, out string mapName))
						continue;

					m_AvailableTextureNames.Add(mapName);
				}
			}

			m_AvailableTrembleEntityClassnames.Sort();
			m_AvailableTrembleEntityClassnames.Insert(0, "(generate new entity class)");

			m_AvailableTextureNames.Sort();
			m_AvailableTextureNames.Insert(0, "(leave as is)");

			m_AvailableMapPaths.Add("(select a map)");
			m_AvailableMapPaths.AddRange(AssetDatabaseUtil.GetAllTrembleMapPaths());

			if (m_EditingMapIdx != 0)
			{
				Init(m_EditingMapIdx); // Re-init
			}
		}

		private void Init(int mapIndex)
		{
			if (mapIndex == 0)
			{
				m_EditingMap = null;
				return;
			}

			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
			m_EditingMap = MapParser.Parse(m_AvailableMapPaths[mapIndex]);
			m_EditingMapIdx = mapIndex;

			// Format title
			string mapFilename = Path.GetFileName(m_AvailableMapPaths[mapIndex]);
			titleContent = new($"Repairing '{mapFilename}' - {GetGameOriginInfo()}");

			// Parse!
			m_MapClassToData.Clear();
			m_MapTextureNameToMappedName.Clear();

			foreach (MapEntity entity in m_EditingMap.Entities)
			{
				bool isBrushEntity = entity.Brushes.Count > 0;

				if (!entity.Classname.EqualsInvariant("worldspawn", caseSensitive: true)
				    && !entity.Classname.EqualsInvariant("func_group", caseSensitive: true))
				{
					// Find or create class
					if (!m_MapClassToData.TryGetValue(entity.Classname, out ClassData classData))
					{
						FgdClass newClass = new(isBrushEntity ? FgdClassType.Brush : FgdClassType.Point, entity.Classname, $"Class {entity.Classname}");
						newClass.AddBaseClass(FgdConsts.CLASS_MAP_BASE);

						if (!isBrushEntity)
						{
							newClass.AddBaseClass(FgdConsts.CLASS_MAP_POINT_BASE);
						}

						string existingClassname = entity.Classname;
						if (!m_MapTypeLookup.TryGetClassFromMapName(entity.Classname, out _) &&
						    !m_PrefabNameLookup.TryGetPrefabPathFromMapName(entity.Classname, out _))
						{
							existingClassname = null;
						}
						classData = new(GeneratedClass: newClass, MappedClassname: existingClassname);
						m_MapClassToData[entity.Classname] = classData;
					}

					foreach ((string key, string value) in entity.Entries)
					{
						if (key.EqualsInvariant("classname"))
							continue;

						if (classData.GeneratedClass.HasField(key))
							continue;

						classData.GeneratedClass.TryAddInferredField(key, value);
					}
				}

				foreach (MapBrush brush in entity.Brushes)
				{
					foreach (MapBrushFace face in brush.Faces)
					{
						if (m_MapTextureNameToMappedName.ContainsKey(face.TextureName))
							continue;

						if (face.TextureName.EqualsInvariant(TBConsts.EMPTY_TEXTURE, caseSensitive: true))
							continue;

						// Try to find material
						string group = null;
						string textureName = face.TextureName;
						int slashIdx = textureName.IndexOf('/');

						if (slashIdx != -1)
						{
							group = face.TextureName.Substring(0, slashIdx);
							textureName = face.TextureName.Substring(slashIdx + 1);

							if (group.EqualsInvariant(TBConsts.SPECIAL_TEXTURE, caseSensitive: true))
								continue;
						}

						MaterialGroup materialGroup = MaterialGroup.CreateDefault();
						if (!group.IsNullOrEmpty())
						{
							MaterialGroup[] mg = syncSettings.MaterialGroups
								.Where(mg => mg.Name.EqualsInvariant(group))
								.ToArray();

							if (mg.Length > 0)
							{
								materialGroup = mg[0];
							}
						}

						string matchingMaterialPath;
						Material matchingMaterial = materialGroup.Materials.FirstOrDefault(m => m && m.name.ContainsInvariant(textureName));

						if (matchingMaterial)
						{
							matchingMaterialPath = AssetDatabase.GetAssetPath(matchingMaterial);
						}
						else
						{
							matchingMaterialPath = AssetDatabase.FindAssets($"t:material {textureName}")
								.Select(AssetDatabase.GUIDToAssetPath)
								.FirstOrDefault();
						}

						if (!matchingMaterialPath.IsNullOrEmpty() && m_MaterialNameLookup.TryGetMapNameFromMaterialPath(matchingMaterialPath, out string mapName))
						{
							m_MapTextureNameToMappedName.Add(face.TextureName, mapName);
						}
						else
						{
							m_MapTextureNameToMappedName.Add(face.TextureName, null);
						}
					}
				}
			}
		}

		private void OnGUI()
		{
			if (m_EditingMapIdx == 0)
			{
				m_TitleStyle ??= new(EditorStyles.boldLabel)
				{
					fontSize = EditorStyles.boldLabel.fontSize + 5
				};

				if (m_MapPath.IsNullOrEmpty())
				{
					GUILayout.Label("Select a map to repair:", m_TitleStyle);

					int currentMapIdx = EditorGUILayout.Popup(m_EditingMapIdx, m_AvailableMapPaths.ToArray());
					Init(currentMapIdx);

					GUILayout.Label("Choose a map above to repair it. Repairing a map allows you to fix up broken"
					                + " materials and entity references.\n\nIt can also be used to try to import \"foreign\" maps from"
					                + " other games, although using it in this way is experimental - particularly you may find that"
					                + " texture UVs are completely incorrect for foreign maps.\n\nTremble will however attempt to"
					                + " generate new MonoBehaviour classes for any missing entities - but these are best-guesses"
					                + " based on the map file and will need manual editing.", EditorStyles.helpBox);
				}
				else
				{
					int mapIdx = Math.Max(0, m_AvailableMapPaths.IndexOf(m_MapPath));
					Init(mapIdx);
				}
			}
			else
			{
				m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, m_Tabs, "LargeButton");

				using (EditorGUILayout.ScrollViewScope scroll = new(m_ScrollPosition, GUILayout.ExpandHeight(true)))
				{
					EditorGUILayoutUtil.Pad(PADDING_SIZE, () =>
					{
						switch (m_SelectedTab)
						{
							case 0: OnTexturesGUI(); break;
							case 1: OnClassesGUI(); break;
						}
					});
					m_ScrollPosition = scroll.scrollPosition;
				}

				if (GUILayout.Button("Repair and Resave!", GUILayout.Height(50f)))
				{
					RepairAndSave();
				}
			}
		}

		private void OnTexturesGUI()
		{
			bool hasMissingTextures = m_MapTextureNameToMappedName.Any(kv => kv.Value == null);
			if (hasMissingTextures)
			{
				GUI.color = Color.yellow;
				{
					GUILayout.BeginVertical(EditorStyles.helpBox);
					{
						GUILayout.Label("If you have a WAD file for this game, you should first import that to generate Materials for the map's textures.", EditorStyles.wordWrappedLabel);
						if (GUILayout.Button("Import WAD"))
						{
							string wadFilePath = EditorUtility.OpenFilePanel("Select WAD file", "Assets", "wad");
							if (wadFilePath.IsNullOrEmpty())
								return;

							string mapBasePath = Path.GetDirectoryName(m_AvailableMapPaths[m_EditingMapIdx]);
							File.Copy(wadFilePath, Path.Combine(mapBasePath, Path.GetFileName(wadFilePath)));

							AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

							EditorApplication.delayCall += () =>
							{
								EditorUtility.DisplayDialog("WAD Imported!", "The WAD file was imported successfully!", "Okay");
								OnEnable();
							};
						}
					}
					GUILayout.EndVertical();

					GUILayout.Space(20f);
				}
				GUI.color = Color.white;
			}

			GUILayoutOption fixedHalfWidth = GUILayout.Width(position.width / 2f - PADDING_SIZE * 2f);
			GUILayoutOption smallerHalfWidth = GUILayout.Width(position.width / 2f - PADDING_SIZE * 2f - 25f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Texture from Map:", EditorStyles.boldLabel, fixedHalfWidth);
				GUILayout.Label("Unity Material:", EditorStyles.boldLabel, fixedHalfWidth);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5f);

			foreach (string textureName in m_MapTextureNameToMappedName.Keys.OrderBy(mt => mt))
			{
				if (textureName.EqualsInvariant(TBConsts.CLIP_TEXTURE) ||
				    textureName.EqualsInvariant(TBConsts.SKIP_TEXTURE) ||
				    textureName.EqualsInvariant(TBConsts.TRIGGER_TEXTURE) ||
				    textureName.StartsWithInvariant(TBConsts.SPECIAL_TEXTURE))
					continue;

				GUILayout.BeginHorizontal();
				{
					GUILayout.Label(textureName, fixedHalfWidth);

					Color colour = Color.white;

					int prevSelectedIdx = Math.Max(0, m_AvailableTextureNames.IndexOf(m_MapTextureNameToMappedName[textureName]));
					if (prevSelectedIdx == 0)
					{
						colour = Color.red;
					}
					else if (!m_MapTextureNameToMappedName[textureName].EqualsInvariant(textureName, caseSensitive: true))
					{
						colour = Color.yellow;
					}

					GUI.color = colour;
					int selectedIdx = EditorGUILayout.Popup(prevSelectedIdx, m_AvailableTextureNames.ToArray(), smallerHalfWidth);
					GUI.color = Color.white;

					if (prevSelectedIdx != selectedIdx)
					{
						m_MapTextureNameToMappedName[textureName] = m_AvailableTextureNames[selectedIdx];
					}

					if (GUILayout.Button(new GUIContent("!", "Copy to all missing"), GUILayout.Width(20f), GUILayout.Height(20f)))
					{
						foreach (string mapTextureName in m_MapTextureNameToMappedName.Keys.ToArray())
						{
							if (m_MapTextureNameToMappedName[mapTextureName].IsNullOrEmpty())
							{
								m_MapTextureNameToMappedName[mapTextureName] = m_AvailableTextureNames[selectedIdx];
							}
						}
						GUILayout.EndHorizontal();
						return;
					}
				}
				GUILayout.EndHorizontal();
			}
		}

		private void OnClassesGUI()
		{
			bool hasMissingClasses = m_MapClassToData.Any(kv => !kv.Key.EqualsInvariant(kv.Value.MappedClassname));
			if (hasMissingClasses)
			{
				GUI.color = Color.yellow;
				{
					GUILayout.BeginVertical(EditorStyles.helpBox);
					{
						GUILayout.Label("If you have an FGD file for this game, you should first import that to generate MonoBehaviours for the game's entities. If you do this, the classes generated from Map Repair will have much more accurate names and descriptions!", EditorStyles.wordWrappedLabel);
						if (GUILayout.Button("Import FGD"))
						{
							string fgdFilePath = EditorUtility.OpenFilePanel("Select FGD file", "Assets", "fgd");
							if (fgdFilePath.IsNullOrEmpty())
								return;

							string mapBasePath = Path.GetDirectoryName(m_AvailableMapPaths[m_EditingMapIdx]);
							File.Copy(fgdFilePath, Path.Combine(mapBasePath, Path.GetFileName(fgdFilePath)));
							EditorCompileUtil.CompileThen(typeof(MapRepairWindow), nameof(OnPostCompileAfterFGDImport));
						}
					}
					GUILayout.EndVertical();

					GUILayout.Space(20f);
				}
				GUI.color = Color.white;
			}

			GUILayoutOption fixedHalfWidth = GUILayout.Width(position.width / 2f - PADDING_SIZE * 2f - 10f);

			GUILayout.BeginHorizontal();
			{
				GUILayout.Label("Entity Class from Map:", EditorStyles.boldLabel, fixedHalfWidth);
				GUILayout.Label("MonoBehaviour:", EditorStyles.boldLabel, fixedHalfWidth);
			}
			GUILayout.EndHorizontal();

			GUILayout.Space(5f);

			foreach (string className in m_MapClassToData.Keys.OrderBy(cn => cn))
			{
				GUILayout.BeginHorizontal();
				{
					GUILayout.Label(className, fixedHalfWidth);

					Color colour = Color.white;

					int prevSelectedIdx = Math.Max(0, m_AvailableTrembleEntityClassnames.IndexOf(m_MapClassToData[className].MappedClassname));
					if (prevSelectedIdx == 0)
					{
						colour = Color.green;
					}
					else if (!m_MapClassToData[className].MappedClassname.EqualsInvariant(className, caseSensitive: true))
					{
						colour = Color.yellow;
					}

					GUI.color = colour;
					int selectedIdx = EditorGUILayout.Popup(prevSelectedIdx, m_AvailableTrembleEntityClassnames.ToArray(), fixedHalfWidth);
					GUI.color = Color.white;

					if (prevSelectedIdx != selectedIdx)
					{
						m_MapClassToData[className] = m_MapClassToData[className] with
						{
							MappedClassname = m_AvailableTrembleEntityClassnames[selectedIdx]
						};
					}
				}
				GUILayout.EndHorizontal();
			}

			EditorGUILayout.Space(10f);

			m_ClassSettingsFoldOutOpen = EditorGUILayout.Foldout(m_ClassSettingsFoldOutOpen, "Class Generation Settings", true);
			if (m_ClassSettingsFoldOutOpen)
			{
				EditorGUI.indentLevel++;

				m_OutputPath = EditorGUILayout.TextField("Save To", m_OutputPath);
				GUILayout.Label("There is the folder where Tremble will store any new classes for the entities found in the map. This path is relative to your Assets folder. If it doesn't exist, it'll be created!", EditorStyles.helpBox);

				m_GeneratedNamespace = EditorGUILayout.TextField("C# Namespace", m_GeneratedNamespace);
				GUILayout.Label("This namespace is only applied to newly-generated entity MonoBehaviours.", EditorStyles.helpBox);

				EditorGUI.indentLevel--;
			}
		}

		private string GetGameOriginInfo()
		{
			string gameName = m_EditingMap.Metadata.GetValueOrDefault("Game");

			if (gameName.IsNullOrEmpty())
				return "map from Quake(?)";

			if (gameName.EqualsInvariant(TrembleConsts.GAME_NAME, caseSensitive: true))
				return "map from this game";

			if (gameName.EqualsInvariant(TrembleConsts.ASSET_STORE_GAME_NAME, caseSensitive: true))
				return "map from Tremble sample";

			if (gameName.ContainsInvariant("unity:"))
				return "map from another Tremble game";

			return $"map from {gameName}";
		}

		private void RepairAndSave()
		{
			if (m_EditingMap == null)
				return;

			string mapFilename = Path.GetFileName(m_AvailableMapPaths[m_EditingMapIdx]);

			if (TrenchBroomUtil.IsTrenchBroomRunning)
			{
				EditorUtility.DisplayDialog(
					title: "Uh oh - TrenchBroom appears to be running!",
					message: $"Hey - if you have {mapFilename} open in TrenchBroom right now, you should "
					         + "close it before repairing the map",
					"Closed, continue repairing!");
			}

			bool generatedAnyNewClasses = false;

			// Write out classes for any missing entities
			foreach ((string _, ClassData classData) in m_MapClassToData)
			{
				if (!classData.MappedClassname.IsNullOrEmpty())
					continue;

				string rootFolder = Path.Combine("Assets", m_OutputPath);
				classData.GeneratedClass.Name.Split('_', out string category, out string classname);

				// Use category as folder
				if (!category.IsNullOrEmpty())
				{
					rootFolder = Path.Combine(rootFolder, category.ToNamingConvention(NamingConvention.UpperCamelCase));
				}

				string fileName = classname.ToNamingConvention(NamingConvention.UpperCamelCase) + ".cs";

				string filePath = Path.Combine(rootFolder, fileName);
				DirectoryUtil.CreateAllDirectories(filePath);
				classData.GeneratedClass.WriteCSharp(filePath, namespaceName: m_GeneratedNamespace);

				generatedAnyNewClasses = true;
			}

			// Pass 1: remap entity classes & strip 'wad' from worldspawn
			foreach (MapEntity entity in m_EditingMap.Entities)
			{
				// Remove Wad files
				if (entity.Classname.EqualsInvariant("worldspawn", caseSensitive: true) && entity.Entries.ContainsKey("wad"))
				{
					entity.Entries.Remove("wad");
				}

				if (!m_MapClassToData.TryGetValue(entity.Classname, out ClassData classData))
					continue;

				// Not remapped?
				if (classData.MappedClassname.IsNullOrEmpty())
					continue;

				entity.Entries["classname"] = classData.MappedClassname;
			}

			// Pass 2: remap textures
			foreach (MapEntity entity in m_EditingMap.Entities)
			{
				foreach (MapBrush brush in entity.Brushes)
				{
					foreach (MapBrushFace face in brush.Faces)
					{
						// Ignore if not mapped, or mapped texture is something like "(leave as is)"
						if (m_MapTextureNameToMappedName.TryGetValue(face.TextureName, out string mappedTexture) &&
						    !mappedTexture.IsNullOrEmpty() && !mappedTexture.ContainsInvariant("("))
						{
							face.TextureName = mappedTexture;
						}
					}
				}
			}

			// Rebuild new code
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

			// Import fixed map
			string mapPath = m_AvailableMapPaths[m_EditingMapIdx];
			m_EditingMap.WriteTo(mapPath);
			AssetDatabase.ImportAsset(mapPath);

			// Warn about re-importing
			if (generatedAnyNewClasses)
			{
				EditorUtility.DisplayDialog("New Classes Generated", "Tremble has generated new MonoBehaviours for your map entities. You might need to manually Reimport the map file once in order for these to be picked up.", "Sure thing!");
			}

			Close();
		}

		private static void OnPostCompileAfterFGDImport()
		{
			EditorUtility.DisplayDialog("FGD Imported!", "The FGD file was imported successfully!", "Okay");

			MapRepairWindow mrw = GetWindow<MapRepairWindow>();
			mrw.OnEnable();
		}
	}
}