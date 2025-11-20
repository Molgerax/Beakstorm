//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Linq;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public static class TextureUtil
	{
		public static Texture2D GenerateColourTexture(Color32 colour)
		{
			int size = TrembleSyncSettings.Get().MaterialExportSize.ToIntSize();
			int sizeSqr = size * size;

			Texture2D texture = new(size, size);

			if (s_ColourBuffer == null || s_ColourBuffer.Length != sizeSqr)
			{
				s_ColourBuffer = new Color32[sizeSqr];
			}

			for (int i = 0; i < sizeSqr; i++)
			{
				s_ColourBuffer[i] = colour;
				s_ColourBuffer[i].a = 0xFF;
			}

			texture.SetPixels32(s_ColourBuffer);
			return texture;
		}

		public static Texture2D GenerateCheckerTexture(int checkerSize = 64) => GenerateCheckerTexture(Color.black, Color.white, checkerSize);

		public static Texture2D GenerateCheckerTexture(Color32 colour1, Color32 colour2, int checkerSize = 64)
		{
			int size = TrembleSyncSettings.Get().MaterialExportSize.ToIntSize();

			Texture2D texture = new(size, size);
			texture.SetPixels32(GenerateCheckerColours(size, colour1, colour2, checkerSize));
			return texture;
		}

		public static Texture2D GenerateClipTexture(Color32 colour1, Color32 colour2, Color32 clipText, int checkerSize = 64)
		{
			int size = TrembleSyncSettings.Get().MaterialExportSize.ToIntSize();

			Texture2D texture = new(size, size);
			Color32[] image = GenerateCheckerColours(size, colour1, colour2, checkerSize);
			ApplyClipTexture(image, size, clipText, checkerSize);
			texture.SetPixels32(image);

			return texture;
		}

		public static Texture2D GenerateSkipTexture(Color32 colour1, Color32 colour2, Color32 clipText, int checkerSize = 64)
		{
			int size = TrembleSyncSettings.Get().MaterialExportSize.ToIntSize();

			Texture2D texture = new(size, size);
			Color32[] image = GenerateCheckerColours(size, colour1, colour2, checkerSize);
			ApplySkipTexture(image, size, clipText, checkerSize);
			texture.SetPixels32(image);

			return texture;
		}

		private static Color32[] s_ColourBuffer;

		private static Color32[] GenerateCheckerColours(int size, Color32 colour1, Color32 colour2, int checkerSize = 64)
		{
			int sizeSqr = size * size;

			if (s_ColourBuffer == null || s_ColourBuffer.Length != sizeSqr)
			{
				s_ColourBuffer = new Color32[sizeSqr];
			}

			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					int xCoord = x / checkerSize;
					int yCoord = y / checkerSize;

					if ((xCoord + yCoord) % 2 == 0)
					{
						s_ColourBuffer[y * size + x] = colour1;
					}
					else
					{
						s_ColourBuffer[y * size + x] = colour2;
					}

					s_ColourBuffer[y * size + x].a = 0xFF;
				}
			}

			return s_ColourBuffer;
		}

		// Cheeky clip texture
		private const char _ = ' ';

		private static readonly string[] s_ClipTexture =
		{
			"", "", // 2-line space
			_ + "/--" + _ + "|  " + _ + "|" + _ + "/-|" + _ + _ + _,
			_ + "|  " + _ + "|  " + _ + " " + _ + "| |" + _ + _ + _,
			_ + "|  " + _ + "|  " + _ + "|" + _ + "+_/" + _ + _ + _,
			_ + "|  " + _ + "|  " + _ + "|" + _ + "|  " + _ + _ + _,
			_ + "|__" + _ + "|__" + _ + "|" + _ + "|  " + _ + _ + _,
			"", // 1-line space
		};

		private static readonly string[] s_SkipTexture =
		{
			"", "", // 2-line space
			_ + "/--" + _ + "| |" + _ + "|" + _ + "/-|" + _ + _ + _,
			_ + "|  " + _ + "| /" + _ + " " + _ + "| |" + _ + _ + _,
			_ + "---" + _ + "|/ " + _ + "|" + _ + "+-/" + _ + _ + _,
			_ + "  |" + _ + "|+ " + _ + "|" + _ + "|  " + _ + _ + _,
			_ + "--+" + _ + "| +" + _ + "|" + _ + "|  " + _ + _ + _,
			"", // 1-line space
		};

		private static void ApplyClipTexture(Color32[] image, int size, Color32 textColour, int checkerSize = 64)
			=> ApplyWordTexture(image, s_ClipTexture, size, textColour, checkerSize);

		private static void ApplySkipTexture(Color32[] image, int size, Color32 textColour, int checkerSize = 64)
			=> ApplyWordTexture(image, s_SkipTexture, size, textColour, checkerSize);

		private static void ApplyWordTexture(Color32[] image, string[] wordTexture, int size, Color32 textColour, int checkerSize = 64)
		{
			int clipWidth = 16;
			float clipScale = (float)size / (float)checkerSize;

			for (int x = 0; x < size; x++)
			{
				for (int y = 0; y < size; y++)
				{
					// Offset every other row
					int offset = ((y / checkerSize) & 1) == 0 ? 0 : checkerSize;

					// Scale the text
					int scaledX = (int)((x + offset) / clipScale);
					int scaledY = (int)(y / clipScale);

					// Flip rows (textures, amirite?!)
					int row = size - scaledY - 1;

					// Pull the pixel data from the array
					string wordRow = wordTexture[row % wordTexture.Length];
					char wordValue = wordRow.Length > 0 ? wordRow[scaledX % clipWidth] : _;

					if (wordValue != _)
					{
						// Write pixel if it's on
						image[y * size + x] = textColour;
					}
				}
			}
		}

		// Account for weird custom choices for main texture names
		private static readonly int[] LIKELY_MAIN_TEXTURE_IDS =
		{
			Shader.PropertyToID("_BaseColor"),
			Shader.PropertyToID("_Color"),
			Shader.PropertyToID("_maintex"),
			Shader.PropertyToID("_Maintex"),
		};
		public static bool TryGetMainTex(this Material material, out Texture2D mainTexture)
		{
			if (!material)
			{
				mainTexture = null;
				return false;
			}

			// First check for _MainTex or [MainTexture]
			mainTexture = material.mainTexture as Texture2D;

			if (!mainTexture)
			{
				foreach (int name in LIKELY_MAIN_TEXTURE_IDS)
				{
					if (!material.HasTexture(name))
						continue;

					mainTexture = material.GetTexture(name) as Texture2D;
					break;
				}
			}

			return mainTexture;
		}
	}
}