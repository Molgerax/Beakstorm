//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace TinyGoose.Tremble.Editor
{
	[Serializable]
	public record Changeset
	{
		public string Version;
		public string[] Changes;
		//public string Q3Map2Hash; // Not used!
		//public string Q3Map2Url; // Not used!

		public string Q3Map2Hash_Windows;
		public string Q3Map2Hash_Mac;
		public string Q3Map2Hash_Linux;

		public string Q3Map2Url => $"https://tinygoose.com/data/q3map2_{Q3Map2Hash}.tpak";

		public string Q3Map2Hash
		{
			get
			{
#if UNITY_EDITOR_OSX
				return Q3Map2Hash_Mac;
#elif UNITY_EDITOR_LINUX
				return Q3Map2Hash_Linux;
#else
				return Q3Map2Hash_Windows;
#endif

			}
		}
	}

	[Serializable]
	public record VersionsFile
	{
		public Changeset[] Changes;
	}

	public static class VersionCheck
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Consts
		// -----------------------------------------------------------------------------------------------------------------------------
		private const string VERSION_CHECK_URL = "https://tinygoose.com/data/tremble_version.json";

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private static VersionsFile s_Versions;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Public API
		// -----------------------------------------------------------------------------------------------------------------------------
		public static bool HasData => s_Versions?.Changes is { Length: > 0 };
		private static Changeset LatestChange => s_Versions.Changes[0];

		public static Changeset CurrentInstalledVersion
		{
			get
			{
				int currentVersion = GetVersion(TrembleConsts.VERSION_STRING);
				if (HasData)
				{
					Changeset versionInfo = s_Versions.Changes.FirstOrDefault(c => GetVersion(c.Version) == currentVersion);
					if (versionInfo != null)
						return versionInfo;
				}

				// Try to make an empty version info
				Changeset template = s_Versions?.Changes?.FirstOrDefault();

				return new()
				{
					Changes = new[] { "Development Version"},
					Version = TrembleConsts.VERSION_STRING,
					Q3Map2Hash_Windows = template?.Q3Map2Hash_Windows ?? "<unknown>",
					Q3Map2Hash_Mac = template?.Q3Map2Hash_Mac ?? "<unknown>",
					Q3Map2Hash_Linux = template?.Q3Map2Hash_Linux ?? "<unknown>",
				};
			}
		}
		public static Changeset NewestAvailableVersion => HasData ? LatestChange : default;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Checking and prompting
		// -----------------------------------------------------------------------------------------------------------------------------
		public static void FetchLatestVersionsInBackground(Action thenMainThread = null)
		{
			// Start web request on main thread
			UnityWebRequest request = UnityWebRequest.Get(VERSION_CHECK_URL);
			UnityWebRequestAsyncOperation async = request.SendWebRequest();
			async.completed += operation =>
			{
				if (request.responseCode != 200)
					return;

				s_Versions = JsonUtility.FromJson<VersionsFile>(request.downloadHandler.text);

				thenMainThread?.Invoke();
			};
		}

		public static bool IsNewerVersionAvailable
		{
			get
			{
				if (!HasData)
					return false;

				int latestVersion = GetVersion(s_Versions.Changes[0].Version);
				int currentVersion = GetVersion(TrembleConsts.VERSION_STRING);

				return latestVersion > currentVersion;
			}
		}

		public static void PromptToUpgradeIfAvailable(bool showEvenIfDeclined = false)
		{
			// First prompt to upgrade Tremble itself
			bool showMainVersionUpgrade = IsNewerVersionAvailable && (showEvenIfDeclined || !HasPromptedForVersion(LatestChange.Version));
			if (showMainVersionUpgrade)
			{
				SetHasPromptedForVersion(LatestChange.Version);
				EditorUtility.DisplayDialog(
					title: "New Tremble Version",
					message: $"A newer version of Tremble (v{LatestChange.Version}) is available! "
					         + "Open Window > Package Manager to update. You won't be reminded about this version again.",
					ok: "Cool!");
			}

			// Now sanity-check the DLL situation
			Q3Map2DllDownloaderWindow.OpenIfRequired();
		}

		public static string[] ChangesSinceCurrentVersion => HasData
			? s_Versions.Changes
				.Where(c => GetVersion(c.Version) > GetVersion(TrembleConsts.VERSION_STRING))
				.SelectMany(c => c.Changes)
				.ToArray()
			: Array.Empty<string>();

		public static int NumVersionsBehind => HasData
			? s_Versions.Changes.Count(c => GetVersion(c.Version) > GetVersion(TrembleConsts.VERSION_STRING))
			: 0;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Editor prefs
		// -----------------------------------------------------------------------------------------------------------------------------
		private static string GetPromptKey(string version) => $"Tremble_{Application.productName}_PromptedFor_{version}";
		private static bool HasPromptedForVersion(string version) => EditorPrefs.HasKey(GetPromptKey(version));
		private static void SetHasPromptedForVersion(string version) => EditorPrefs.SetBool(GetPromptKey(version), true);


		// -----------------------------------------------------------------------------------------------------------------------------
		//		Utils
		// -----------------------------------------------------------------------------------------------------------------------------
		private static int GetVersion(string versionString)
		{
			if (versionString.IsNullOrEmpty())
				return 0;

			string[] parts = versionString.Split(".");
			if (parts.Length != 3)
				return 0;

			int[] intParts = parts.Select(p => int.TryParse(p, out int intPart) ? intPart : 0).ToArray();

			return intParts[0] * 1000000 + intParts[1] * 1000 + intParts[2];
		}
	}
}