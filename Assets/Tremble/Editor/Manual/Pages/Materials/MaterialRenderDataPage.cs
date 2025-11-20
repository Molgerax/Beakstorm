//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Materials/Render Settings")]
	public class MaterialRenderDataPage : ManualPageBase
	{
		private TrembleSyncSettings m_SyncSettings;
		private MaterialGroup m_DefaultMaterialGroup;
		private Material[] m_UnexposedMaterials;

		protected override void OnInit()
		{
			m_SyncSettings = TrembleSyncSettings.Get();
		}

		protected override void OnGUI()
		{
			Text(
				@"When Tremble exports materials, it can convert your materials in a few different ways.",
                @"You can choose a material's render settings by finding that material in the editor,",
                @"and changing the following settings:"
			);

            Image("T_Manual_RenderSettings");

			H1("Render Mode");

            Bold("MainTex Mode");
            Bullet(
                @"Uses the main texture of the material as the texture exported to Trenchbroom.",
                @"This will work fine for most materials. Directly copies the texture to the export folder",
                "if the resolution is unmodified."
            );
            Bullet(
                @"If you're using a custom shader, make sure your material either has a texture named _MainTex,",
                @"or a texture marked as Main Texture. This can be done through ShaderLab markup with [MainTexture]",
                "for code shaders, or by marking the texture as a Main Texture in Shader Graph shaders.");
            Bullet(
                @"If the renderer fails to find a matching main texture, it will try a few other texture names",
                @"like _Color, _BaseColor, or other misspellings of _MainTex (just in case).");
            Bullet(
                @"If that also fails, the renderer will switch to Legacy Mode."
                    );

            Bold("Legacy Mode");
			Bullet(
				@"Renders the material using a camera system. ",
                @"This is useful for procedural materials, or any other material that doesn't have",
                @"a main texture parameter."
			);
            Bullet(
                @"Will be automatically used if MainTex mode fails."
            );

            H1("Resolution");
            Text(
                @"You can use these settings to set the resolution of a texture.",
                @"This is useful for textures that don't have the same resolution on the X and Y axis.",
                @"Note that the resolution affects UV mapping - in Trenchbroom, one pixel is one unit by default."
                );
		}
	}
}