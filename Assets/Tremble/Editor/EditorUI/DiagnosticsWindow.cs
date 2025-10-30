//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public class DiagnosticsWindow : EditorWindow
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Menu Items
		// -----------------------------------------------------------------------------------------------------------------------------
		[MenuItem("Tools/Tremble/Advanced/Debug/Diagnostics")]
		public static void OpenDiagnosticsWindow()
		{
			DiagnosticsWindow window = GetWindow<DiagnosticsWindow>(true, "Tremble Diagnostics", true);

			Vector2 size = new(900f, 800f);
			window.minSize = size;
			window.position = new(EditorGUIUtility.GetMainWindowPosition().center - size / 2f, size);
			window.Show();
		}

		private Vector2 m_ScrollPosition = Vector2.zero;
		private FgdWriter m_FgdWriter;
		private string m_TrenchbroomInstallPath;
		private Q3Map2VersionInfo m_Q3Map2VersionInfo;


		private void OnEnable()
		{
			m_Q3Map2VersionInfo = Q3Map2Dll.GetTinyGooseVersionInfo();
			m_FgdWriter = TrembleSync.GatherEntities();
			m_TrenchbroomInstallPath = Path.GetFullPath(TrenchBroomUtil.TrenchBroomPath);
		}

		private void OnGUI()
		{
			m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
			{
				Header("Basic Info");
				{
					Diagnostic("Tremble Version", () => TrembleConsts.VERSION_STRING);
					Diagnostic("Unity Version", () => Application.unityVersion);
					Diagnostic("Configured TB Version", () => TrembleSyncSettings.Get().TrenchBroomVersion);
					Diagnostic("OS", () => Application.platform.ToString());
					Diagnostic("OS Locale", () => CultureInfo.CurrentCulture.DisplayName);
				}

				Header("Project Info");
				{
					Diagnostic("Project Name", () => Application.productName);
					Diagnostic("Company Name", () => Application.companyName);
					Diagnostic("TB Game Name", () => TrembleConsts.GAME_NAME);
				}

				Header("Paths");
				{
					Diagnostic("User Path", () => Environment.GetFolderPath(Environment.SpecialFolder.Personal));
					Diagnostic("Map Staging Path", () => Application.temporaryCachePath);
					Diagnostic("Project Path", () => Path.GetFullPath(Directory.GetCurrentDirectory()));
					Diagnostic("BaseQ3 Path", () => TrembleConsts.BASEQ3_PATH);
					Diagnostic("TB Game Path", () => Path.GetFullPath(TrenchBroomUtil.GetGameFolder()));
					Diagnostic("TB Install Path", () => m_TrenchbroomInstallPath);
				}

				Header("Q3Map2");
				{
					Diagnostic("Q3Map2 Loaded?", () => Q3Map2Dll.IsLoaded);
					Diagnostic("Q3Map2 Version", () => $"{m_Q3Map2VersionInfo.Major}.{m_Q3Map2VersionInfo.Minor}.{m_Q3Map2VersionInfo.Patch}");
					Diagnostic("Q3Map2 Hash", () => m_Q3Map2VersionInfo.GitHash);
					Diagnostic("Q3Map2 Cmdline", () => TrembleSyncSettings.Get().ExtraCommandLineArgs);
				}

				Header("Game");
				{
					Diagnostic("Worldspawn Entity", () => m_FgdWriter.WorldspawnClass.Name);
					Diagnostic("Map Processors", () => TrembleSyncSettings.Get().EnabledMapProcessors.Select(m => m.Class.Name));

					int numMaterials = TrembleSyncSettings.Get().MaterialGroups.Sum(mg => mg.Materials.Length);
					Diagnostic("Material Groups", () => TrembleSyncSettings.Get().MaterialGroups.Select(mg => $"{mg.Name} ({mg.Materials.Length})").Append($"= {numMaterials} total materials"));

					int numBases = m_FgdWriter.BaseClasses.Count;
					int numPoints = m_FgdWriter.Classes.Count(e => e.Type == FgdClassType.Point);
					int numBrushes = m_FgdWriter.Classes.Count(e => e.Type == FgdClassType.Brush);
					Diagnostic("Entity Counts", () => $"{numBases} base(s), {numPoints} point(s), {numBrushes} brush(es)");
				}
			}
			GUILayout.EndScrollView();
		}

		private void Header(string header)
		{
			GUILayout.Space(20f);
			GUILayout.Label(header, EditorStyles.boldLabel);
		}

		private void Diagnostic(string labelTitle, Func<object> contents)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(20f);
				GUILayout.Label(labelTitle, GUILayout.Width(200f));

				try
				{
					object contentObject = contents();
					if (contentObject is IEnumerable contentObjects and not string)
					{
						GUILayout.BeginVertical();
						{
							IEnumerator objectEnumerator = contentObjects.GetEnumerator();
							using IDisposable _ = objectEnumerator as IDisposable; // ensure disposed

							while (objectEnumerator.MoveNext())
							{
								GUILayout.Label(objectEnumerator.Current?.ToString() ?? "<null>", ManualStyles.Styles.Code, GUILayout.ExpandWidth(true));
							}
						}
						GUILayout.EndVertical();
					}
					else
					{
						GUILayout.Label(contentObject.ToString(), ManualStyles.Styles.Code, GUILayout.ExpandWidth(true));
					}
				}
				catch (Exception ex)
				{
					GUI.color = Color.red;
					GUILayout.Label($"FAILED: {ex.Message}", ManualStyles.Styles.Code, GUILayout.ExpandWidth(true));
					GUI.color = Color.white;
				}
			}
			GUILayout.EndHorizontal();
		}
	}
}