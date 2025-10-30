//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	public static class TrenchBroomUtil
	{

		private static string TrenchBroomPathFromPrefs
		{
#if UNITY_EDITOR
			get => EditorPrefs.GetString("Tremble_TBPath", null);
			set => EditorPrefs.SetString("Tremble_TBPath", value);
#else
			get => PlayerPrefs.GetString("Tremble_TBPath", null);
			set => PlayerPrefs.SetString("Tremble_TBPath", value);
#endif
		}

		public static string TrenchBroomPath
		{
			get
			{
				// First look for a running Trenchbroom
				Process[] procs = Process.GetProcessesByName("trenchbroom");
				if (procs.Length > 0 && procs[0].MainModule != null)
				{
					TrenchBroomPathFromPrefs = procs[0].MainModule.FileName;
					return procs[0].MainModule.FileName;
				}

				// Not running - try a path we previously knew about
				string path = TrenchBroomPathFromPrefs;
				if (path != null && File.Exists(path))
					return path;

				return null;
			}

			private set => TrenchBroomPathFromPrefs = value;
		}

		public static bool IsTrenchBroomRunning
		{
			get
			{
				Process proc = Process.GetProcessesByName("trenchbroom").FirstOrDefault();
				if (proc == null)
					return false;

				TrenchBroomPath = proc.MainModule.FileName;

				return true;
			}
		}

		public static bool DoesEnginePathExistInTrenchBroomPrefs()
		{
			string preferencesFile = Path.Combine(GetTrenchBroomConfigFolder(), "Preferences.json");
			string key = $"Games/{TrembleConsts.GAME_NAME}/Path";

			if (!File.Exists(preferencesFile))
				return false;

			// Read in current config
			foreach (string line in File.ReadLines(preferencesFile))
			{
				string trimmedLine = line.Trim(' ', ',');

				// Ignore braces and empty lines
				if (trimmedLine.IsNullOrEmpty() || trimmedLine is "{" or "}")
					continue;

				// Existing path? - patch it!
				if (trimmedLine.StartsWith($"\"{key}\""))
					return true;
			}

			return false;
		}

		public static async Task<bool> WriteEnginePathIntoTrenchBroomPrefs()
		{
			// I didn't want to import a JSON library so - this is it.
			// Read each line into an array (ignore the braces) and remove the commas.
			// Find if there's a current path - if so, patch it up.
			//		if not, add a new one at the end.

			string preferencesFile = Path.Combine(GetTrenchBroomConfigFolder(), "Preferences.json");
			string key = $"Games/{TrembleConsts.GAME_NAME}/Path";

			string projectFolderUnix = Directory.GetCurrentDirectory().Replace('\\', '/');
			string entry = $"\"{key}\": \"{projectFolderUnix}\"";

			bool wasPatched = false;
			List<string> configLines = new();

			if (File.Exists(preferencesFile))
			{
				// Read in current config
				foreach (string line in File.ReadLines(preferencesFile))
				{
					string trimmedLine = line.Trim(' ', ',');

					// Ignore braces and empty lines
					if (trimmedLine.IsNullOrEmpty() || trimmedLine is "{" or "}")
						continue;

					// Existing path? - patch it!
					if (trimmedLine.StartsWith($"\"{key}\""))
					{
						configLines.Add(entry);
						wasPatched = true;

						continue;
					}

					configLines.Add(trimmedLine);
				}
			}

			if (!wasPatched)
			{
				// We didn't find the game - append to the end (before close brace)
				configLines.Add(entry);
			}

			// Write out new config
			for (int lineIdx = 0; lineIdx < configLines.Count; lineIdx++)
			{
				// Indent
				configLines[lineIdx] = $"    {configLines[lineIdx]}";

				// Add commas (not on the last one!)
				if (lineIdx < configLines.Count - 1)
				{
					configLines[lineIdx] += ",";
				}
			}

			configLines.Insert(0, "{");
			configLines.Add("}");

			DirectoryUtil.CreateAllDirectories(preferencesFile);
			await File.WriteAllLinesAsync(preferencesFile, configLines);

			return !wasPatched;
		}

		private static string GetTrenchBroomConfigFolder()
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
			=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "TrenchBroom");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TrenchBroom");
#else
			=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".TrenchBroom");
#endif

		public static string GetGameFolder() => Path.Combine(GetTrenchBroomConfigFolder(), "games", "Unity", Application.productName);

		public static void OpenWithTrenchBroom(string mapFile)
		{
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
			RunCommand($"open -a TrenchBroom '{mapFile}'");
#elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			// Sad. On Windows, TB is mostly portable so we may have to just show the file in Explorer, lol
			if (TrenchBroomPath != null)
			{
				RunCommand($"\"{TrenchBroomPath}\" \"{mapFile}\"", useShell: false);
			}
			else
			{
				#if UNITY_EDITOR
					EditorUtility.DisplayDialog(
						title: "Where's TrenchBroom?",
						message: "Tremble could not find TrenchBroom. Ensure TrenchBroom is installed and " +
						         "running, and Tremble will make a note of where it's installed for next time." +
						         " For now, I'll show your map in Explorer.", "Okay");
				#endif
				RunCommand($"explorer /select, \"{mapFile}\"");
			}
#else
			RunCommand($"/usr/bin/trenchbroom '{mapFile}'");
#endif
		}

		private static void RunCommand(string command, bool useShell = true)
		{
			command.Split(' ', out string exe, out string args);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
			// Windows Command Prompt
			if (useShell)
			{
				args = $"/c \"{exe} {args}\"";
				exe = "cmd.exe";
			}
#else
			// Under macOS and Linux we assume /bin/sh is a thing
			if (useShell)
			{
				args = $"-c \"{exe} {args}\"";
				exe = "/bin/sh";
			}
#endif

			Process.Start(new ProcessStartInfo(exe, args) { WorkingDirectory = Directory.GetCurrentDirectory() });
		}
	}
}