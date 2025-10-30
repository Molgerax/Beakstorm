//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

using static TinyGoose.Tremble.Editor.TrembleSettingsGUI;

namespace TinyGoose.Tremble.Editor
{
	[CustomEditor(typeof(TrembleSyncSettings))]
	public class TrembleSyncSettingsEditor : UnityEditor.Editor
	{
		private SerializedObject m_SettingsObject;
		private SerializedObject m_DefaultObject;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Consts
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly string[] m_Tabs = { "About", "Config", "Naming", "Materials", "Advanced" };

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Lookups
		// -----------------------------------------------------------------------------------------------------------------------------
		private PrefabNameLookup m_PrefabNameLookup;
		private MapTypeLookup m_MapTypeLookup;
		private MaterialNameLookup m_MaterialNameLookup;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Getters
		// -----------------------------------------------------------------------------------------------------------------------------
		public bool IsAnyPropDirty => m_IsAnyPropDirty;

		public SerializedObject DefaultObject
		{
			get
			{
				if (m_DefaultObject == null || !m_DefaultObject.targetObject)
				{
					m_DefaultObject = new(CreateInstance<TrembleSyncSettings>());
				}

				return m_DefaultObject;
			}
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private int m_SelectedTab;
		private bool m_IsAnyPropDirty;

		private Vector2 m_ScrollPosition;

		private GUIStyle m_TitleStyle;
		private GUIStyle m_PipelineTitleStyle;
		private GUIStyle m_SmallTitleStyle;
		private GUIStyle m_LeftAlignedSmallStyle;
		private GUIStyle m_RightAlignedSmallStyle;
		private float m_OriginalLabelWidth;

		private string m_OriginalIdentityProperty;

		private void OnEnable()
		{
			VersionCheck.FetchLatestVersionsInBackground();
		}

		public void EnsureInit(bool onlyKeywordsCapture = false)
		{
			if (m_SettingsObject == null)
			{
				if (target)
				{
					m_SettingsObject = new(target);

					m_PrefabNameLookup = new((TrembleSyncSettings)target);
					m_MapTypeLookup = new((TrembleSyncSettings)target);
					m_MaterialNameLookup = new((TrembleSyncSettings)target);
				}

				if (m_SettingsObject == null)
				{
					Debug.LogError("Settings editor with no settings?");
					Label("Error...");
					return;
				}
			}

			m_SettingsObject.Update();

			if (onlyKeywordsCapture)
				return;

			m_TitleStyle ??= new(EditorStyles.boldLabel) { fontSize = EditorStyles.boldLabel.fontSize + 5 };
			m_PipelineTitleStyle ??= new(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = EditorStyles.boldLabel.fontSize + 2 };
			m_SmallTitleStyle ??= new(EditorStyles.boldLabel) { fontSize = EditorStyles.boldLabel.fontSize + 3 };
			m_LeftAlignedSmallStyle ??= new(EditorStyles.label) { fontSize = EditorStyles.label.fontSize - 2, alignment = TextAnchor.MiddleLeft };
			m_RightAlignedSmallStyle ??= new(EditorStyles.label) { fontSize = EditorStyles.label.fontSize - 2, alignment = TextAnchor.MiddleRight};

			bool isEmbedded = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.EmbedTrembleSettingsInProjectSettings)).boolValue;

			m_OriginalLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = isEmbedded ? 275f : m_OriginalLabelWidth * 1.2f;
		}

		public void ResetLabelWidth(bool onlyKeywordsCapture = false)
		{
			if (onlyKeywordsCapture)
				return;

			EditorGUIUtility.labelWidth = m_OriginalLabelWidth;

			m_SettingsObject?.ApplyModifiedProperties();
		}

		public override void OnInspectorGUI()
		{
			EnsureInit();

			m_SelectedTab = GUILayout.Toolbar(m_SelectedTab, m_Tabs, "LargeButton");

			using (EditorGUILayout.ScrollViewScope scroll = new(m_ScrollPosition))
			{
				EditorGUILayoutUtil.Pad(10f, () =>
				{
					switch (m_SelectedTab)
					{
						case 0: OnAboutGUI(); break;
						case 1: OnSettingsGUI(); break;
						case 2: OnNamingConventionsGUI(); break;
						case 3: OnMaterialsGUI(); break;
						case 4: OnAdvancedGUI(); break;
					}
				});
				m_ScrollPosition = scroll.scrollPosition;
			}

			ResetLabelWidth();
		}

