//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;

namespace TinyGoose.Tremble
{
	public struct Q3Map2VersionInfo
	{
		public int Major;
		public int Minor;
		public int Patch;
		public char Letter;
		public string GitHash;

		public override string ToString() => $"v{Major}.{Minor}.{Patch}{Letter}-git-{GitHash}";

		private static Q3Map2VersionInfo Empty => new() { Major = 0, Minor = 0, Patch = 0, Letter = 'x', GitHash = "deadbeef" };

		public static Q3Map2VersionInfo Parse(string version)
		{
			if (String.IsNullOrEmpty(version) || !version.ContainsInvariant("-") || !version.ContainsInvariant("."))
				return Empty;

			string[] rootVersionInfo = version.Split('-');
			if (rootVersionInfo.Length != 3)
				return Empty;

			string[] versionParts = rootVersionInfo[0].Split('.');
			if (versionParts.Length != 3)
				return Empty;

			Q3Map2VersionInfo info;
			info.Major = int.Parse(versionParts[0].Substring(1));
			info.Minor = int.Parse(versionParts[1]);
			info.Patch = int.Parse(versionParts[2].Substring(0, versionParts[2].Length - 1));
			info.Letter = versionParts[2][^1];
			info.GitHash = rootVersionInfo[2];

			return info;
		}
	}
}