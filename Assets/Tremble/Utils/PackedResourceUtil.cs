//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.IO;
using System.Threading.Tasks;
using UnityEngine;


namespace TinyGoose.Tremble
{
	public static class PackedResourceUtil
	{
#if UNITY_EDITOR
		public static async Task<string> PackFolderIntoResource(string folderPath, string resourceName)
		{
			if (!Directory.Exists(folderPath))
				return null;

			string containingFolder = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets");
			string resourcePath = Path.Combine(containingFolder, resourceName + ".tpak");

			DirectoryUtil.CreateAllDirectories(containingFolder);
			File.Delete(resourcePath);

			TpakArchive tpak = new(resourcePath);
			tpak.AddDirectory(folderPath);
			await tpak.PackAsync();

			return resourcePath;
		}
#endif

		public static async Task<bool> UnpackResourceIntoFolder(string resourceName, string folderPath)
		{
			// Empty directory
			DirectoryUtil.CreateAllDirectories(folderPath);

			string resourcePath = Path.Combine(Application.streamingAssetsPath, resourceName + ".tpak");
			if (!File.Exists(resourcePath))
				return false;

			TpakArchive tpak = new(resourcePath);
			await tpak.UnpackAsync(folderPath);
			return true;
		}
	}
}