//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Advanced/Custom Logic with IOnImportFromMapEntity")]
	public class OnImportFromMapEntityPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Sometimes, the automated import which takes entity properties from TrenchBroom cannot",
				"convert the entity fully, or you need to add custom processing logic to translate the",
				"data afterwards."
			);

			Text(
				"You can see an example of this in SheepSpawner.cs in the sample project.",
				"In this case, we are importing a Point Entity with various properties,",
				"but at runtime we set up a reference to a default prefab to spawn, if not set in TrenchBroom.",
				$"So, by using this interface and handling OnImportFromMapEntity({nameof(MapBsp)} mapBsp, {nameof(BspEntity)} entity),",
				"we can wire this up before the GameObject is saved into the imported map."
			);

			Text(
				"You might also want to add extra components to a Brush entity - for example,",
				"adding a Rigidbody:"
			);

			H1("Code sample");
			Code(
				$"[{FormatAttributeName(typeof(BrushEntityAttribute))}(\"rigidbody\", category: \"misc\")]",
				$"public class RigidbodyEntity : MonoBehaviour, {nameof(IOnImportFromMapEntity)}",
				"{",
				"    // Customisable mass for the rigidbody - but 1kg by default",
				"    [SerializeField] private float m_Mass = 1f;",
				"    ",
				"    // Called after importing this entity into the map.",
				$"    public void OnImportFromMapEntity({nameof(MapBsp)} mapBsp, {nameof(BspEntity)} entity)",
				"    {",
				"        // Turn all colliders for this mesh convex (required for Rigidbodies)",
				"        foreach (MeshCollider meshCollider in gameObject.GetComponentsInChildren<MeshCollider>())",
				"        {",
				"            meshCollider.convex = true;",
				"        }",
				"        ",
				"        // Finally add the rigidbody to the entity, and set its mass.",
				"        Rigidbody rb = gameObject.AddComponent<Rigidbody>();",
				"        rb.mass = m_Mass;",
				"    }",
"}");
		}
	}
}