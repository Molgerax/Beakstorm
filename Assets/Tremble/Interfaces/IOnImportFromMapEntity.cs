// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble
{
	/// <summary>
	/// Interface for [PrefabEntityEntity], [BrushEntity], and [PointEntity] classes, to
	/// perform additional processing after import from a map file.
	///
	/// If these modifications after import are complicated, need to occur using Editor
	/// functionality, or refer to other objects you may wish to consider instead
	/// using a custom Map Processor for your map to process them at the top level. 
	/// </summary>
	public interface IOnImportFromMapEntity
	{
		// Change ITrembleBaseClass if this changes!!
		void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity);
	}
}