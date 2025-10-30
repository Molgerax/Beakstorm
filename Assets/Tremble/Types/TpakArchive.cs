//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace TinyGoose.Tremble
{
	[Serializable]
	public struct TpakTocEntry
	{
		public string Path;
		public string SourcePath;

		public int FileLength;
	}

	public class TpakArchive
	{
		private const string TPAK_MAGIC = "TPAK";
		private const int TPAK_MAX_PATH = 255;

		private readonly string m_ArchiveFile;
		private readonly List<TpakTocEntry> m_Entries = new();

		public TpakArchive(string archiveFile)
		{
			m_ArchiveFile = archiveFile;
		}

		public void AddDirectory(string directory)
		{
			AddDirectory_Recursive(directory, null);
		}

		public void AddFile(string filePath, string filenameInTpak)
		{
			m_Entries.Add(new()
			{
				Path = filenameInTpak,
				SourcePath = filePath,
				FileLength = (int)(new FileInfo(filePath).Length)
			});
		}

		public async Task<bool> UnpackAsync(string folder)
		{
			StreamReadIO pakIn = new(m_ArchiveFile);
			if (!pakIn.ReadString(4).EqualsInvariant(TPAK_MAGIC, caseSensitive: true))
			{
				Debug.LogError($"Invalid magic reading tpak '{m_ArchiveFile}'!");
				return false;
			}

			int numEntries = pakIn.ReadInt32();
			for (int entryIdx = 0; entryIdx < numEntries; entryIdx++)
			{
				int pathLength = pakIn.ReadInt32();

				TpakTocEntry entry = new()
				{
					Path = pakIn.ReadString(pathLength),
					FileLength = pakIn.ReadInt32(),
				};
				entry.SourcePath = Path.Combine(folder, entry.Path);
				DirectoryUtil.CreateAllDirectories(entry.SourcePath);

				m_Entries.Add(entry);
			}

			foreach (TpakTocEntry entry in m_Entries)
			{
				if (File.Exists(entry.SourcePath))
				{
					pakIn.SkipBytes(entry.FileLength);
					continue;
				}

				byte[] file = pakIn.ReadBytes(entry.FileLength);
				await File.WriteAllBytesAsync(entry.SourcePath, file);
			}

			return true;
		}

		public async Task PackAsync()
		{
			await using FileStream pakOutStream = new(m_ArchiveFile, FileMode.Create, FileAccess.Write);

			using StreamWriteIO pakOut = new(pakOutStream);
			pakOut.WriteString(TPAK_MAGIC, 4);
			pakOut.WriteInt32(m_Entries.Count);

			foreach (TpakTocEntry entry in m_Entries)
			{
				pakOut.WriteInt32(entry.Path.Length);
				pakOut.WriteString(entry.Path);
				pakOut.WriteInt32(entry.FileLength);
			}

			byte[] buffer = new byte[1024];
			foreach (TpakTocEntry entry in m_Entries)
			{
				await using FileStream entryFile = new(entry.SourcePath, FileMode.Open, FileAccess.Read);
				using BinaryReader entryIn = new(entryFile);

				int readBytes;
				do
				{
					readBytes = entryIn.Read(buffer, 0, buffer.Length);
					pakOut.WriteBytes(buffer, readBytes);
				} while (readBytes > 0);
			}
		}

		private void AddDirectory_Recursive(string directory, string currentPath)
		{
			foreach (string file in Directory.EnumerateFiles(directory))
			{
				string path = currentPath == null ? Path.GetFileName(file) : Path.Combine(currentPath, Path.GetFileName(file));
				AddFile(file, path);
			}

			foreach (string dir in Directory.EnumerateDirectories(directory))
			{
				string newCurrentPath = currentPath == null ? Path.GetFileName(dir) : Path.Combine(currentPath, Path.GetFileName(dir));
				AddDirectory_Recursive(dir, newCurrentPath);
			}
		}
	}
}