using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public enum MaterialExportSize
	{
		[InspectorName("16")] Size16,
		[InspectorName("32")] Size32,
		[InspectorName("64")] Size64,
		[InspectorName("128")] Size128,
		[InspectorName("256")] Size256,
		[InspectorName("512")] Size512,
		[InspectorName("1024")] Size1024,
		[InspectorName("2048")] Size2048,
	}

	public static class MaterialExportSizeUtils
	{
		public static MaterialExportSize GetDefault() => MaterialExportSize.Size64;

		public static string[] Names =>
			s_Names ??= Enum.GetValues(typeof(MaterialExportSize))
				.Cast<MaterialExportSize>()
				.Select(mes => typeof(MaterialExportSize)
					.GetMember(mes.ToString())[0]
					.GetCustomAttribute<InspectorNameAttribute>()?.displayName ?? mes.ToString())
				.ToArray();

		private static int[] IntSizes => s_Sizes ??= Names.Select(int.Parse).ToArray();

		public static int ToIntSize(this MaterialExportSize mes) => IntSizes[(int)mes];

		public static Vector2Int ToVector2IntSize(this MaterialExportSize mes) => new(mes.ToIntSize(), mes.ToIntSize());

		private static string[] s_Names;
		private static int[] s_Sizes;
	}
}