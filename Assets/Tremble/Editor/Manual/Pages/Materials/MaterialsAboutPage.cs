//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Materials/About")]
	public class MaterialsAboutPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Tremble uses a novel approach compared to other map import tools.",
				"Many map import tools will allow users to create their own Textures folder for use",
				"with TrenchBroom, and will try to create materials based on those on import."
			);

			Text(
				"This results in materials in your project which use the built-in 'Standard'",
				"(or URP/HDRP 'Lit') shader."
			);

			Text(
				"Tremble does NOT work in this way. Tremble takes your existing Unity materials,",
				"and generates a Textures folder for you with an accurate representation of each",
				"Unity material as a texture."
			);

			H1("Why?!");
			Text(
				"Well, we believe that users don't want to spend time curating textures for",
				"TrenchBroom when ultimately what they care about is the Material looking good",
				"in Unity (where your players will see it). It's the same reason Tremble does",
				"not support Quake®'s lightmapping tech. Your game will use Unity's lighting,",
				"so this is where you should apply your scene's lighting setup."
			);

			H1("But really, what benefits does it give me?");
			Bullet("Your materials and shaders appear exactly how you set them up in Unity, in your game!");
			Bullet(
				"You are not restricted to using Standard/Lit shaders for your map materials! You",
				"can use custom shaders/Shader Graphs, including Unlit, bumpmapped, or entirely HLSL-driven",
				"materials."
			);
			Bullet(
				"There's no need to worry about how TrenchBroom handles textures - Tremble",
				"keeps everything in sync automagically!"
			);
		}
	}
}