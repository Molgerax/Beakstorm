//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace TinyGoose.Tremble
{
	public class WadParser
	{
		private readonly StreamReadIO m_Stream;

		public WadParser(string filename)
		{
			m_Stream = new(filename);
		}

		public void WriteTextureToFile(in WadEntry entry, string outFile)
		{
			m_Stream.Seek(entry.Address);

			string name = m_Stream.ReadString(16);
			int width = m_Stream.ReadInt32();
			int height = m_Stream.ReadInt32();
			m_Stream.SkipBytes(16);

			byte[] palettedTextureData = m_Stream.ReadBytes(width * height);
			byte[] rgbTextureData = new byte[palettedTextureData.Length * 3];

			// Copy paletted to RGB, flipping as we go
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					int readOffset = y * width + x;

					int invY = height - y - 1;
					int writeOffset = (invY * width + x) * 3;

					ref Color32 colour = ref WadPalette.QuakePalette.Get(palettedTextureData[readOffset]);

					rgbTextureData[writeOffset + 0] = colour.r;
					rgbTextureData[writeOffset + 1] = colour.g;
					rgbTextureData[writeOffset + 2] = colour.b;
				}
			}

			byte[] pngBytes = ImageConversion.EncodeArrayToPNG(rgbTextureData, GraphicsFormat.R8G8B8_UNorm, (uint)width, (uint)height);
			File.WriteAllBytes(outFile, pngBytes);
		}

		public WadFile Parse()
		{
			m_Stream.Seek(0);

			string magic = new(m_Stream.ReadChars(4));
			if (!magic.EqualsInvariant("wad2"))
			{
				Debug.LogError($"File was not a WAD2 :(");
				return default;
			}

			int numEntries = m_Stream.ReadInt32();
			int dirOffset = m_Stream.ReadInt32();

			m_Stream.Seek(dirOffset);

			List<WadEntry> entries = new(numEntries);
			for (int entryIdx = 0; entryIdx < numEntries; entryIdx++)
			{
				int entryAddress = m_Stream.ReadInt32();
				int entrySize = m_Stream.ReadInt32();
				m_Stream.SkipBytes(4);
				char type = (char)m_Stream.ReadByte();
				m_Stream.SkipBytes(3);
				string name = new(m_Stream.ReadString(16));

				// Skip anything that's not a texture
				if (type != 'D')
					continue;

				entries.Add(new()
				{
					Address = entryAddress,
					Size = entrySize,
					Type = type,
					Name = name.Replace('*', '_') // * is not a valid name for an asset!
				});
			}

			return new()
			{
				Entries = entries
			};
		}
	}
}