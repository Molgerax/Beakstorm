//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Reflection;

namespace TinyGoose.Tremble
{
	public class MapTypeLookup
	{
		public MapTypeLookup(TrembleSyncSettings syncSettings)
		{
			m_TypeNamingConvention = syncSettings.TypeNamingConvention;
			
			GenerateLookup();
		}
		
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Setup
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly NamingConvention m_TypeNamingConvention;
		
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private readonly Dictionary<string, Type> m_MapNameToClass = new();
		private readonly Dictionary<string, EntityType> m_MapNameToEntityType = new();
		private readonly Dictionary<Type, string> m_TypeToMapName = new();

		public IEnumerable<Type> AllTypes => m_TypeToMapName.Keys;

		public bool TryGetClassFromMapName(string mapName, out Type foundType)
		{
			return m_MapNameToClass.TryGetValue(mapName, out foundType);
		}
		public bool TryGetEntityTypeFromMapName(string mapName, out EntityType foundType)
		{
			return m_MapNameToEntityType.TryGetValue(mapName, out foundType);
		}
		public bool TryGetMapNameFromClass(Type type, out string foundName)
		{
			return m_TypeToMapName.TryGetValue(type, out foundName);
		}
		
		private void GenerateLookup()
		{
			foreach (Type type in TypeDatabase.GetTypesWithAttribute<EntityAttributeBase>())
			{
				if (type == typeof(TrembleSpawnablePrefab))
					continue;

				EntityAttributeBase attribute = type.GetCustomAttribute<EntityAttributeBase>();
				string baseName = attribute.TrenchBroomName ?? type.Name.ToNamingConvention(m_TypeNamingConvention);
				
				if (attribute is BrushEntityAttribute { BrushType: BrushType.Trigger }
				    && !baseName.ContainsInvariant("trigger")
				    && (attribute.Category == null || !attribute.Category.ContainsInvariant("trigger")))
				{
					UnityEngine.Debug.LogWarning($"Brush Entity of type '{type.Name}' is marked as a trigger. Consider adding 'trigger' into the name '{baseName}' so that TrenchBroom renders it appropriately.");
				}

				try
				{
					switch (attribute)
					{
						case PointEntityAttribute:
							string pointPrefix = attribute.Category ?? FgdConsts.POINT_PREFIX;
							string fullPointName = (pointPrefix.Length == 0) ? baseName : $"{pointPrefix}_{baseName}";

							m_MapNameToClass.Add(fullPointName, type);
							m_MapNameToEntityType.Add(fullPointName, EntityType.Point);
							m_TypeToMapName.Add(type, fullPointName);
							break;
						case BrushEntityAttribute:
							string brushPrefix = attribute.Category ?? FgdConsts.BRUSH_PREFIX;
							string fullBrushName = (brushPrefix.Length == 0) ? baseName : $"{brushPrefix}_{baseName}";

							m_MapNameToClass.Add(fullBrushName, type);
							m_MapNameToEntityType.Add(fullBrushName, EntityType.Brush);
							m_TypeToMapName.Add(type, fullBrushName);
							break;
					}
				}
				catch (ArgumentException)
				{
					UnityEngine.Debug.LogError($"Type '{type.Name}' or name '{baseName}' already used!}}");
				}
			}
		}
	}
}