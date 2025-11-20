//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.IO;

namespace TinyGoose.Tremble
{
	public static class StreamWriterUtil
	{
		public static void WriteIndent(this StreamWriter sw, int indent)
		{
			for (int i = 0; i < indent; i++)
			{
				sw.Write('\t');
			}
		}
	}
}