//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

// Adapted from Unity3D-IconManager by Thundernerd.
// This is only a tiny portion of the original code, but in the interest of attributing to his good work,
// I'll include his licence and a link to the original.

// https://github.com/Thundernerd/Unity3D-IconManager/

// Original Licence follows:
// MIT License
//
// Copyright (c) [2020] [Christiaan Bloemendaal]
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial
// portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public static class EditorGameObjectIconUtil
	{
		public enum EditorIcon
		{
			GrayDiamond,
			BlueDiamond,
			TealDiamond,
			GreenDiamond,
			YellowDiamond,
			OrangeDiamond,
			RedDiamond,
			PurpleDiamond,

			MAX
		}

		public enum EditorLabel
		{
			Gray,
			Blue,
			Teal,
			Green,
			Yellow,
			Orange,
			Red,
			Purple,

			MAX
		}

		public static void SetEditorIcon(this GameObject go, EditorIcon icon)
		{
			//TODO(jwf): cache these!
			Texture2D texture = EditorGUIUtility.FindTexture($"sv_icon_dot{(int)icon+8}_pix16_gizmo");
			EditorGUIUtility.SetIconForObject(go, texture);
		}

		public static void SetEditorIcon(this GameObject go, EditorLabel label)
		{
			//TODO(jwf): cache these!
			Texture2D texture = EditorGUIUtility.FindTexture($"sv_label_{(int)label}");
			EditorGUIUtility.SetIconForObject(go, texture);
		}
	}
}