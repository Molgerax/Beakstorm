//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	// -----------------------------------------------------------------------------------------------------------------------------
	//		Actual DLL call
	// -----------------------------------------------------------------------------------------------------------------------------
	public enum Q3Map2Result
	{
		Succeeded,
		SucceededWithWarnings,
		FailedWithMissingTextures,
		Failed,
	}
	public static class Q3Map2Dll
	{
		private static bool s_IsLoaded = false;
		public static bool IsLoaded => s_IsLoaded;

		// Callback function signature
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void MessageHandlerFunc(string output);

		// Change lib name based on platform
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
		private const string Q3MAP2_LIB_NAME = "q3map2";
#else
//TODO(jwf): ATV: No q3map2 - solution: use "__Internal" as the name and build as static lib
		#warning Unknown platform - libq3map must be build as a static lib!
		private const string Q3MAP2_LIB_NAME = "__Internal";
#endif

#if UNITY_WEBGL || UNITY_TVOS
		// Stub for WebGL, tvOS
		public static int DLL_ConvertMap(
			string mapFile,
			string fsPath,
			MessageHandlerFunc functionCallback,
			string[] inArgs,
			int inArgCount)
		{
			return -1;
		}
#else
		// DLL entry point for conversion
		[DllImport(Q3MAP2_LIB_NAME, EntryPoint = "convert_map_unity", CallingConvention = CallingConvention.Cdecl)]
		private static extern int DLL_ConvertMap(
			[MarshalAs(UnmanagedType.LPStr)]																					string mapFile,
			[MarshalAs(UnmanagedType.LPStr)]																					string fsPath,
			[MarshalAs(UnmanagedType.FunctionPtr)]																			MessageHandlerFunc functionCallback,
			[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr, SizeParamIndex = 4)]	string[] inArgs,
			[MarshalAs(UnmanagedType.SysInt)]																				int inArgCount);
#endif

		public static int ConvertMap(string mapFile, MessageHandlerFunc functionCallback, string[] inArgs)
			=> ConvertMap(mapFile, "Library", functionCallback, inArgs);

		public static int ConvertMap(string mapFile, string fsPath, MessageHandlerFunc functionCallback, string[] inArgs)
		{
			try
			{
				s_IsLoaded = true;
				
				// Use another thread to do the actual compilation,
				// so that we can control the stack size.
				const int STACK_SIZE_MB = 8;
				
				int exitCode = 0;
				void CompileOnOtherThread() 
					=> exitCode = DLL_ConvertMap(mapFile, fsPath, functionCallback, inArgs, inArgs.Length);

				Thread compilerThread = new(
					start: CompileOnOtherThread, 
					maxStackSize: STACK_SIZE_MB * 1024 * 1024
				);
				compilerThread.Start();
				compilerThread.Join();
				
				return exitCode;
			}
			catch (DllNotFoundException)
			{
				s_IsLoaded = false;
				return -0xdead;
			}
		}

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Plugins Folder
		// -----------------------------------------------------------------------------------------------------------------------------
#if UNITY_EDITOR
		private static string PluginsFolder => Path.Combine(TrembleConsts.EDITOR_GetTrembleInstallFolder(), "Plugins");
		public static bool EDITOR_PluginsFolderExists() => Directory.Exists(PluginsFolder);

		public static void EDITOR_DeletePluginsFolder()
		{
			if (!EDITOR_PluginsFolderExists())
				return;

			EDITOR_IsPluginsFolderFlaggedForDeletion = false;

			try
			{
				Directory.Delete(PluginsFolder, true);

				string metaFile = PluginsFolder + ".meta";
				if (File.Exists(metaFile))
				{
					File.Delete(PluginsFolder + ".meta");
				}
			}
			catch (UnauthorizedAccessException)
			{
				EDITOR_IsPluginsFolderFlaggedForDeletion = true;

				EditorUtility.DisplayDialog(
					title: "Q3Map2 Upgrade",
					message: $"Unfortunately, the auto-upgrader has encountered an error. Please quit Unity, and manually delete {PluginsFolder} in your file explorer. Sorry!",
					ok: "Got it, no worries!"
				);
			}
		}

		public static bool EDITOR_IsPluginsFolderFlaggedForDeletion
		{
			get => EditorPrefs.GetBool($"Tremble_{Application.productName}_DeletePluginsNow", false);
			set => EditorPrefs.SetBool($"Tremble_{Application.productName}_DeletePluginsNow", value);
		}
#endif

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Version Info
		// -----------------------------------------------------------------------------------------------------------------------------
		public static Q3Map2VersionInfo GetTinyGooseVersionInfo()
		{
			const string LIB_Q3_MAP2 = "libQ3Map2 (Tiny Goose)";

			Dictionary<string, string> info = GetVersionInfo();
			return Q3Map2VersionInfo.Parse(info.GetValueOrDefault(LIB_Q3_MAP2, null)); // e.g. v2.5.1n-git-f278fa
		}

		public static Dictionary<string, string> GetVersionInfo()
		{
			string[] args = { "-v" };
			List<string> outputLines = new();

			ConvertMap("", line => outputLines.Add(line), args);

			Dictionary<string, string> info = new();
			foreach (string line in outputLines)
			{
				if (!TrySplitLineIntoKeyValue(line, out string key, out string value))
					continue;

				info[key] = value;
			}

			return info;
		}

		private static bool TrySplitLineIntoKeyValue(string line, out string key, out string value)
		{
			Span<char> separators = stackalloc char[2];
			separators[0] = '-';
			separators[1] = ':';

			foreach (char separator in separators)
			{
				int sepIdx = line.IndexOf(separator);
				if (sepIdx == -1)
					continue;

				key = line.Substring(0, sepIdx).Trim();
				value = line.Substring(sepIdx + 1).Trim();
				return true;
			}

			key = value = null;
			return false;
		}
	}
}
