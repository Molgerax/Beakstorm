// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.IO;
using System.Threading.Tasks;

namespace TinyGoose.Tremble
{
	public static class DirectoryUtil
	{
		private static readonly char[] s_PathSeparators = { '\\', '/' };

		public static void EmptyAndCreateDirectory(string folder)
		{
			if (Directory.Exists(folder))
			{
				Directory.Delete(folder, true);
			}
			
			CreateAllDirectories(folder);
		}
		
		public static int CreateAllDirectories(string folders)
		{
			int numCreated = 0;

			string[] pathFolders = folders.Split(s_PathSeparators);
			if (pathFolders.Length <= 1)
				return numCreated;
			
			string path = pathFolders[0];
			
			for (int i = 1; i < pathFolders.Length; i++)
			{
				// Path.Combine breaks when it's absolute (because /Users becomes Users)
				path += Path.DirectorySeparatorChar + pathFolders[i];

				if (i == pathFolders.Length - 1 && pathFolders[i].Contains('.'))
				{
					// Last component and it has a dot - it's a file!
					break;
				}
				
				if (!Directory.Exists(path))
				{
					Directory.CreateDirectory(path);
					numCreated++;
				}
			}

			return numCreated;
		}

		public static async Task<bool> CopyDirectory(string source, string destination)
		{
			DirectoryInfo dir = new(source);

			// Check if the source directory exists
			if (!dir.Exists)
				return false;

			EmptyAndCreateDirectory(destination);

			await Task.Run(async () =>
			{
				foreach (FileInfo file in dir.GetFiles())
				{
					string targetFilePath = Path.Combine(destination, file.Name);
					file.CopyTo(targetFilePath);
				}

				// Recurse!
				foreach (DirectoryInfo subDir in dir.GetDirectories())
				{
					string newDestinationDir = Path.Combine(destination, subDir.Name);
					await CopyDirectory(subDir.FullName, newDestinationDir);
				}
			});

			return true;
		}
	}
}