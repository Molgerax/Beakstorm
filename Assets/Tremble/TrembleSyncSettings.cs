// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	public enum TrenchBroomVersion
	{
		Version2023_1,
		Version2024_1,
		Version2024_2,
		Version2025_3,
		VersionNext,
	}
	
	public enum NamingConvention
	{
		SnakeCase,
		UpperCamelCase,
		LowerCamelCase,
		PreserveExact,
		HumanFriendly,
	}

	public enum Q3Map2ResultDisplayType
	{
		NeverShow,
		ShowWhenWarningsOccur,
		AlwaysShow
	}

	public enum FgdFormattingStyle
	{
		Fast,
		HumanReadable,
		VeryVerbose
	}

	public enum EntityIconStyle
	{
		Nothing,
		ColouredDiamondShapes,
		ColouredLabels,
	}

	public enum PrefabOverrideHandling
	{
		AllowOverrides,
		WarnOnOverridesFound,
		AutomaticallyRevert
	}

	[Serializable]
	public struct MapProcessorEntry
	{
		public MapProcessorClass Class;
		public bool Enabled;
	}

	[Serializable]
	public struct MaterialGroup
	{
		public string Name;
		public Material[] Materials;

		public static MaterialGroup CreateDefault()
		{
			return new()
			{
				Name = "game",
				Materials = TrembleAssetLoader.FindAssetPaths<Material>()
					.Where(p => Path.GetExtension(p).EqualsInvariant(".mat"))
					.Where(p => !p.ContainsInvariant("M_NullRender"))
					.Select(TrembleAssetLoader.LoadAssetByPath<Material>)
					.ToArray()
			};
		}
	}

	[Serializable]
	public struct AssetMetadata
	{
		public string MapName;
		public string Path;
		public string FullTypeName;

#if ADDRESSABLES_INSTALLED
		public string AddressableName;
#endif
		
		public Vector3 SpawnOffset;
	}

	public class TrembleSyncSettings : ScriptableObject
	{
#if ADDRESSABLES_INSTALLED
		public const string ADDRESSABLE_KEY = "TrembleSyncSettings";
#endif

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Singleton
		// -----------------------------------------------------------------------------------------------------------------------------
		private static TrembleSyncSettings s_TrembleSyncSettings;
		public static TrembleSyncSettings Get(bool createIfNotExists = true)
		{
			// Return cached?
			if (s_TrembleSyncSettings)
				return s_TrembleSyncSettings;

			// Load & cache if possible
			s_TrembleSyncSettings = TrembleAssetLoader.LoadSyncSettings();
			if (s_TrembleSyncSettings || !createIfNotExists)
				return s_TrembleSyncSettings;

			// Return, or create temporary
			s_TrembleSyncSettings = CreateInstance<TrembleSyncSettings>();
#if UNITY_EDITOR
			AssetDatabase.CreateAsset(s_TrembleSyncSettings, Path.Combine("Assets", "Tremble Sync Settings.asset"));
#endif
			return s_TrembleSyncSettings;
		}

		// Show inline help
		[SerializeField] private bool m_ShowExpanded = true;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Basic Settings
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("Automagically keep project in-sync with TrenchBroom when compiling sources? Turn off if this is slow for your project.")]
		private bool m_AutoSyncOnCompile = true;

		[SerializeField, Tooltip("(Experimental!) Allow importing of maps in standalone/retail builds? Requires the Addressables package. Tremble will add Addressable Groups for your materials and entity prefabs to support in-game import, and will package up your entities.fgd and baseq3 data into your game.")]
		private bool m_AllowStandaloneImport = false;

		[SerializeField, Tooltip("TrenchBroom Version (2023 = GameConfig v4, 2024_1 = GameConfig v8, 2024_2, 2025_3 = GameConfig v9).")]
		private TrenchBroomVersion m_TrenchBroomVersion = TrenchBroomVersion.Version2025_3;

		public bool IsTrenchBroomVersionAtLeast(TrenchBroomVersion version) => (int)m_TrenchBroomVersion >= (int)version;

		[SerializeField, Tooltip("The map to use as a template when creating new ones. Leave blank to generate an empty map.")]
		private GameObject m_TemplateMap = null;

		[SerializeField, Tooltip("The Worldspawn script to use. Use this to set map-wide variables (such as difficulty level, etc.). This affects all maps in your game.")]
		private WorldspawnScript m_WorldspawnScript;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Naming Conventions
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("The naming convention to use for entity classes.")]
		private NamingConvention m_TypeNamingConvention = NamingConvention.PreserveExact;

		[SerializeField, Tooltip("The naming convention to use for variables in TrenchBroom.")]
		private NamingConvention m_FieldNamingConvention = NamingConvention.SnakeCase;

		[SerializeField, Tooltip("The naming convention to use for SpawnFlag variables in TrenchBroom.")]
		private NamingConvention m_SpawnFlagNamingConvention = NamingConvention.HumanFriendly;

		[SerializeField, Tooltip("The naming convention to use for materials in TrenchBroom.")]
		private NamingConvention m_MaterialNamingConvention = NamingConvention.PreserveExact;

		[SerializeField, Tooltip("The name of the property used to identify entities")]
		private string m_IdentityPropertyName = "targetname";

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Material Settings
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("How to export materials into groups. Leave the group list blank to export ALL materials in the project.")]
		private MaterialGroup[] m_MaterialGroups = Array.Empty<MaterialGroup>();
		//TODO(jwf): move into MRD below!

		[SerializeField, Tooltip("Render settings for materials")]
		private List<MaterialRenderData> m_MaterialRenderData = new();


		// -----------------------------------------------------------------------------------------------------------------------------
		//		Pipeline Settings
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("Split mesh into smaller meshes for better rendering performance? Turn off if not needed or too slow.")]
		private bool m_PipelineSplitMesh = true;

		[SerializeField, Tooltip("Smooth mesh normals based on map settings? Turn off for more of a low-poly aesthetic.")]
		private bool m_PipelineSmoothMeshNormals = true;

		[SerializeField, Tooltip("Simplify collisions to increase runtime performance? Turn off if your meshes don't need this.")]
		private bool m_PipelineSimplifyCollisionMeshes = true;

		[SerializeField, Tooltip("Unwrap UV2 so that meshes can be lightmapped? Turn off if your project doesn't use lightbaking.")]
		private bool m_PipelineUnwrapUV2 = true;

		[SerializeField, Tooltip("Run project-wide map processors? Per-map map processors are always run.")]
		private bool m_PipelineRunMapProcessors = true;

		[SerializeField, Tooltip("Which custom map processors to run. Use Unity 2022+ to enable a better UI for this!")]
		private MapProcessorEntry[] m_MapProcessors;


		// -----------------------------------------------------------------------------------------------------------------------------
		//		Advanced Settings
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("Print debug info when syncing and importing?")]
		private bool m_LogOnImportAndSync = false;

		[SerializeField, Tooltip("Allow live updating of maps while in playmode? Not all changes are supported.")]
		private bool m_UseLiveUpdate = true;

		[SerializeField, Tooltip("Whether to reimport maps when prefabs/materials inside them change?")]
		private bool m_AutomaticallyReimportWhenDependencyChanges = true;

		[SerializeField, Tooltip("Which kind of gizmos to add to Entities in editor?")]
		private EntityIconStyle m_EntityIconStyle = EntityIconStyle.ColouredDiamondShapes;

		[SerializeField, Tooltip("Allow prefab overrides on maps? This can cause hard-to-debug issues with materials and field values - but change this if you have a legitimate reason to use overrides (and know what you're doing!)")]
		private PrefabOverrideHandling m_PrefabOverrideHandling = PrefabOverrideHandling.AutomaticallyRevert;

		[SerializeField, Tooltip("Use class Quake-style culling? This removes all areas inaccessible by point entities, including exteriors. Maps without point/prefabs entities may import blank!")]
		private bool m_UseClassicQuakeCulling = false;

		[SerializeField, Tooltip("Under which circumstances would you like to see the output from Q3Map2? This can be used to debug map issues.")]
		private Q3Map2ResultDisplayType m_Q3Map2ResultDisplayType = Q3Map2ResultDisplayType.ShowWhenWarningsOccur;

		[SerializeField, Tooltip("Open Tremble's settings inside Project Settings by default?")]
		private bool m_EmbedTrembleSettingsInProjectSettings = true;

		[SerializeField, Tooltip("When writing the entities.fgd file for TrenchBroom, how should it be formatted? Very Verbose will be much slower to parse but great for debugging issues manually. We recommend Human Readable.")]
		private FgdFormattingStyle m_FgdFormattingStyle = FgdFormattingStyle.HumanReadable;

		[SerializeField, Tooltip("Subdivide worldspawn mesh down to the leaf level, ignoring surface area? This could be faster but less efficient at runtime.")]
		private bool m_SubdivideAllLeafs;

		[SerializeField, Tooltip("Always pack booleans into spawnflags? If off, booleans will separately appear as 0 or 1 values. You can use [SpawnFlags] or [NoSpawnFlags] to override this on a per-field basis.")]
		private bool m_AlwaysPackBoolsIntoSpawnFlags = true;

		[SerializeField, Tooltip("Show enum values as strings in TrenchBroom? Turn off to use ints, which is faster but a bit more clunky to work with.")]
		private bool m_SyncEnumsAsStringValue = true;

		[SerializeField, Tooltip("Show all [SerializeField]s in TrenchBroom by default? Use [NoTremble] to override this on a per-field basis, or [Tremble] to mark fields as map-serialisable.")]
		private bool m_SyncSerializedFields = true;

		[SerializeField, Tooltip("Automatically sync Unity Prefab and Material properties by default? Use [NoTremble] to override this on a per-field basis, or [Tremble] to mark fields as map-serialisable.")]
		private bool m_SyncMaterialsAndPrefabs = true;

		[SerializeField, Tooltip("The Unity layer to place Worldspawn brushes on.")]
		private string m_WorldspawnLayer = "Default";

		[SerializeField, Tooltip("Flatten entities and brushes instead of using the groups that were seen in the map?")]
		private bool m_DiscardMapGroups = false;

		[SerializeField, Tooltip("The name for the 'worldspawn' brush GameObject(s) in Unity.")]
		private string m_MainMeshName = "worldspawn";

		[SerializeField, Tooltip("Automagically remove TrenchBroom's autosave backups when syncing?")]
		private bool m_AutoDeleteMapBackups = false;

		[SerializeField, Tooltip("Extra commandline args to pass to Q3Map2 - see https://en.wikibooks.org/wiki/Q3Map2 for more info.")]
		private string m_ExtraCommandLineArgs = "-threads 8 -meta -v";

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Advanced Materials
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("Determines the base render resolution for materials rendering with Legacy Mode. PLEASE NOTE: Changing this size will affect UVing on your existing maps!")]
		private MaterialExportSize m_MaterialExportSize = MaterialExportSize.Size512;

		[SerializeField, Tooltip("The light intensity to use when capturing materials (default = 0.5)"), Range(0f, 1f)]
		private float m_MaterialCaptureLightIntensity = 0.5f;

		[SerializeField, Tooltip("The light angle to use when capturing materials. 0.0 means a perpendicular light, 1.0 means a parallel light. Purely parallel lights can 'blow out' materials with low roughness, but purely perpendicular lights will not illuminate your materials well. (default = 0.75)"), Range(0f, 1f)]
		private float m_MaterialCaptureLightAngle = 0.75f;

		[SerializeField, Tooltip("Export clip and skip textures?")]
		private bool m_ExportClipSkipTextures = true;


		// -----------------------------------------------------------------------------------------------------------------------------
		//		Import Scale
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, Tooltip("The scaling to use when importing maps. Default is 0.015625.")]
		private float m_ImportScale = 1f / 64f;



		// -----------------------------------------------------------------------------------------------------------------------------
		//		Addressables metadata storage
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField] private AssetMetadata[] m_AssetMetadatas = null;

		public AssetMetadata[] AssetMetadatas
		{
			get => m_AssetMetadatas;
#if UNITY_EDITOR
			set => m_AssetMetadatas = value;
#endif
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Public API
		// -----------------------------------------------------------------------------------------------------------------------------
		public bool ShowExpanded => m_ShowExpanded;

		// Basic Settings
		public bool AutoSyncOnCompile => m_AutoSyncOnCompile;
		public bool AllowStandaloneImport => m_AllowStandaloneImport;
		public TrenchBroomVersion TrenchBroomVersion => m_TrenchBroomVersion;
		public string TrenchBroomGameConfigVersion => TrenchBroomVersion switch
		{
			TrenchBroomVersion.Version2023_1 => "4",
			TrenchBroomVersion.Version2024_1 => "8",
			TrenchBroomVersion.Version2024_2 => "9",
			TrenchBroomVersion.Version2025_3 => "9",
			TrenchBroomVersion.VersionNext => "9",
			_ => "0"
		};
		public GameObject TemplateMap => m_TemplateMap;
		public WorldspawnScript WorldspawnScript => m_WorldspawnScript;

		// Naming Convention
		public NamingConvention TypeNamingConvention => m_TypeNamingConvention;
		public NamingConvention FieldNamingConvention => m_FieldNamingConvention;
		public NamingConvention SpawnFlagNamingConvention => m_SpawnFlagNamingConvention;
		public NamingConvention MaterialNamingConvention => m_MaterialNamingConvention;
		public string IdentityPropertyName => m_IdentityPropertyName;

		// Material Settings
		public MaterialGroup[] MaterialGroups => m_MaterialGroups;

		public List<MaterialGroup> GetMaterialGroupsOrDefault()
		{
			List<MaterialGroup> materialGroups = new();
			if (m_MaterialGroups is { Length: > 0 })
			{
				materialGroups.AddRange(m_MaterialGroups);
			}
			else
			{
				materialGroups.Add(MaterialGroup.CreateDefault());
			}

			return materialGroups;
		}

		public MaterialRenderData GetMaterialRenderData(Material material)
		{
			MaterialRenderData existing = m_MaterialRenderData.FirstOrDefault(mrd => mrd.Material == material);
			if (existing == null)
			{
				existing = new(material);
				m_MaterialRenderData.Add(existing);
			}

			return existing;
		}

		// Pipeline Settings
		public bool PipelineSplitMesh => m_PipelineSplitMesh;
		public bool PipelineSmoothMeshNormals => m_PipelineSmoothMeshNormals;

		public bool PipelineSimplifyCollisionMeshes => m_PipelineSimplifyCollisionMeshes;

		public bool PipelineUnwrapUV2 => m_PipelineUnwrapUV2;
		public bool PipelineRunMapProcessors => m_PipelineRunMapProcessors;
		public MapProcessorEntry[] MapProcessors => m_MapProcessors;
		public MapProcessorClass[] EnabledMapProcessors => m_MapProcessors == null ? Array.Empty<MapProcessorClass>() : m_MapProcessors.Where(mp => mp.Enabled).Select(mp => mp.Class).ToArray();


		// Advanced Settings
		public bool LogOnImportAndSync => m_LogOnImportAndSync;
		public bool UseLiveUpdate => m_UseLiveUpdate;
		public bool AutomaticallyReimportWhenDependencyChanges => m_AutomaticallyReimportWhenDependencyChanges;
		public EntityIconStyle EntityIconStyle => m_EntityIconStyle;
		public PrefabOverrideHandling PrefabOverrideHandling => m_PrefabOverrideHandling;
		public bool UseClassicQuakeCulling => m_UseClassicQuakeCulling;
		public Q3Map2ResultDisplayType Q3Map2ResultDisplayType => m_Q3Map2ResultDisplayType;
		public bool EmbedTrembleSettingsInProjectSettings => m_EmbedTrembleSettingsInProjectSettings;
		public FgdFormattingStyle FgdFormattingStyle => m_FgdFormattingStyle;
		public bool SubdivideAllLeafs => m_SubdivideAllLeafs;
		public bool AlwaysPackBoolsIntoSpawnFlags => m_AlwaysPackBoolsIntoSpawnFlags;
		public bool SyncEnumsAsStringValue => m_SyncEnumsAsStringValue;
		public bool SyncSerializedFields => m_SyncSerializedFields;
		public bool SyncMaterialsAndPrefabs => m_SyncMaterialsAndPrefabs;
		public string WorldspawnLayer => m_WorldspawnLayer;
		public bool DiscardMapGroups => m_DiscardMapGroups;
		public string MainMeshName => m_MainMeshName;
		public bool AutoDeleteMapBackups => m_AutoDeleteMapBackups;
		public string ExtraCommandLineArgs => m_ExtraCommandLineArgs;

		// Advanced materials
		public MaterialExportSize MaterialExportSize => m_MaterialExportSize;
		public float MaterialCaptureLightIntensity => m_MaterialCaptureLightIntensity;
		public float MaterialCaptureLightAngle => m_MaterialCaptureLightAngle;

		public bool ExportClipSkipTextures => m_ExportClipSkipTextures;

		// Prefixes and Import Scale
		public float ImportScale => m_ImportScale;
	}
}