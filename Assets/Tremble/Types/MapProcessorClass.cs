//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using UnityEngine;
using Assembly = System.Reflection.Assembly;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	[Serializable]
	public class MapProcessorClass
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Serialised
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField] private string m_MapClassName;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Static lookup
		// -----------------------------------------------------------------------------------------------------------------------------
		private static readonly Dictionary<string, Type> s_MapClassLookup = new();

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private Type m_Class;

		public MapProcessorClass(string mapClassName = null)
		{
			m_MapClassName = mapClassName;
		}
		public MapProcessorClass(Type mapClass)
		{
			m_Class = mapClass;
			m_MapClassName = mapClass.FullName;
		}

		public static implicit operator MapProcessorClass(Type t) => new(t);

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Equality
		// -----------------------------------------------------------------------------------------------------------------------------
		protected bool Equals(MapProcessorClass other) => m_MapClassName == other.m_MapClassName;

		public override bool Equals(object obj)
		{
			if (obj is null)
				return false;
			if (ReferenceEquals(this, obj))
				return true;

			return obj.GetType() == GetType() && Equals((MapProcessorClass) obj);
		}
		public override int GetHashCode() => m_MapClassName?.GetHashCode() ?? 0;

		public static bool operator ==(MapProcessorClass left, MapProcessorClass right) => Equals(left, right);
		public static bool operator !=(MapProcessorClass left, MapProcessorClass right) => !Equals(left, right);

		public Type Class
		{
			get
			{
				// Have cached type?
				if (m_Class != null) 
					return m_Class;

				m_Class = GetClassWithName(m_MapClassName);
				return m_Class;
			}
		}
		
		public static Type GetClassWithName(string className)
		{
#if UNITY_EDITOR
			// Unsupported - but hopefully fine!
			return Unsupported.GetTypeFromFullName(className);
#else
			if (className == null)
				return null;
	  
			if (s_MapClassLookup.Count == 0)
			{
				GenerateClassLookup();
			}

			// Try looking up - we should refresh if not
			if (s_MapClassLookup.TryGetValue(className, out Type foundClass))
				return foundClass;
			
			return null;
#endif
		}
		
		public MapProcessorBase CreateInstance() => (MapProcessorBase)Activator.CreateInstance(Class);
		
		public bool IsValid => Class != null;
		public bool IsA<T>() => Class == typeof(T) || Class.IsSubclassOf(typeof(T));
		
		private static void GenerateClassLookup()
		{
			List<Type> typesFound = new(1024);
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
				typesFound.AddRange(assembly.GetTypes());

			s_MapClassLookup.Clear();
					
			foreach (Type availableType in typesFound)
			{
				string fullName = availableType.FullName;
						
				if (fullName == null)
					continue;
				
				s_MapClassLookup[fullName] = availableType;
			}
		}
	}
}