		public void OnAboutGUI()
		{
			Space(10f);

			Texture2D logoTexture = TrembleAssetLoader.LoadAssetByName<Texture2D>("T_TrembleLogo");
			float width = Screen.width / EditorGUIUtility.pixelsPerPoint;
			float useWidth = Mathf.Min(200f, width);

			// Logo
			BeginHorizontal();
			{
				Image(logoTexture, useWidth, useWidth);
				BeginVertical(GUILayout.ExpandWidth(true));
				{
					Space(20f);

					Label($"Tremble version {TrembleConsts.VERSION_STRING}", m_TitleStyle);
					Label($"for {TrembleConsts.GAME_NAME}");

					Space(10f);

					Label("(c) 2024-2025 Tiny Goose & contributors", EditorStyles.wordWrappedMiniLabel);
					Label("Special thanks to our community members: Andicraft, bill1487, Tinog, xage & everyone from our Discord server!", EditorStyles.wordWrappedMiniLabel);

					Space(20f);

					if (VersionCheck.IsNewerVersionAvailable)
					{
						UseColour(Color.green, () =>
						{
							Label($"A newer version of Tremble is available!!");
							Label($"Open Window > Package Manager to update to v{VersionCheck.NewestAvailableVersion.Version}.");
							if (Button("Open Package Manager"))
							{
								EditorApplication.ExecuteMenuItem("Window/Package Manager");
							}
						});
					}
				}
				EndVertical();
			}
			EndHorizontal();

			Space(80f);

			// Discord & other links
			Label("Support Links", m_SmallTitleStyle);
			Space(10f);

			LabelledHyperlink("Read the manual", "Open in Unity", () => TrembleEditorAPI.OpenManual());
			LabelledHyperlink("Visit our Discord server", "discord.gg/aUTmYxbVHZ", "https://discord.gg/aUTmYxbVHZ");
			LabelledHyperlink("Email support", "hello@tinygoose.com", "mailto:hello@tinygoose.com");

			Space(40f);

			string sampleFolder = Path.Combine(TrembleConsts.EDITOR_GetTrembleInstallFolder(), "Sample");
			if (Directory.Exists(sampleFolder))
			{
				Label("Sample Content", m_SmallTitleStyle);
				Label("Your project contains Tremble sample contents (materials, prefabs, scripts, etc.)");
				Label("You may want to delete these when you are done with them!");

				if (Button("Delete Sample Contents"))
				{
					Directory.Delete(sampleFolder, true);
					File.Delete(sampleFolder + ".meta");

					AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
					TrembleEditorAPI.SyncToTrenchBroom();
				}
			}

			Space(40f);

			// Acknowledgements
			Label("Acknowledgements", m_SmallTitleStyle);
			string[] acknowledgements =
			{
				"Tremble would not be possible without Q3Map2, licensed under the GPL Licence. Original Q3Map software by id Software.",
				"Tremble was originally built upon the BSP import and MD3 model export functionality found in 'UBSP Map Tools for Unity' by John Evans (evans3d512@gmail.com).",
				"Tremble is not affiliated with, or sponsored by the authors of TrenchBroom or id Software.",
				"",
				"Thanks for buying Tremble!"
			};
			Label(String.Join('\n', acknowledgements), EditorStyles.wordWrappedMiniLabel);

			Space(10f);

			BeginHorizontal();
			{
				FlexibleSpace();

				SerializedProperty expandedProp = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.ShowExpanded));
				expandedProp.boolValue = Toggle(expandedProp.boolValue, new("Show Inline Help", "Show help boxes for new users - disable to reduce clutter"));
			}
			EndHorizontal();
		}
		public void OnSettingsGUI(bool isEmbedded = false)
		{
			Title("Basic Settings",
				helpText: "The default settings should be fine for most users.",
				isEmbedded: isEmbedded);

			Property(nameof(TrembleSyncSettings.AutoSyncOnCompile),
				overrideLabel: "Automatic Sync",
				off: "Manual sync only (from toolbar button)", on: "Sync to TrenchBroom whenever C# code changes");

#if !ADDRESSABLES_INSTALLED
			UseDisabled(true, () =>
			{
#endif

			Property(nameof(TrembleSyncSettings.AllowStandaloneImport),
				overrideLabel: "User-Generated Content",
				off: "Developer Only - Do not allow User-Generated Content", on: "Developer & Players - Allow import of User-Generated Content)",

				extraContents: () =>
				{
#if !ADDRESSABLES_INSTALLED
					UseDisabled(false, () =>
					{
						UseColour(Color.red, ()=>
						{
							HelpBox("You must install 'Addressables' from the Unity Package Manager to use this setting!", MessageType.Error);
						});
					});
#endif
				}
			);

#if !ADDRESSABLES_INSTALLED
			});
#endif

			Space(20f);

			Property(nameof(TrembleSyncSettings.TrenchBroomVersion), overrideLabel: "TrenchBroom Version");

			// Validate template map
			GameObject defaultMapObject = (GameObject)m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.TemplateMap)).objectReferenceValue;
			if (defaultMapObject && !defaultMapObject.TryGetComponent(out MapDocument _))
			{
				Property(nameof(TrembleSyncSettings.TemplateMap),
					extraContents: () =>
					{
						UseColour(new(1f, 0.4f, 0.4f), () =>
						{
							HelpBox($"{defaultMapObject.name} is NOT a valid map and will not be used!", MessageType.Error);
						});
					});
			}
			else
			{
				Property(nameof(TrembleSyncSettings.TemplateMap));
			}

			Property(nameof(TrembleSyncSettings.WorldspawnScript), overrideLabel: "Worldspawn Script Class");
		}

		public void OnNamingConventionsGUI(bool isEmbedded = false)
		{
			Title("Naming Conventions",
				helpText: "Here you can set how Tremble maps Unity naming to TrenchBroom. " +
				          "Use this to follow a more standard Quake-style naming convention, " +
				          "or use a more Unity-style convention.",
				isEmbedded: isEmbedded);

			Span<Color> pastelColours = stackalloc[]
			{
				new Color(1f, 0.6f, 0.6f),
				new Color(1f, 1f, 0.6f),
				new Color(0.6f, 1f, 0.6f),
				new Color(0.6f, 0.8f, 1f),
			};

			UseColour(pastelColours[0], () => Property(nameof(TrembleSyncSettings.TypeNamingConvention), overrideLabel: "Entity Class Names"));
			UseColour(pastelColours[1], () => Property(nameof(TrembleSyncSettings.FieldNamingConvention), overrideLabel: "Field Names"));
			UseColour(pastelColours[2], () => Property(nameof(TrembleSyncSettings.SpawnFlagNamingConvention), overrideLabel: "Spawnflag Names", allowHumanFriendly: true));
			UseColour(pastelColours[3], () => Property(nameof(TrembleSyncSettings.MaterialNamingConvention), overrideLabel: "Material Names", allowHumanFriendly: true));

			if (TrembleSyncSettings.Get().IsTrenchBroomVersionAtLeast(TrenchBroomVersion.VersionNext))
			{
				string identityProperty = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.IdentityPropertyName)).stringValue;
				m_OriginalIdentityProperty ??= identityProperty;

				bool isIdentityPropertyChanged = m_OriginalIdentityProperty != null && !identityProperty.Equals(m_OriginalIdentityProperty);
				Property(nameof(TrembleSyncSettings.IdentityPropertyName), overrideTooltip: isIdentityPropertyChanged
					? $"The name of the property used to identify entities (NOTE: you will need to manually change any of your maps that are still using '{m_OriginalIdentityProperty}' to use '{identityProperty}'!)"
					: null);
			}

			Separator(2f);

			// Examples
			Title("Examples", helpText: "Here are some examples of how your settings might look:");

			string[] classExamples = m_PrefabNameLookup.AllPrefabPaths
				.Take(2)
				.Select(Path.GetFileNameWithoutExtension)
				.Concat(m_MapTypeLookup.AllTypes
					.Take(2)
					.Select(t => t.Name))
				.ToArray();

			string[] fieldsExamples = { "m_NumberOfEggs", "m_eggCount", "_eggs", "spawnEggs" };

			string[] spawnflagsExamples = { "m_dangerousObject", "m_couldEatYou", "_scaryMode", "mightEmitSmoke" };
			string suffix = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.SpawnFlagNamingConvention)).enumValueIndex == (int)NamingConvention.HumanFriendly
				? "?"
				: "";

			string[] materialExamples = m_MaterialNameLookup.AllMaterialPaths
				.Take(4)
				.Select(Path.GetFileNameWithoutExtension)
				.ToArray();

			UseColour(pastelColours[0], () => RenderNamingExamples("Entity Classes", classExamples, nameof(TrembleSyncSettings.TypeNamingConvention)));
			UseColour(pastelColours[1], () => RenderNamingExamples("Fields/Properties", fieldsExamples, nameof(TrembleSyncSettings.FieldNamingConvention)));
			UseColour(pastelColours[2], () => RenderNamingExamples("Spawnflags Variables", spawnflagsExamples, nameof(TrembleSyncSettings.SpawnFlagNamingConvention), suffix: suffix));
			UseColour(pastelColours[3], () => RenderNamingExamples("Materials", materialExamples, nameof(TrembleSyncSettings.MaterialNamingConvention)));
		}


		public void OnMaterialsGUI(bool isEmbedded = false)
		{
			SerializedProperty materialGroupsProp = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.MaterialGroups));

			MaterialGroup defaultMaterialGroup = MaterialGroup.CreateDefault();
			int numMaterials = defaultMaterialGroup.Materials.Length;
			int numGroups = materialGroupsProp.arraySize;

			int totalGroupedMaterials = 0;
			for (int i = 0; i < numGroups; i++)
			{
				SerializedProperty matGroup = materialGroupsProp.GetArrayElementAtIndex(i);
				totalGroupedMaterials += matGroup.FindPropertyRelative("Materials").arraySize;
			}

			Title("Material Settings",
				helpText: $"Controls how your project's {numMaterials} materials are synced with TrenchBroom.",
				isEmbedded: isEmbedded);
			{
				string helpBoxText = numGroups > 0
					? $"{totalGroupedMaterials} of your project's {numMaterials} compatible materials will be exported to TrenchBroom, in {numGroups} folders named after your material groups."
					: $"{numMaterials} compatible materials will be exported to TrenchBroom, in a single texture folder.";

				for (int i = 0; i < numGroups; i++)
				{
					SerializedProperty matGroup = materialGroupsProp.GetArrayElementAtIndex(i);
					string groupName = matGroup.FindPropertyRelative("Name").stringValue;
					int numGroupMaterials = matGroup.FindPropertyRelative("Materials").arraySize;

					helpBoxText += $"\n - Group '{groupName}' with {numGroupMaterials} material(s)";
				}

				HelpBox(helpBoxText, MessageType.Info);
				Space(10f);

				Property
				(
					nameof(TrembleSyncSettings.MaterialGroups),
					displayVertically: true,
					overrideTooltip: ""
				);
			}
		}

		public void OnPipelineGUI(bool isEmbedded = false)
		{
			Label("This page shows you how Tremble will import your map. You can turn off pipeline " +
			     "stages that you don't require to speed up map import.", EditorStyles.wordWrappedLabel);

			PipelineHeader("On Map Import");
			PipelineArrow();
			PipelineStage("Split Mesh", TrembleTimer.Context.SplitMesh, nameof(TrembleSyncSettings.PipelineSplitMesh));
			PipelineArrow();
			PipelineStage("Smooth Mesh Normals", TrembleTimer.Context.SmoothMeshNormals, nameof(TrembleSyncSettings.PipelineSmoothMeshNormals));
			PipelineArrow();
			PipelineStage("Simplify Collision Meshes", TrembleTimer.Context.SimplifyCollisionMeshes, nameof(TrembleSyncSettings.PipelineSimplifyCollisionMeshes));
			PipelineArrow();
			PipelineStage("Generate UV2 Lightmaps", TrembleTimer.Context.GenerateUV2, nameof(TrembleSyncSettings.PipelineUnwrapUV2));
			PipelineArrow();
			PipelineStage("Run Map Processors", TrembleTimer.Context.RunMapProcessors, nameof(TrembleSyncSettings.PipelineRunMapProcessors), extraContents: () =>
			{
#if UNITY_2022_1_OR_NEWER
				SerializedProperty processorsArray = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.MapProcessors));

				TypeCache.TypeCollection allProcessorTypes = TypeCache.GetTypesDerivedFrom<MapProcessorBase>();
				HashSet<Type> typesInArray = new(allProcessorTypes.Count);

				// Show enabled types
				for (int i = 0; i < processorsArray.arraySize; i++)
				{
					if (processorsArray.GetArrayElementAtIndex(i).boxedValue is not MapProcessorEntry entry || !entry.Class.IsValid)
					{
						// Broken or invalid - remove and roll back
						processorsArray.DeleteArrayElementAtIndex(i);
						i--;

						continue;
					}

					PipelineMapProcessor(entry.Class.Class, processorsArray, entry.Enabled, i);
					typesInArray.Add(entry.Class.Class);
				}

				// Show enableable type
				foreach (Type type in allProcessorTypes)
				{
					if (typesInArray.Contains(type))
						continue;

					PipelineMapProcessor(type, processorsArray, false);
				}

				Label("Note: Map Processors are run concurrently as entities are discovered in the map, rather " +
				      "than one after the other. The order dictates which processor sees each entity first.",
					style: EditorStyles.wordWrappedMiniLabel);
#else
				// Unity 2021 and below can't do boxedValue :( - at least show the array!
				Property(nameof(TrembleSyncSettings.MapProcessors));
#endif
			});

			PipelineArrow();
			PipelineHeader("Done");
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Pipeline stuff
		// -----------------------------------------------------------------------------------------------------------------------------
		private void PipelineArrow() => Label("↓", EditorStyles.centeredGreyMiniLabel);

