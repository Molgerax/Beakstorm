//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TinyGoose.Tremble.Editor
{
	public class ModTimeDB
	{
		private readonly string m_DbFile;
		private readonly Dictionary<string, ulong> m_Data = new();

		public ModTimeDB(string dbFile)
		{
			m_DbFile = dbFile;
			ReadFromFile();
		}

		public ulong GetFileModifiedTime(Object asset)
		{
			string assetDatabasePath = AssetDatabase.GetAssetPath(asset);
			if (assetDatabasePath.IsNullOrEmpty() || !m_Data.TryGetValue(assetDatabasePath, out ulong time))
				return 0ul;

			return time;
		}

		public void SetFileModifiedTime(Object asset, ulong time)
		{
			string assetDatabasePath = AssetDatabase.GetAssetPath(asset);
			if (assetDatabasePath.IsNullOrEmpty())
				return;

			m_Data[assetDatabasePath] = time;
		}

		public static ulong CalcModelModifiedTime(GameObject gameObject)
		{
			ulong time = 0ul;

			// Prefab time
			AppendObjectModifiedTime(gameObject, ref time);

			// Materials
			foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>())
			{
				foreach (Material material in renderer.sharedMaterials)
				{
					if (!material)
						continue;

					ulong matTime = CalcMaterialModifiedTime(material);
					if (matTime > time)
					{
						time = matTime;
					}
				}
			}

			// Meshes
			foreach (MeshFilter filter in gameObject.GetComponentsInChildren<MeshFilter>())
			{
				if (!filter.sharedMesh)
					continue;

				AppendObjectModifiedTime(filter.sharedMesh, ref time);
			}

			return time;
		}

		public static ulong CalcMaterialModifiedTime(Material material)
		{
			ulong time = 0ul;

			// Material time
			AppendObjectModifiedTime(material, ref time);

			if (material.shader)
			{
				// Shader time
            AppendObjectModifiedTime(material.shader, ref time);

            foreach (string texName in material.GetTexturePropertyNames())
            {
	            // Texture times
	            Texture texture = material.GetTexture(texName);
	            if (!texture)
		            continue;

	            AppendObjectModifiedTime(texture, ref time);
            }
			}

			return time;
		}


		private void ReadFromFile()
		{
			if (!File.Exists(m_DbFile))
				return;

			string data = File.ReadAllText(m_DbFile);

			TokenParser tp = new(data);
			while (!tp.IsAtEnd)
			{
				ReadOnlySpan<char> file = tp.ReadToken(supportCommentsAndQuotes: true);
				ulong time = tp.ReadUlong();

				m_Data[file.ToString()] = time;
			}
		}

		public void WriteToFile()
		{
			using FileStream stream = new(m_DbFile, FileMode.Create, FileAccess.Write);
			using StreamWriter writer = new(stream);

			foreach ((string file, ulong time) in m_Data)
			{
				writer.WriteLine($"\"{file}\" {time}");
			}
		}

		public static ulong GetObjectModifiedTime(Object unityObject)
		{
			string objectPath = AssetDatabase.GetAssetPath(unityObject);

			if (objectPath.IsNullOrEmpty())
				return 0ul;

			AssetImporter importer = AssetImporter.GetAtPath(objectPath);
			if (!importer)
				return 0ul;

			return importer.assetTimeStamp;
		}

		private static void AppendObjectModifiedTime(Object unityObject, ref ulong time)
		{
			ulong thisTime = GetObjectModifiedTime(unityObject);

			if (thisTime > time)
			{
				time = thisTime;
			}
		}
	}
}