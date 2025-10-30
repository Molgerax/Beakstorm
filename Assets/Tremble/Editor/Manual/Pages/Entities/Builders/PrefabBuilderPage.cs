//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Entities/Prefab Entity Builder", showInTree: false)]
	public class PrefabBuilderPage : BuilderPageBase<PrefabBuilderPage.SampleData>
	{
		private static readonly Dictionary<Type, string> s_ScriptFileLookup = new();

		protected override bool HasInitialQuestions => true;

		private bool m_MonoBehaviourListDirty = false;
		private readonly List<Type> m_CandidateTypes = new();

		private readonly TrembleFieldConverterCollection m_FieldConverterCollection = new();

		private Type m_UnityScriptType;
		private string m_NewUnityScriptName;
		private Action m_ResolutionAction;

		private bool m_IsOnlyAddingSpawnablePrefab = false;

		private bool m_IsAlreadyTrembleSpawnable = false;
		private bool m_IsAlreadyPrefabEntity = false;
		private bool m_IsMap = false;

		private bool IsNewScript => !m_NewUnityScriptName.IsNullOrEmpty();
		private string ScriptName => m_UnityScriptType?.Name ?? m_NewUnityScriptName;

		public class SampleData : ScriptableObject
		{
			[SerializeField] internal GameObject m_BasePrefab;
			[SerializeField] internal bool m_WantsData;

			[SerializeField] internal string m_Category = "";
			[SerializeField] internal GameObject[] m_ExcludedPrefabs = Array.Empty<GameObject>();
			[SerializeField] internal GameObject[] m_IncludedPrefabs = Array.Empty<GameObject>();
			[SerializeField] internal bool m_OnlyVariants = false;
		}

		protected override void OnInitialQuestionsGUI()
		{
			Text("Please select a prefab that you want to use in Tremble!");

			m_MonoBehaviourListDirty = RenderProperty(nameof(SampleData.m_BasePrefab),
				title: "Target Prefab",
				description: "The prefab to use");

			RenderProperty(nameof(SampleData.m_WantsData),
				title: "I want to add data",
				description: "Whether to allow setting fields on the prefab from TrenchBroom. e.g. "+
				             "variables such as Health, Colour, etc?");

			if (Sample.m_BasePrefab)
			{
				if (m_MonoBehaviourListDirty)
				{
					Type[] monoBehaviours = Sample.m_BasePrefab
						.GetComponents<MonoBehaviour>()
						.Select(m => m.GetType())
						.ToArray();

					m_CandidateTypes.Clear();
					m_CandidateTypes.AddRange(monoBehaviours
						.Where(t => t != typeof(TrembleSpawnablePrefab)));

					m_IsAlreadyTrembleSpawnable = monoBehaviours.Contains(typeof(TrembleSpawnablePrefab));
					m_IsMap = monoBehaviours.Contains(typeof(MapDocument));
					m_IsAlreadyPrefabEntity = !m_IsAlreadyTrembleSpawnable && monoBehaviours.Any(t => t.HasCustomAttribute<PrefabEntityAttribute>());
				}

				if (m_IsMap)
				{
					Text("Ha! No, that's a map prefab. We better not.");
				}
				else if (m_IsAlreadyPrefabEntity)
				{
					Text("Well, that's already spawnable in TrenchBroom. Try another prefab!");
				}
				else if (m_CandidateTypes.Count == 0)
				{
					if (m_IsAlreadyTrembleSpawnable)
					{
						Callout("Note, this prefab already has a TrembleSpawnablePrefab component",
							"allowing it to be spawned from TrenchBroom - but without field support.",
							"Converting this to a PrefabEntity will remove the TrembleSpawnablePrefab component!");
					}

					if (m_IsAlreadyTrembleSpawnable)
					{
						if (Sample.m_WantsData)
						{
							Resolution("Create a new MonoBehaviour to use - this allows",
								"you to add fields which can be set in TrenchBroom",
								"- in order to be able to set data (i.e. Health, Type, etc).");
							ActionBar_SingleAction("Create New MonoBehaviour", () =>
							{
								m_NewUnityScriptName = GetScriptNameFromPrefab(Sample.m_BasePrefab);

								if (Sample.m_BasePrefab.TryGetComponent(out TrembleSpawnablePrefab tsp))
								{
									Sample.m_Category = tsp.Category;
									Sample.m_OnlyVariants = tsp.OnlyVariants;
								}

								SetResolution(Resolve_CreateNewClassAndAdd);
							});
						}
						else
						{
							Text("Well, that's already spawnable in TrenchBroom. Tick the box above",
								"to add data using a [PrefabEntity].");
						}
					}
					else
					{
						if (Sample.m_WantsData)
						{
							Resolution("Create a new MonoBehaviour to use - this allows",
								"you to add fields which can be set in TrenchBroom",
								"- in order to be able to set data (i.e. Health, Type, etc).");
							ActionBar_SingleAction("Create New MonoBehaviour", () =>
							{
								m_NewUnityScriptName = GetScriptNameFromPrefab(Sample.m_BasePrefab);

								if (Sample.m_BasePrefab.TryGetComponent(out TrembleSpawnablePrefab tsp))
								{
									Sample.m_Category = tsp.Category;
									Sample.m_OnlyVariants = tsp.OnlyVariants;
								}

								SetResolution(Resolve_CreateNewClassAndAdd);
							});
						}
						else
						{
							Resolution("Add a 'TrembleSpawnablePrefab' component - this",
								"only allows you to spawn this prefab without being able to",
								"set any data on it.");

							ActionBar_SingleAction("Add TrembleSpawnablePrefab component", () =>
							{
								m_IsOnlyAddingSpawnablePrefab = true;
								SetResolution(Resolve_AddTrembleSpawnablePrefab);
							});
						}
					}
				}
				else if (m_IsAlreadyTrembleSpawnable)
				{
					// Already spawnable, but no candidates for [PrefabEntity] - make a new code class
					if (Sample.m_WantsData)
					{
						Resolution("Upgrade to a [PrefabEntity] - this allows",
							"you to add fields which can be set in TrenchBroom",
							"- in order to be able to set data (i.e. Health, Type, etc).");

						foreach (Type type in m_CandidateTypes)
						{
							Bullet(type.Name);
							ActionBar_SingleAction($"Add [PrefabEntity] to {type.Name}", () =>
							{
								m_UnityScriptType = type;

								if (Sample.m_BasePrefab.TryGetComponent(out TrembleSpawnablePrefab tsp))
								{
									Sample.m_Category = tsp.Category;
									Sample.m_OnlyVariants = tsp.OnlyVariants;
								}

								SetResolution(Resolve_AddPrefabEntityAttribute);
							});
						}
					}
					else
					{
						Text("This is already a TrembleSpawnablePrefab. Unless you want to add data,",
							"you don't need to do anything!");
					}
				}
				else
				{
					if (Sample.m_WantsData)
					{
						if (m_CandidateTypes.Count == 1)
						{
							Type type = m_CandidateTypes[0];

							Resolution($"You can add a [PrefabEntity] attribute to {type.Name} on this",
								"prefab. This will enable setting the following fields:");

							Indent(() =>
							{
								foreach (string fieldName in GetFieldNames(type))
								{
									Bullet(fieldName);
								}
							});

							ActionBar_SingleAction($"Add [PrefabEntity]", () =>
							{
								m_UnityScriptType = type;
								SetResolution(Resolve_AddPrefabEntityAttribute);
							});
						}
						else
						{
							Resolution("This prefab has MonoBehaviours which can be set to be a",
								"[PrefabEntity]. Select which one to add this attribute to.");

							foreach (Type type in m_CandidateTypes)
							{
								string[] fieldNames = GetFieldNames(type);

								string fields = (fieldNames is { Length: > 0 }) ? String.Join(", ", fieldNames) : "no fields";
								Bullet($"{type.Name} ({fields})");

								ActionBar_SingleAction($"Add [PrefabEntity] to {type.Name}", () =>
								{
									m_UnityScriptType = type;
									SetResolution(Resolve_AddPrefabEntityAttribute);
								});
							}
						}
					}
					else
					{
						Resolution("Simply add a TrembleSpawnablePrefab component to it, which will",
							"allow you to spawn the prefab in TrenchBroom, but not allow you to set any",
							"data on it.");
						ActionBar_SingleAction("Add TrembleSpawnablePrefab component", () =>
						{
							m_IsOnlyAddingSpawnablePrefab = true;
							SetResolution(Resolve_AddTrembleSpawnablePrefab);
						});
					}
				}
			}
		}

		private string[] GetFieldNames(Type type)
		{
			List<Type> soTypes= new();
			FgdClass dummyClass = new(FgdClassType.Point, type.Name, type.Name);
			TrembleSync.AddExposedFieldsToEntity(type, dummyClass, m_FieldConverterCollection, soTypes);

			// Gather spawnflags
			NamingConvention spawnFlagNaming = TrembleSyncSettings.Get().SpawnFlagNamingConvention;

			IEnumerable<string> spawnFlagNames = dummyClass.AllSpawnFlags
				.Select(f =>
				{
					string name = f.FieldName.ToNamingConvention(spawnFlagNaming);
					if (spawnFlagNaming == NamingConvention.HumanFriendly)
					{
						name += "?";
					}

					return $"{name} (Boolean)";
				});

			return dummyClass.AllFields
				.Select(f => $"{f.FieldName} ({f.FriendlyTypeName})")
				.Concat(spawnFlagNames)
				.ToArray();
		}

		protected override void OnPropertiesGUI()
		{
			H2("Optional Extras");

			RenderProperty(nameof(SampleData.m_Category),
				title: "Category Name",
				description: "(optional) The category/group name to use in TrenchBroom. Leave this blank to use the default.");

			Foldout("(optional) Include/Exclude specific Prefabs?", () =>
			{
				if (!m_IsOnlyAddingSpawnablePrefab)
				{
					Text("By default, any other prefabs containing your MonoBehaviour will be exported.");
					Text("Instead, you can choose to explicitly whitelist prefab(s) here.");
					Text("Or, you can choose to blacklist certain prefab(s) here.");

					RenderProperty(nameof(SampleData.m_IncludedPrefabs),
						title: "Included Prefabs",
						description: $"A whitelist of the ONLY prefab(s) to include, even though others may include your {ScriptName} MonoBehaviour?");

					RenderProperty(nameof(SampleData.m_ExcludedPrefabs),
						title: "Excluded Prefabs",
						description: $"A blacklist of any prefab(s) to ignore, even though they include your {ScriptName} MonoBehaviour?");
				}

				RenderProperty(nameof(SampleData.m_OnlyVariants),
					title: "Only Prefab Variants?",
					description: "Whether to only export VARIANTS of the prefab(s) found?");
			});
		}

		protected override void OnPostPropertiesGUI()
		{
			string catName = Sample.m_Category.IsNullOrEmpty() ? FgdConsts.PREFAB_PREFIX : Sample.m_Category;
			//string entName = Sample.m_UnityName.ToNamingConvention(SyncSettings.TypeNamingConvention);
			string fullEntName = $"{catName}_{Sample.m_BasePrefab.name}";

			if (m_IsOnlyAddingSpawnablePrefab)
			{
				H2("Tremble Spawnable Prefab Details");
				PropertyDescription("Prefab Name", labelWidth: 250f, Sample.m_BasePrefab.name);
				PropertyDescription("Exported TrenchBroom Name", labelWidth: 250f, fullEntName);
			}
			else
			{
				H2("C# Code");
				Text($"This is the code that will be used for {Sample.m_BasePrefab.name} if you click Apply below. You don't need to copy",
					"it manually (unless you want to!)");
				CodeWithHeader($"C# Script: {ScriptName}.cs (for TrenchBroom Prefab entity '{fullEntName}')", GenerateFullCodeSnippet());
			}

			Space();

			GUI.color = Color.green;
			LargeButton("Apply & Finish!", Resolve);
			GUI.color = Color.white;
		}

		private void Resolution(params string[] resolution)
		{
			HorizontalLine();

			H1("Action to take");
			Text(resolution);
		}

		private string GetAttributeLine()
		{
			List<string> attributeParts = new(16);

			if (!Sample.m_Category.IsNullOrEmpty())
			{
				attributeParts.Add($"category: \"{Sample.m_Category}\"");
			}

			if (TryFormatPrefabNames("excludePrefab", Sample.m_ExcludedPrefabs, out string excludePart))
			{
				attributeParts.Add(excludePart);
			}

			if (TryFormatPrefabNames("includePrefab", Sample.m_IncludedPrefabs, out string includePart))
			{
				attributeParts.Add(includePart);
			}

			if (Sample.m_OnlyVariants)
			{
				attributeParts.Add("onlyVariants: true");
			}

			string attrName = FormatAttributeName(typeof(PrefabEntityAttribute));

			return attributeParts.Count == 0
				? $"[{attrName}]"
				: $"[{attrName}({String.Join(", ", attributeParts)})]";
		}

		private string GenerateFullCodeSnippet()
		{
			return
				"using UnityEngine;\n" +
				$"using {nameof(TinyGoose)}.{nameof(Tremble)};\n" +
				"\n" +
				GetAttributeLine() + "\n" +
				$"public class {ScriptName} : MonoBehaviour\n" +
				"{\n" +
				(IsNewScript ? $"    // Your new {ScriptName} class contents ;)" : $"    // (your existing {ScriptName} code)") + "\n" +
				"}";
		}

		private bool TryFormatPrefabNames(string propertyName, GameObject[] prefabs, out string formatted)
		{
			GameObject[] filteredPrefabs = prefabs.Where(p => p).ToArray();

			switch (filteredPrefabs.Length)
			{
				case 1:
					formatted = $"{propertyName}: \"{filteredPrefabs[0].name}\"";
					return true;
				case > 1:
				{
					string prefabList = String.Join("\", \"", filteredPrefabs.Select(p => p.name));
					formatted = $"{propertyName}s: \"{prefabList}\"";
					return true;
				}
			}

			formatted = default;
			return false;
		}

		private void SetResolution(Action resolutionAction)
		{
			m_ResolutionAction = resolutionAction;
			MarkInitialQuestionsDone();
		}

		private void Resolve()
		{
			m_ResolutionAction.Invoke();
			GoToPage(typeof(PrefabEntitiesPage));
		}

		private void Resolve_AddTrembleSpawnablePrefab()
		{
			Sample.m_BasePrefab.AddComponent<TrembleSpawnablePrefab>().Modify(so =>
			{
				so.FindBackedProperty(nameof(TrembleSpawnablePrefab.OnlyVariants)).boolValue = Sample.m_OnlyVariants;
				so.FindBackedProperty(nameof(TrembleSpawnablePrefab.Category)).stringValue = Sample.m_Category;
			});

			EditorUtility.SetDirty(Sample.m_BasePrefab);
			AssetDatabase.SaveAssets();

			EditorApplication.delayCall += TrembleEditorAPI.SyncToTrenchBroom;

			ShowCompleteDialog(
				title: "TrembleSpawnablePrefab component added!",
				message: $"{Sample.m_BasePrefab.name} now has a Tremble Spawnable Prefab component and should be available from TrenchBroom.");
		}

		private void Resolve_CreateNewClassAndAdd()
		{
			// Write code and compile
			string assetsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Assets");

			string scriptFilePath = EditorUtility.SaveFilePanel($"Choose where to save {ScriptName}", assetsFolder, $"{ScriptName}.cs", "cs");
			if (scriptFilePath.IsNullOrEmpty())
				return;

			File.WriteAllText(scriptFilePath, GenerateFullCodeSnippet());

			EditorCompileUtil.CompileThen(typeof(PrefabBuilderPage), nameof(OnPostCompile_AddNewScript),
				ScriptName, AssetDatabase.GetAssetPath(Sample.m_BasePrefab));
		}

		private void Resolve_AddPrefabEntityAttribute()
		{
			string scriptFileName = GetScriptFilename(m_UnityScriptType);
			if (scriptFileName.IsNullOrEmpty())
			{
				EditorUtility.DisplayDialog(
					title: "Uh-oh!",
					message: $"Tremble could not find the source file for {ScriptName}.",
					"Oh no!");
				return;
			}

			bool prefabEntityWasAdded = false;
			bool usingWasAdded = false;

			string[] lines = File.ReadAllLines(scriptFileName);
			List<string> outLines = new(lines.Length+2);
			foreach (string line in lines)
			{
				if (!usingWasAdded && !line.ContainsInvariant("using", caseSensitive: true))
				{
					outLines.Add("using TinyGoose.Tremble;");
					usingWasAdded = true;
				}

				if (line.ContainsInvariant($"class {ScriptName}", caseSensitive: true))
				{
					prefabEntityWasAdded = true;
					outLines.Add($"\t{GetAttributeLine()}");
				}

				outLines.Add(line);
			}

			if (!prefabEntityWasAdded)
			{
				EditorUtility.DisplayDialog(
					title: "Uh-oh!",
					message: $"Tremble could not find the class {ScriptName} in the source file {Path.GetFileName(scriptFileName)}.",
					"Oh no!");
				return;
			}

			File.WriteAllLines(scriptFileName, outLines);

			EditorCompileUtil.CompileThen(typeof(PrefabBuilderPage), nameof(OnPostCompile_AddAttribute), ScriptName);
		}

		private static string GetScriptNameFromPrefab(GameObject prefab)
		{
			string newName = prefab.name;
			if (newName.StartsWithInvariant("P_", caseSensitive: true))
				return newName.Substring(2);

			StringBuilder finalName = new(newName.Length);
			foreach (char c in newName)
			{
				if (c is '-')
				{
					// Swap characters
					finalName.Append('_');
				}
				else if (c is ' ')
				{
					// Omit characters
				}
				else
				{
					// Allowed characters
					finalName.Append(c);
				}
			}

			return finalName.ToString();
		}

		private static string GetScriptFilename(Type monoBehaviourType)
		{
			if (s_ScriptFileLookup.TryGetValue(monoBehaviourType, out string existingPath))
				return existingPath;

			foreach (string path in AssetDatabase.GetAllAssetPaths())
			{
				// Check if the file name matches exactly (without the full path)
				if (Path.GetFileNameWithoutExtension(path).EqualsInvariant(monoBehaviourType.Name))
				{
					s_ScriptFileLookup[monoBehaviourType] = path;
					return path;
				}
			}

			return null;
		}

		private static void ShowCompleteDialog(string title, string message)
		{
			if (TrenchBroomUtil.IsTrenchBroomRunning)
			{
				message += "\n\n Note: Press F6 inside TrenchBroom to refresh Entity Definitions for it to appear!";
			}

			EditorUtility.DisplayDialog(title, message, "Great!");
		}

		private static void OnPostCompile_AddAttribute(string scriptName)
		{
			TrembleEditorAPI.SyncToTrenchBroom();

			ShowCompleteDialog(
				title: "[PrefabEntity] attribute added!",
				message: $"{scriptName} now has a [PrefabEntity] attribute and should be available from TrenchBroom.");
		}

		private static void OnPostCompile_AddNewScript(string scriptName, string prefabPath)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

			Type newComponentType = Unsupported.GetTypeFromFullName(scriptName);
			prefab.AddComponent(newComponentType);

			foreach (TrembleSpawnablePrefab tsp in prefab.GetComponents<TrembleSpawnablePrefab>())
			{
				Component.DestroyImmediate(tsp, true);
			}

			EditorUtility.SetDirty(prefab);
			AssetDatabase.SaveAssets();

			// Now sync
			EditorApplication.delayCall += TrembleEditorAPI.SyncToTrenchBroom;

			// And tell the user!
			ShowCompleteDialog(
				title: "New Prefab Entity Created!",
				message: $"{scriptName}.cs was created and attached to {prefab.name}, and should be available from TrenchBroom.");
		}
	}
}