#if UNITY_2022_1_OR_NEWER
		private void PipelineMapProcessor(Type mapProcessorClass, SerializedProperty array, bool isEnabled, int currentIdx = -1)
		{
			string friendlyName = mapProcessorClass.Name;
			if (friendlyName.EndsWithInvariant("MapProcessor", caseSensitive: true))
			{
				friendlyName = friendlyName.Substring(0, friendlyName.Length - 12);
			}

			string description = null;
			try
			{
				PropertyInfo propertyInfo = mapProcessorClass.GetProperty("Description");
				description = (string)propertyInfo?.GetValue(mapProcessorClass, null);
			}
			catch (Exception) { /* ignored */ }

			long lastRuntime = TrembleTimer.GetLastTime(mapProcessorClass);

			if (currentIdx > -1)
			{
				// Existing processor
				PipelineStageImpl(friendlyName.ToNamingConvention(NamingConvention.HumanFriendly), description, lastRuntime, isEnabled,
					isMapProcessor: true,
					onSetActive: newActive =>
					{
						MapProcessorEntry entry = (MapProcessorEntry)array.GetArrayElementAtIndex(currentIdx).boxedValue;
						entry.Enabled = newActive;
						array.GetArrayElementAtIndex(currentIdx).boxedValue = entry;

						m_SettingsObject.ApplyModifiedProperties();
					},
					onReorder: offset =>
					{
						int newIdx = currentIdx + offset;
						if (newIdx < 0)
							return;

						if (newIdx >= array.arraySize)
							return;

						array.MoveArrayElement(currentIdx, newIdx);
						m_SettingsObject.ApplyModifiedProperties();
					});
			}
			else
			{
				PipelineStageImpl(friendlyName.ToNamingConvention(NamingConvention.HumanFriendly), description, lastRuntime, isEnabled,
					isMapProcessor: true,
					onSetActive: newActive =>
					{
						if (newActive)
						{
							array.AppendArrayElement().boxedValue = new MapProcessorEntry()
							{
								Class = mapProcessorClass,
								Enabled = true
							};
						}
					});
			}
		}
