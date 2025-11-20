//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;
using UnityEngine;

namespace TinyGoose.Tremble
{
	// -----------------------------------------------------------------------------------------------------------------------------
	//		Writing
	// -----------------------------------------------------------------------------------------------------------------------------
	// For writing to the MD3 stream, we bypass BinaryWriter because it's MUCH slower.
	// I think it's because the BinaryWriter does Endianness and Encoding related things, which we
	// don't need for our limited use-case of ASCII-encoded MD3 models.
	public class StreamWriteIO : IDisposable
	{
		private const int BUFFER_SIZE_KB = 160; // 160kB buffer

		public StreamWriteIO(Stream stream)
		{
			m_Stream = stream;
		}
		public void Dispose()
		{
			Flush();
		}

		private readonly Stream m_Stream;
		private readonly byte[] m_Buffer = new byte[BUFFER_SIZE_KB * 1024];
		private int m_BufPtr;

		private readonly float[] m_Floats = new float[1];
		private readonly byte[] m_Bytes = new byte[4];

		public void WriteInt32(int value)
		{
			WriteByte((byte)(value & 0xFF));
			WriteByte((byte)((value >> 8) & 0xFF));
			WriteByte((byte)((value >> 16) & 0xFF));
			WriteByte((byte)((value >> 24) & 0xFF));
		}

		public void WriteFloat(float value)
		{
			// Block copy the float value's bytes into preallocated buffer
			m_Floats[0] = value;
			Buffer.BlockCopy(m_Floats, 0, m_Bytes, 0, 4);

			// Write out
			WriteByte(m_Bytes[0]);
			WriteByte(m_Bytes[1]);
			WriteByte(m_Bytes[2]);
			WriteByte(m_Bytes[3]);
		}

		public void WriteInt16(short value)
		{
			WriteByte((byte)(value & 0xFF));
			WriteByte((byte)((value >> 8) & 0xFF));
		}

		public void WriteNormalAsLatLong(Vector3 normal)
		{
			const float BYTE_TO_DEG = (255.0f / 360.0f) * Mathf.Rad2Deg;
			Vector3 q3Normal = normal.UnityToQ3Vector(scale: 1f);

			if (q3Normal is { x: 0, y: 0 })
			{
				WriteByte(q3Normal.z > 0 ? (byte)0x00 : (byte)0x80); // U
				WriteByte(0x00); // V
			}
			else
			{
				WriteByte((byte)(Mathf.Acos(q3Normal.z) * BYTE_TO_DEG)); // U
				WriteByte((byte)(Mathf.Atan2(q3Normal.y, q3Normal.x) * BYTE_TO_DEG)); // V
			}
		}

		public void WriteBytes(byte[] bytes)
		{
			foreach (byte b in bytes)
			{
				WriteByte(b);
			}
		}
		public void WriteBytes(byte[] buffer, int count)
		{
			for (int b = 0; b < count; b++)
			{
				WriteByte(buffer[b]);
			}
		}

		public void WriteString(string value)
		{
			foreach (char c in value)
			{
				WriteByte((byte)c);
			}
		}

		public void WriteString(string value, int length)
		{
			for (int i = 0; i < length; i++)
			{
				if (i < value.Length)
				{
					WriteByte((byte)value[i]);
				}
				else
				{
					WriteByte(0x00);
				}
			}
		}

		private void WriteByte(byte b)
		{
			if (m_BufPtr >= m_Buffer.Length)
			{
				Flush();
			}

			m_Buffer[m_BufPtr++] = b;
		}

		private void Flush()
		{
			if (m_BufPtr == 0)
				return;

			m_Stream.Write(m_Buffer, 0, m_BufPtr);
			m_BufPtr = 0;
		}
	}

	// -----------------------------------------------------------------------------------------------------------------------------
	//		Reading
	// -----------------------------------------------------------------------------------------------------------------------------
	public class StreamReadIO
	{
		private readonly float[] m_Floats = new float[1];
		private readonly byte[] m_Bytes = new byte[8];

		private readonly byte[] m_StreamContents;
		private int m_Offset;

		public StreamReadIO(string path)
		{
			m_StreamContents = File.ReadAllBytes(path);
		}

		public void Seek(int offset)
		{
			m_Offset = offset;
		}
		public int Position => m_Offset;

		public byte ReadByte()
		{
			return m_StreamContents[m_Offset++];
		}

		public void SkipBytes(int count)
		{
			m_Offset += count;
		}

		public short ReadInt16()
		{
			m_Bytes[0] = ReadByte();
			m_Bytes[1] = ReadByte();

			return (short)(m_Bytes[0] |  m_Bytes[1] << 8);
		}

		public int ReadInt32()
		{
			m_Bytes[0] = ReadByte();
			m_Bytes[1] = ReadByte();
			m_Bytes[2] = ReadByte();
			m_Bytes[3] = ReadByte();

			return (int)(m_Bytes[0] | m_Bytes[1] << 8 | m_Bytes[2] << 16 | m_Bytes[3] << 24);
		}

		public float ReadSingle()
		{
			// Block copy the bytes into a float, using a preallocated buffer
			m_Bytes[0] = ReadByte();
			m_Bytes[1] = ReadByte();
			m_Bytes[2] = ReadByte();
			m_Bytes[3] = ReadByte();
			Buffer.BlockCopy(m_Bytes, 0, m_Floats, 0, 4);

			// Return
			return m_Floats[0];
		}

		public char[] ReadChars(int numChars)
		{
			char[] result = new char[numChars];
			for (int i = 0; i < numChars; i++)
			{
				result[i] = (char)ReadByte();
			}
			return result;
		}

		public byte[] ReadBytes(int numBytes)
		{
			byte[] result = new byte[numBytes];

			Buffer.BlockCopy(m_StreamContents, m_Offset, result, 0, numBytes);
			m_Offset += numBytes;

			return result;
		}

		public string ReadString(int numChars)
		{
			string result = new(ReadChars(numChars));
			int nulCharacter = result.IndexOf('\0');

			return nulCharacter > 0 ? result.Substring(0, nulCharacter) : result;
		}

		public Vector3 ReadVector3(bool convertToQ3 = false, float scale = 1f)
		{
			Vector3 result = new(ReadSingle() * scale, ReadSingle() * scale, ReadSingle() * scale);
			return convertToQ3 ? result.Q3ToUnityVector() : result;
		}
		public Vector2 ReadUV()
			=> new(ReadSingle() + 0.5f, 1f - ReadSingle() + 0.5f); // Flip Y and offset by 0.5, 0.5

		public Color32 ReadColour()
			=> new(ReadByte(), ReadByte(), ReadByte(), ReadByte());

		public Vector3Int ReadVector3Int() => new(ReadInt32(), ReadInt32(), ReadInt32());

		public Vector2Int ReadVector2Int() => new(ReadInt32(), ReadInt32());
	}
}