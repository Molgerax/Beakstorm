//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;

namespace TinyGoose.Tremble
{
	[Serializable]
	public class FgdComment : FgdFieldBase
	{
		public string Comment { get; init; }

		public override void Write(StreamWriter sw)
		{
			sw.WriteLine($"// {Comment}");
		}

		public override void WriteCSharpClass(StreamWriter sw, int indent)
		{
			sw.WriteIndent(indent);
			sw.WriteLine($"// {Comment}");
		}

		public override void WriteCSharpInterface(StreamWriter sw, int indent)
		{
			sw.WriteIndent(indent);
			sw.WriteLine($"// {Comment}");
		}
	}
}