#endif

		private void PipelineHeader(string header) => PipelineStageImpl(header, null, -1L, false, null);

		private void PipelineStage(string label, TrembleTimer.Context timerContext, string pipelinePropertyName, Action extraContents = null)
		{
			SerializedProperty prop = m_SettingsObject.FindBackedProperty(pipelinePropertyName);
			PipelineStageImpl(label, prop.tooltip, TrembleTimer.GetLastTime(timerContext), prop.boolValue, v =>
			{
				prop.boolValue = v;
				m_SettingsObject.ApplyModifiedProperties();
			}, extraContents: extraContents);
		}

		private void PipelineStageImpl(string label, string description, long lastRuntime, bool isActive, Action<bool> onSetActive, bool isMapProcessor = false, Action<int> onReorder = null, Action extraContents = null)
		{
			if (onSetActive == null)
			{
				GUI.color = Color.white;
			}
			else
			{
				bool disableDueToMapProcessor = isMapProcessor && !TrembleSyncSettings.Get().PipelineRunMapProcessors;
				GUI.color = isActive && !disableDueToMapProcessor ? Color.green : Color.grey;
			}

			BeginVertical(EditorStyles.helpBox);
			{
				BeginHorizontal();
				{
					if (onSetActive != null)
					{
						onSetActive(Toggle(isActive, GUIContent.none, GUILayout.Width(10f)));
					}

					BeginVertical(GUILayout.ExpandWidth(true));
					{
						StringBuilder finalLabel = new();
						if (!isActive && onSetActive != null)
						{
							finalLabel.Append("(skipped) ");
						}

						finalLabel.Append(label);

						if (lastRuntime != -1L)
						{
							finalLabel.Append($" (last run: {lastRuntime}ms)");
						}

						Label(finalLabel.ToString(), m_PipelineTitleStyle, GUILayout.ExpandWidth(true));

						if (description != null)
						{
							Label(description, EditorStyles.centeredGreyMiniLabel);
						}
					}
					EndVertical();

					if (onReorder != null)
					{
						BeginVertical(GUILayout.Width(20f));
						{
#if UNITY_2023_1_OR_NEWER
							if (Button("⬆")) { onReorder(-1); }
							if (Button("⬇")) { onReorder(+1); }
#else
							if (Button("^")) { onReorder(-1); }
							if (Button("v")) { onReorder(+1); }
#endif
						}
						EndVertical();
					}
				}
				EndHorizontal();

				extraContents?.Invoke();
			}
			EndVertical();

			// Also allow clicks inside the entire block!
			if (onSetActive != null)
			{
				MakeLastRectClickable(() => onSetActive(!isActive));
			}

			GUI.color = Color.white;
		}

		public void OnAdvancedGUI()
		{
			Title("Advanced Settings",
				helpText: "Here be dragons! These are way more advanced options, which " +
				          "most users will not need to care about!");

			Foldout("Features", OnAdvancedGUI_Features);
			Foldout("Debugging", OnAdvancedGUI_Debugging);
			Foldout("Sync/Import", OnAdvancedGUI_SyncImport);
			Foldout("Materials", OnAdvancedGUI_Materials);
			Foldout("Import Scale", OnAdvancedGUI_ImportScale);
		}

		public void OnAdvancedGUI_Features()
		{
			Property(nameof(TrembleSyncSettings.UseLiveUpdate),
				overrideLabel: "Live Update",
				off: "Don't update maps during playmode", on: "Update maps even during playmode (experimental!)",
				extraContents: () =>
				{
					int autoRefreshMode = EditorPrefs.GetInt("kAutoRefreshMode");
					if (autoRefreshMode != 1)
					{
						UseColour(Color.yellow, () =>
						{
#if UNITY_EDITOR_OSX
							const string CTRL_TEXT = "Cmd";
#else
							const string CTRL_TEXT = "Ctrl";
#endif

							HelpBox("Your project has Auto Refresh disabled. " +
							                    "To see map changes during playmode, you will need " +
							                    $"to press {CTRL_TEXT}+R to refresh the map. Alternatively, you " +
							                    "can enable Auto Refresh in Editor Preferences > Asset Pipeline.", MessageType.Warning);
						});
					}
				}
			);

			Property(nameof(TrembleSyncSettings.EmbedTrembleSettingsInProjectSettings),
				off: "Show Tremble settings as a separate window (legacy)", on: "Embed Tremble's settings into Project Settings",
				onChanged: () =>
				{
					TrembleSyncSettingsWindow window = EditorWindow.GetWindow<TrembleSyncSettingsWindow>();
					if (window)
					{
						window.Close();
					}

					TrembleEditorAPI.OpenSettings();
				});

			Property(nameof(TrembleSyncSettings.AutomaticallyReimportWhenDependencyChanges),
				overrideLabel: "Automatic Map Re-Import",
				off: "Don't re-import, just show out of date icons", on: "Re-import maps when Prefabs or Materials inside are changed");

			Property(nameof(TrembleSyncSettings.EntityIconStyle), overrideLabel: "Gizmos for Entities in Editor");

			Property(nameof(TrembleSyncSettings.PrefabOverrideHandling), overrideLabel: "Handling of Prefab Overrides");
		}
		public void OnAdvancedGUI_Debugging()
		{
			Property(nameof(TrembleSyncSettings.LogOnImportAndSync),
				overrideLabel: "Log During Import/Sync",
				off: "Don't log when importing or syncing", on: "Output timing messages when importing or syncing");

			Property(nameof(TrembleSyncSettings.Q3Map2ResultDisplayType),
				overrideLabel: "Show Q3Map2 Result");

			Property(nameof(TrembleSyncSettings.FgdFormattingStyle));

			Property(nameof(TrembleSyncSettings.SubdivideAllLeafs),
				overrideLabel: "(Debug) Map Subdivision Mode",
				off: "Standard", on: "Subdivide down to BSP leafs (inefficient)");
		}
		public void OnAdvancedGUI_SyncImport()
		{
			Property(nameof(TrembleSyncSettings.AlwaysPackBoolsIntoSpawnFlags),
				overrideLabel: "Boolean Fields",
				off: "Expose bool fields as separate booleans", on: "Pack all bool fields into spawnflags");

			Property(nameof(TrembleSyncSettings.SyncEnumsAsStringValue),
				overrideLabel: "Enum Fields",
				off: "Store enums as integers", on: "Store enums as strings");

			Property(nameof(TrembleSyncSettings.SyncMaterialsAndPrefabs),
				overrideLabel: "Material & Prefab Fields",
				off: "Don't expose Material & Prefab fields", on: "Expose Material & Prefab fields");

			Property(nameof(TrembleSyncSettings.SyncSerializedFields),
				overrideLabel: "Other Serialised Fields",
				off: "Only expose fields explicitly marked with [Tremble]", on: "Expose all [SerializedField] and public fields");

			Space(20f);

			Property(nameof(TrembleSyncSettings.DiscardMapGroups),
				overrideLabel: "Map Groups",
				off: "Preserve layers & groups from the map", on: "Flatten layers & groups");

			Space(20f);

			Property(nameof(TrembleSyncSettings.WorldspawnLayer),
				displayAsLayerMask: true,
				overrideLabel: "Worldspawn Layer");

			Property(nameof(TrembleSyncSettings.MainMeshName),
				overrideLabel: "Worldspawn GameObject Name");

			Property(nameof(TrembleSyncSettings.UseClassicQuakeCulling),
				off: "(Tremble Default) Prevent entire map culling, even with no point entities",
				on: "Cull areas inaccessible by point entities, including exteriors. Entire map is culled without point entities!");

			Space(20f);

			Property(nameof(TrembleSyncSettings.AutoDeleteMapBackups),
				overrideLabel: "TrenchBroom Autosaves",
				off: "Allow TrenchBroom to create autosave backup files inside your project", on: "Remove TrenchBroom's autosave backup files from your project");

			Property(nameof(TrembleSyncSettings.ExtraCommandLineArgs),
				overrideLabel: "Extra Command Line Args for Q3Map2");
		}
		public void OnAdvancedGUI_Materials()
		{
			Label("Use these sliders to control how Tremble renders your Unity materials " +
			                "into textures for TrenchBroom. This can fix 'blown-out' or dimly-lit " +
			                "texture appearance in TrenchBroom.", EditorStyles.wordWrappedLabel);

			Space(10f);

			Property(nameof(TrembleSyncSettings.MaterialCaptureLightIntensity), onChanged: TrembleEditorAPI.InvalidateMaterialAndPrefabCache);
			Property(nameof(TrembleSyncSettings.MaterialCaptureLightAngle), onChanged: TrembleEditorAPI.InvalidateMaterialAndPrefabCache);

			Space(20f);

			Label("These settings control advanced material settings.");

			Space(10f);

			Property(nameof(TrembleSyncSettings.MaterialExportSize), onChanged: TrembleEditorAPI.InvalidateMaterialAndPrefabCache);
			Property(nameof(TrembleSyncSettings.ExportClipSkipTextures));
		}
		public void OnAdvancedGUI_ImportScale()
		{
			Label("Change how TrenchBroom units map to Unity metres. By default, 1m = 64 map units.", EditorStyles.wordWrappedLabel);
			Space(10f);

			SerializedProperty scale = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.ImportScale));
			float oneMetre = 1f / scale.floatValue;

			BeginHorizontal();
			{
				Label("1 metre", GUILayout.Width(80f));
				Label("=", GUILayout.Width(20f));
				oneMetre = FloatField(oneMetre);
				Label("map unit(s)", GUILayout.Width(80f));
			}
			EndHorizontal();

			Label("or", EditorStyles.centeredGreyMiniLabel);

			BeginHorizontal();
			{
				Label("64 map units", GUILayout.Width(80f));
				Label("=", GUILayout.Width(20f));
				oneMetre = FloatField(oneMetre / 64f) * 64f;
				Label("metre(s)", GUILayout.Width(80f));
			}
			EndHorizontal();

			scale.floatValue = 1f / oneMetre;
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Internals
		// -----------------------------------------------------------------------------------------------------------------------------
		private void Title(string title, string helpText = null, bool isEmbedded = false)
		{
			if (!isEmbedded)
			{
				Space(10f);
				Label(title, m_TitleStyle);
			}

			if (helpText != null)
			{
				Label(helpText, EditorStyles.wordWrappedLabel);
			}

			Space(20f);
		}

		private void RenderNamingExamples(string title, string[] examples, string namingConventionPropName, string suffix = "")
		{
			Space(2f);
			Label(title);

			Indent(() =>
			{
				if (examples.Length == 0)
				{
					Label("No examples found in your project!");
				}
				else
				{
					NamingConvention naming = (NamingConvention)m_SettingsObject.FindBackedProperty(namingConventionPropName).enumValueIndex;

					foreach (string example in examples)
					{
						string rawExample = naming == NamingConvention.PreserveExact
							? example
							: RemovePrefixes(example, "M_", "P_", "m_", "_");

						BeginHorizontal();
						{
							Space(30f);
							Label($"(Unity) '{example}'", m_RightAlignedSmallStyle, GUILayout.Width(100f), GUILayout.ExpandWidth(true));
							Label("->", EditorStyles.centeredGreyMiniLabel, GUILayout.Width(40f));
							Label($"'{rawExample.ToNamingConvention(naming)}{suffix}' (TrenchBroom)", m_LeftAlignedSmallStyle, GUILayout.Width(100f), GUILayout.ExpandWidth(true));
						}
						EndHorizontal();
					}
				}
			});
		}

		private string RemovePrefix(string input, string prefix) => input.StartsWith(prefix) ? input.Substring(prefix.Length) : input;
		private string RemovePrefixes(string input, params string[] prefixes)
		{
			foreach (string prefix in prefixes)
			{
				input = RemovePrefix(input, prefix);
			}

			return input;
		}

		private void Property(string propertyName,
			string overrideLabel = null, string overrideTooltip = null,
			Action extraContents = default, Action onChanged = default,
			bool allowHumanFriendly = false,
			string on = null, string off = null,
			bool displayVertically = false, bool displayAsLayerMask = false)
		{
			bool isExpanded = m_SettingsObject.FindBackedProperty(nameof(TrembleSyncSettings.ShowExpanded)).boolValue;

			SerializedProperty sp = m_SettingsObject.FindBackedProperty(propertyName);
			SerializedProperty defaultProp = DefaultObject.FindBackedProperty(propertyName);
			uint oldValueHash = sp.GetHashOfContent();
			string useTooltip = overrideTooltip ?? sp.tooltip;

			// Naming convention selection - show in human-friendly format
			if (sp.propertyType == SerializedPropertyType.Enum && sp.name.ContainsInvariant("NamingConvention", caseSensitive: true))
			{
				// Special-case: Show naming convention in actual naming convention
				List<string> names = new() { "snake_case", "UpperCamelCase", "lowerCamelCase", "(Preserve Exact Name)" };

				if (allowHumanFriendly)
				{
					names.Add("Human Friendly");
				}

				BeginHorizontal();

				PrefixLabel(overrideLabel ?? sp.displayName, useTooltip);
				if (defaultProp != null)
				{
					AddResetButton(sp.enumValueIndex == defaultProp.intValue, () => sp.enumValueIndex = defaultProp.intValue);
				}

				if (displayVertically)
				{
					EndHorizontal();
				}

				sp.enumValueIndex = Popup(sp.enumValueIndex, names.ToArray(), EditorStyles.popup);

				if (!displayVertically)
				{
					EndHorizontal();
				}
			}
			// Show booleans as dropdowns if we passed on/off labels
			else if (sp.propertyType == SerializedPropertyType.Boolean && on != null && off != null)
			{
				BeginHorizontal();

				bool wasDefaulted = false;

				PrefixLabel(overrideLabel ?? sp.displayName, useTooltip);

				if (defaultProp != null)
				{
					AddResetButton(sp.boolValue == defaultProp.boolValue, () =>
					{
						sp.boolValue = defaultProp.boolValue;
						wasDefaulted = true;
					});
				}

				if (displayVertically)
				{
					EndHorizontal();
				}

				sp.boolValue = wasDefaulted ? defaultProp.boolValue : Popup(sp.boolValue ? 1 : 0, new[]{ off, on }) == 1;

				if (!displayVertically)
				{
					EndHorizontal();
				}
			}
			// Layermask dropdown, if needed
			else if (displayAsLayerMask)
			{
				string[] layerNames = Enumerable.Range(0, 32).Select(LayerMask.LayerToName).ToArray();
				int currentLayer = LayerMask.NameToLayer(sp.stringValue);

				BeginHorizontal();

				PrefixLabel(overrideLabel ?? sp.displayName, useTooltip);

				bool wasDefaulted = false;

				if (defaultProp != null)
				{
					AddResetButton(sp.stringValue == defaultProp.stringValue, () =>
					{
						wasDefaulted = true;
						sp.stringValue = defaultProp.stringValue;
					});
				}

				if (displayVertically)
				{
					EndHorizontal();
				}

				int layerIndex = Popup(currentLayer, layerNames);
				sp.stringValue = LayerMask.LayerToName(wasDefaulted ? 0 : layerIndex);

				if (!displayVertically)
				{
					EndHorizontal();
				}
			}
			// Nothing special: draw normal one
			else
			{
				// All else: use normal property field
				BeginHorizontal();

				PrefixLabel(overrideLabel ?? sp.displayName, useTooltip);
				if (defaultProp != null)
				{
					AddResetButton(sp.EqualByValue(defaultProp), () => sp.SetTo(defaultProp));
				}

				if (displayVertically)
				{
					EndHorizontal();
				}

				PropertyField(sp, GUILayout.ExpandWidth(true));

				if (!displayVertically)
				{
					EndHorizontal();
				}
			}

			// Draw help boxes if in beginner mode
			if (isExpanded && !useTooltip.IsNullOrEmpty())
			{
				BeginHorizontal();
				Space(EditorGUIUtility.labelWidth + 20f + 5f); // 20f = reset button
				Label(useTooltip, EditorStyles.helpBox);
				EndHorizontal();
			}

			// Extra contents, if any
			extraContents?.Invoke();

			// On changed, if set
			if (sp.GetHashOfContent() != oldValueHash)
			{
				m_SettingsObject.ApplyModifiedProperties();
				onChanged?.Invoke();
				m_IsAnyPropDirty = true;
			}

			// Spacing
			Space(isExpanded ? 15f : 5f);
		}

		private void AddResetButton(bool isDefault, Action setDefault)
		{
			GUIContent icon = isDefault ? new(" ") : EditorGUIUtility.IconContent("preAudioLoopOff");
			icon.tooltip = "Reset to default value";

			UseColour(Color.yellow, () =>
			{
				if (Button(icon, EditorStyles.label, GUILayout.Width(20f)))
				{
					setDefault();
				}
			});
		}
	}
}