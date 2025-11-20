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
	public class MapFileAssetPostprocessor : AssetPostprocessor
	{
		private static readonly Stack<string> s_MapsToConvert = new();
		private static string s_LastMapPath;
		
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			foreach (string requestFileName in importedAssets)
			{
				// We imported something, but not a MapFileAsset
				if (AssetDatabase.GetMainAssetTypeAtPath(requestFileName) != typeof(MapFileAsset))
					continue;

				// Check we haven't already processed this map, or already plan to
				string mapFileName = GetMapFileName(requestFileName);
				if (File.Exists(mapFileName) || s_MapsToConvert.Contains(requestFileName))
					continue;
				
				// Add to the work queue
				s_MapsToConvert.Push(requestFileName);
				
				// Start conversion process
				EditorApplication.update -= PerformConversionOnUpdate;
				EditorApplication.update += PerformConversionOnUpdate;
			}
		}

		private static string GetMapFileName(string requestFileName) => requestFileName.Replace(".asset", ".map");

		private static void PerformConversionOnUpdate()
		{
			// Nothing left to do - stop!
			if (s_MapsToConvert.Count == 0)
			{
				EditorApplication.update -= PerformConversionOnUpdate;
				return;
			}

			string requestFileName = s_MapsToConvert.Pop();

			// Delete the request file
			string mapFileName = GetMapFileName(requestFileName);
			AssetDatabase.DeleteAsset(requestFileName);

			// Open new file for write
			using FileStream file = File.Create(mapFileName);
			using StreamWriter sw = new(file, Encoding.ASCII);
			
			// Write a blank map, or copy template
			TrembleSyncSettings trembleSyncSettings = TrembleSyncSettings.Get();
			if (trembleSyncSettings.TemplateMap && trembleSyncSettings.TemplateMap.TryGetComponent(out MapDocument _))
			{
				string templatePath = AssetDatabase.GetAssetPath(trembleSyncSettings.TemplateMap);
				
				using FileStream templateFile = File.OpenRead(templatePath);
				using StreamReader sr = new(templateFile, Encoding.ASCII);

				while (!sr.EndOfStream)
				{
					sw.WriteLine(sr.ReadLine());
				}
				sr.Close();
			}
			else
			{
				// New file
				sw.WriteLine($"// Game: {TrembleConsts.GAME_NAME}");
				sw.WriteLine("// Format: Quake3 (Valve)");
				sw.WriteLine("// entity 0");
				sw.WriteLine("{");
				sw.WriteLine("\"mapversion\" \"220\"");
				
				sw.Write("\"_tb_textures\" \"");
				List<MaterialGroup> materialGroups = trembleSyncSettings.GetMaterialGroupsOrDefault();
				sw.Write(String.Join(';', materialGroups.Select(mg => $"textures/{mg.Name}")));
				sw.WriteLine("\"");
				
				sw.WriteLine("\"classname\" \"worldspawn\"");
				sw.WriteLine("// brush 0");
				sw.WriteLine("{");
				sw.WriteLine("( -128 -128 -64 ) ( -128 -127 -64 ) ( -128 -128 -63 ) __TB_empty [ 0 -1 0 0 ] [ 0 0 -1 0 ] 0 0.5 0.5");
				sw.WriteLine("( -128 -128 -64 ) ( -128 -128 -63 ) ( -127 -128 -64 ) __TB_empty [ 1 0 0 0 ] [ 0 0 -1 0 ] 0 0.5 0.5");
				sw.WriteLine("( -128 -128 -64 ) ( -127 -128 -64 ) ( -128 -127 -64 ) __TB_empty [ -1 0 0 0 ] [ 0 -1 0 0 ] 0 0.5 0.5");
				sw.WriteLine("( 128 128 64 ) ( 128 129 64 ) ( 129 128 64 ) __TB_empty [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 0.5 0.5");
				sw.WriteLine("( 128 128 64 ) ( 129 128 64 ) ( 128 128 65 ) __TB_empty [ -1 0 0 0 ] [ 0 0 -1 0 ] 0 0.5 0.5");
				sw.WriteLine("( 128 128 64 ) ( 128 128 65 ) ( 128 129 64 ) __TB_empty [ 0 1 0 0 ] [ 0 0 -1 0 ] 0 0.5 0.5");
				sw.WriteLine("}");
				sw.WriteLine("}");	
			}
			
			sw.Close();
			
			// Open in TB?
			if (EditorUtility.DisplayDialog("Map Created!", "Want to open it now in TrenchBroom?", "Yep!", "Not now..."))
			{
				TrembleEditorAPI.SyncToTrenchBroom();
				TrenchBroomUtil.OpenWithTrenchBroom(mapFileName);

				s_LastMapPath = mapFileName;
				
				EditorApplication.update -= PromptToOpenMapOnFocus;
				EditorApplication.update += PromptToOpenMapOnFocus;
			}
		}

		private static void PromptToOpenMapOnFocus()
		{	
#if UNITY_2022_1_OR_NEWER
			if (!EditorApplication.isFocused)
				return;
#endif
			
			EditorApplication.update -= PromptToOpenMapOnFocus;
			
			// Place in world?
			if (EditorUtility.DisplayDialog("Welcome back!", "Want to place your brand new map in this Unity scene?", "Yep!", "No thanks"))
			{
				AssetDatabase.Refresh();
					
				GameObject mapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(s_LastMapPath);
				if (!mapPrefab)
				{
					EditorUtility.DisplayDialog("Oh no!", "Your new map failed to import. You may need to restart Unity and then reimport it. (this is a rare bug)", "Okay :(");
					return;
				}
				GameObject mapObject = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab);
				mapObject.transform.position = Vector3.zero;
			}
		}
	}
}