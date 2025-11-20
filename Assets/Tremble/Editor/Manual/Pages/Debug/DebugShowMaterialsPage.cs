//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Debug/Material Explorer")]
	public class DebugShowMaterialsPage : ManualPageBase
	{
		private readonly Dictionary<string, List<Texture>> m_Textures = new();

		protected override void OnInit()
		{
			int texSize = TrembleSyncSettings.Get().MaterialExportSize.ToIntSize();

			string texturesPath = TrembleConsts.BASE_MATERIALS_PATH;
			foreach (string dir in Directory.GetDirectories(texturesPath))
			{
				string[] pngFiles = Directory.GetFiles(dir, "*.png");
				if (pngFiles.Length == 0)
					continue;

				List<Texture> textures = new(pngFiles.Length);

				foreach (string textureFile in pngFiles)
				{
					byte[] textureBytes = File.ReadAllBytes(textureFile);

					Texture2D texture = new(texSize, texSize);
					texture.name = Path.GetFileNameWithoutExtension(textureFile);
					texture.LoadImage(textureBytes);

					textures.Add(texture);
				}

				m_Textures[Path.GetFileName(dir)] = textures;
			}
		}

		protected override void OnDeInit()
		{
			foreach (List<Texture> textures in m_Textures.Values)
			{
				foreach (Texture texture in textures)
				{
					Texture.DestroyImmediate(texture);
				}
			}

			m_Textures.Clear();
		}

		protected override void OnGUI()
		{
			float thumbnailSize = 64f * ManualStyles.Scale;
			float labelSize = thumbnailSize + 40f * ManualStyles.Scale;
			int numWide = Mathf.Max(1, Mathf.FloorToInt(PageWidth / labelSize) - 2);

			foreach (string textureFolder in m_Textures.Keys)
			{
				H1(textureFolder);
				{
					int cell = 0;
					GUILayout.BeginHorizontal();
					{
						foreach (Texture texture in m_Textures[textureFolder])
						{
							GUILayout.BeginVertical();
							{
								GUILayout.BeginHorizontal(GUILayout.Width(labelSize));
								{
									GUILayout.Space((labelSize - thumbnailSize) / 2f);
									GUIContent content = new(texture, texture.name);
									GUILayout.Box(content, GUILayout.Width(thumbnailSize), GUILayout.Height(thumbnailSize));
								}
								GUILayout.EndHorizontal();

								GUILayout.Label(texture.name, ManualStyles.Styles.CenteredGreyMiniLabel, GUILayout.Width(labelSize));
							}
							GUILayout.EndVertical();

							// New row?
							if (++cell >= numWide)
							{
								cell = 0;
								GUILayout.FlexibleSpace();

								GUILayout.EndHorizontal();
								GUILayout.Space(1f);
								GUILayout.BeginHorizontal();
							}
						}

						if (cell <= numWide)
						{
							GUILayout.FlexibleSpace();
						}
					}
					GUILayout.EndHorizontal();
				}
			}
		}
	}
}