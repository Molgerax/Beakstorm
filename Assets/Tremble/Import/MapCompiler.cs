//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace TinyGoose.Tremble
{
	public static class MapCompiler
	{
		public static MapBsp BuildBsp(TrembleSyncSettings syncSettings, string mapPath, string extraCommandLineArgs, out Q3Map2Result result,
			Action<string> onOutputMessage = null)
		{
			string mapName = Path.GetFileNameWithoutExtension(mapPath);
			string stagingFolder = Path.Combine(Application.temporaryCachePath, $"Build_{mapName}");

			try
			{
				// -----------------------------------------------------------------------------------------------------------------------------
				//		Copy to staging folder
				// -----------------------------------------------------------------------------------------------------------------------------
				DirectoryUtil.EmptyAndCreateDirectory(stagingFolder);

				string stageMapPath = Path.Combine(stagingFolder, Path.GetFileName(mapPath));
				CopyMapToStagingFolder(mapPath, stageMapPath, out MapGroupData[] foundGroups);


				// -----------------------------------------------------------------------------------------------------------------------------
				//		Run Q3Map2
				// -----------------------------------------------------------------------------------------------------------------------------
				Q3Map2Result? overrideResult;

				using (TrembleTimerScope __ = new(TrembleTimer.Context.RunQ3Map2))
				{
					// Combine all args
					string[] extraArgs = extraCommandLineArgs.Split(" ", StringSplitOptions.RemoveEmptyEntries);

					// Get output filename
					MapCompilerOutputHandler.Init(onOutputMessage);
					{
						int returnCode = Q3Map2Dll.ConvertMap(stageMapPath, MapCompilerOutputHandler.HandleOutput, extraArgs);
						result = returnCode == 0 ? Q3Map2Result.Succeeded : Q3Map2Result.Failed;
					}
					overrideResult = MapCompilerOutputHandler.DeInit();
				}

				// -----------------------------------------------------------------------------------------------------------------------------
				//		Parse BSP
				// -----------------------------------------------------------------------------------------------------------------------------
				string bspPath = Path.ChangeExtension(stageMapPath, "bsp");
				if (!File.Exists(bspPath))
				{
					result = Q3Map2Result.Failed;
					return null;
				}

				result = overrideResult ?? result;

				using TrembleTimerScope _ = new(TrembleTimer.Context.ParseBsp);
				return BspParser.Parse(bspPath, syncSettings.ImportScale, foundGroups: foundGroups);
			}
			catch (DllNotFoundException dll)
			{
#if UNITY_EDITOR
				Debug.LogException(dll);
				EditorUtility.DisplayDialog("DLL Not Found", "Tremble's DLL was not found or couldn't be loaded. You should try to reinstall it from the Asset Store, or visit our Discord if this keeps happening!", "Okay");
#else
				Debug.LogException(dll);
				Debug.LogError($"Missing q3map2 DLL file for platform: {Application.platform}");
#endif

				result = Q3Map2Result.Failed;
				return null;
			}
			finally
			{
				if (Directory.Exists(stagingFolder))
				{
					Directory.Delete(stagingFolder, true);
				}
			}
		}

		private static void CopyMapToStagingFolder(string inputMap, string buildMap, out MapGroupData[] groups)
		{
			using TrembleTimerScope _ = new(TrembleTimer.Context.ExportMapToStaging);

			// Open map
			using FileStream file = new(inputMap, FileMode.Open, FileAccess.Read);
			using StreamReader sr = new(file, Encoding.ASCII);

			// -----------------------------------------------------------------------------------------------------------------------------
			//		Pass 1 - Gather groups
			// -----------------------------------------------------------------------------------------------------------------------------
			groups = new MapGroupData[32]; // Start with 32 groups, but can grow
			groups[0] = new() { Name = "Default", IsNoExport = false };

			while (!sr.EndOfStream)
			{
				string line = sr.ReadLine();

				if (line == null || !line.ContainsInvariant(FgdConsts.PROPERTY_CLASSNAME, caseSensitive: true) || !line.ContainsInvariant("func_group", caseSensitive: true))
					continue;

				int groupIdx = 0;
				string groupName = "Default";
				bool isNoExport = false;
				bool isLayer = false;

				while (!sr.EndOfStream && line != null && !line.Trim().EqualsInvariant("}", caseSensitive: true) && !line.StartsWithInvariant("//", caseSensitive: true))
				{
					TokenParser tokenParser = new(line);
					ReadOnlySpan<char> key = tokenParser.ReadToken();
					ReadOnlySpan<char> value = tokenParser.ReadToken();

					if (key.EqualsInvariant(TBConsts.ID, caseSensitive: true))
					{
						groupIdx = int.Parse(value);
					}
					else if (key.EqualsInvariant(TBConsts.NAME, caseSensitive: true))
					{
						groupName = value.ToString();
					}
					else if (key.EqualsInvariant(TBConsts.TYPE, caseSensitive: true))
					{
						isLayer = value.EqualsInvariant(TBConsts.LAYER, caseSensitive: true);
					}
					else if (key.EqualsInvariant(TBConsts.OMIT_FROM_EXPORT, caseSensitive: true) && int.TryParse(value, out int noExportInt))
					{
						isNoExport = noExportInt == 1;
					}

					line = sr.ReadLine();
				}

				// Resize groups array if needed
				if (groupIdx >= groups.Length)
				{
					Array.Resize(ref groups, groupIdx + 1);
				}

				groups[groupIdx] = new() { Name = groupName, IsNoExport = isNoExport, IsLayer = isLayer };
			}

			int numGroups = groups.Length - 1;
			while (groups[numGroups].Name == null)
			{
				numGroups--;
			}

			Array.Resize(ref groups, numGroups + 1);

			// -----------------------------------------------------------------------------------------------------------------------------
			//		Pass 2 - Copy map into staging folder without "omit from export" layers
			// -----------------------------------------------------------------------------------------------------------------------------
			using FileStream outputStream = new(buildMap, FileMode.Create, FileAccess.Write);
			using StreamWriter writer = new(outputStream, Encoding.ASCII);

			// Copy each line from reader -> writer, ignoring no export layers
			StringBuilder entity = new();
			bool wroteKeepLights = false;
			int depth = 0;
			int thisLayerOrGroupIdx = 0;

			sr.BaseStream.Seek(0, SeekOrigin.Begin);

			while (!sr.EndOfStream)
			{
				string line = sr.ReadLine();
				if (line == null)
					break;

				if (line.IsNullOrEmpty())
					continue;

				entity.AppendLine(line);

				if (line[0] == '{') depth++;
				else if (line[0] == '}') depth--;

				// Ensure "_keepLights" is 1
				if (depth == 1 && !wroteKeepLights)
				{
					entity.AppendLine("\"_keepLights\" \"1\"");
					wroteKeepLights = true;
				}

				if (line.ContainsInvariant(TBConsts.ID, caseSensitive: true))
				{
					TokenParser tokenParser = new(line);
					tokenParser.ReadToken(); // skip id
					ReadOnlySpan<char> value = tokenParser.ReadToken();

					if (int.TryParse(value, out int layerOrGroupIdx))
					{
						thisLayerOrGroupIdx = layerOrGroupIdx;
					}
				}

				if (depth == 0)
				{
					if (thisLayerOrGroupIdx >= groups.Length || !groups[thisLayerOrGroupIdx].IsNoExport)
					{
						writer.WriteLine(entity);
					}

					entity.Clear();
				}
			}

			// This is lame. We add a fake entity called ent_mapfix which the importer
			// is set to ignore. Without this, sometimes map builds fail on Mac!
			if (!TrembleSyncSettings.Get().UseClassicQuakeCulling)
			{
				writer.WriteLine("// fix for q3map2");
				writer.WriteLine("{");
				writer.WriteLine($"\"classname\" \"{TrembleConsts.MAPFIX_ENTITY_NAME}\"");
				writer.WriteLine("\"origin\" \"32000 32000 32000\"");
				writer.WriteLine("}");
			}
		}
	}
}