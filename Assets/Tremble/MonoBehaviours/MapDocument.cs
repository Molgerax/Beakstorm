// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Scripting;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace TinyGoose.Tremble
{
	[Serializable]
	public struct IDMapping
	{
		[SerializeField] private string m_ID;
		[SerializeField] private GameObject[] m_Objects;
		
		public string ID => m_ID;
		public GameObject[] Objects => m_Objects;

		public static IDMapping Create(string name, GameObject[] objects) => new()
		{
			m_ID = name,
			m_Objects = objects,
		};
	}
	
	/// <summary>
	/// The root of a map file.
	/// Provides a way to connect entities in maps to MonoBehaviours outside of the map.
	/// </summary>
	[DefaultExecutionOrder(-1000),  Preserve, ExecuteInEditMode, AddComponentMenu("")]
	public class MapDocument : MonoBehaviour
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Exposed
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField, HideInInspector] private string m_BspName;
		[SerializeField, HideInInspector] private IDMapping[] m_EntityIDs;

		[SerializeField, HideInInspector] private string m_OriginalAssetGuid;
		[SerializeField, HideInInspector] private GameObject m_WorldspawnObject;
		[SerializeField, HideInInspector] private ulong m_MapImportedTime;

		private Q3Map2Result m_Result = Q3Map2Result.Succeeded;
		private string m_ResultReason;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Properties
		// -----------------------------------------------------------------------------------------------------------------------------
		public string BspName => m_BspName;
		public TWorldspawn GetWorldspawn<TWorldspawn>() where TWorldspawn : Worldspawn => m_WorldspawnObject.GetComponent<TWorldspawn>();

		public bool TryGetWorldspawn<TWorldspawn>(out TWorldspawn worldspawn) where TWorldspawn : Worldspawn
		{
			worldspawn = GetWorldspawn<TWorldspawn>();
			return worldspawn;
		}

		public Q3Map2Result Result => m_Result;
		public string ResultReason => m_ResultReason;

#if UNITY_EDITOR
		public IDMapping[] EntityIDs => m_EntityIDs;
		public string OriginalAssetGuid => m_OriginalAssetGuid;
		public GameObject WorldspawnObject => m_WorldspawnObject;
		public ulong MapImportedTime => m_MapImportedTime;
#endif

		[Conditional("UNITY_EDITOR")]
		private void OnEnable()
		{
#if UNITY_EDITOR
			TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();

			if (syncSettings.PrefabOverrideHandling != PrefabOverrideHandling.AllowOverrides)
			{
				bool hasAnyOverrides = PrefabUtility.HasPrefabInstanceAnyOverrides(gameObject, true);

				List<AddedComponent> addedComponents = PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) ? PrefabUtility.GetAddedComponents(gameObject) : new();
				List<RemovedComponent> removedComponents = PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) ? PrefabUtility.GetRemovedComponents(gameObject) : new();

				int numChangedItems = addedComponents.Count + removedComponents.Count;

#if UNITY_2022_1_OR_NEWER
				List<RemovedGameObject> removedGOs = PrefabUtility.IsAnyPrefabInstanceRoot(gameObject) ? PrefabUtility.GetRemovedGameObjects(gameObject) : new();
				numChangedItems += removedGOs.Count;
#endif

				if (hasAnyOverrides || numChangedItems > 0)
				{
					bool needsRevert = false;

					foreach (PropertyModification propChanged in PrefabUtility.GetPropertyModifications(gameObject))
					{
						if (!propChanged.target)
							continue;

						if (propChanged.propertyPath.EqualsInvariant("name", caseSensitive: false) ||
						    propChanged.propertyPath.EqualsInvariant("m_Name", caseSensitive: false))
							continue;

						// TextMeshPro adds a hidden MeshFilter with overriden props, and then hides it. We can ignore this
						// one since the user can't see it!
						if ((propChanged.target.hideFlags & HideFlags.HideInHierarchy) != 0)
							continue;

						string niceName = propChanged.propertyPath.Split('.')[0].ToNamingConvention(NamingConvention.HumanFriendly);
						GameObject changedInstance = gameObject.Find_Recursive(propChanged.target.name);

						needsRevert = true;

						if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.WarnOnOverridesFound)
						{
							Debug.LogWarning($"Property override found: '{niceName}' ({propChanged.propertyPath}) on '{propChanged.target.name}' ({propChanged.target.GetType().Name}) - please change this value in TrenchBroom instead of Unity!", changedInstance);
						}
						else if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.AutomaticallyRevert)
						{
							Debug.LogWarning($"Auto-reverted overridden property '{niceName}' ({propChanged.propertyPath}) on '{propChanged.target.name}' ({propChanged.target.GetType().Name}) - please change this value in TrenchBroom instead of Unity!", changedInstance);
						}
					}

					foreach (AddedComponent addedComponent in addedComponents)
					{
						if (addedComponent.instanceComponent.GetType().Name.EqualsInvariant("UniversalAdditionalLightData", caseSensitive: true) ||
						    addedComponent.instanceComponent.GetType().Name.EqualsInvariant("HDAdditionalLightData", caseSensitive: true))
						{
							numChangedItems--;
							continue;
						}

						if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.WarnOnOverridesFound)
						{
							Debug.LogWarning($"Added component found: '{addedComponent.instanceComponent.GetType().Name}' on '{addedComponent.instanceComponent.name}' - please change this value in TrenchBroom instead of Unity!", addedComponent.instanceComponent);
						}
						else if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.AutomaticallyRevert)
						{
							Debug.LogWarning($"Auto-reverted added component '{addedComponent.instanceComponent.GetType().Name}' on '{addedComponent.instanceComponent.name}' - please change this value in TrenchBroom instead of Unity!", addedComponent.instanceComponent);
							addedComponent.Revert(InteractionMode.AutomatedAction);
						}
					}

					foreach (RemovedComponent removedComponent in removedComponents)
					{
						if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.WarnOnOverridesFound)
						{
							Debug.LogWarning($"Removed component found: '{removedComponent.assetComponent.GetType().Name}' on '{removedComponent.assetComponent.name}' - please change this value in TrenchBroom instead of Unity!", removedComponent.containingInstanceGameObject);
						}
						else if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.AutomaticallyRevert)
						{
							Debug.LogWarning($"Auto-reverted removed component '{removedComponent.assetComponent.GetType().Name}' on '{removedComponent.assetComponent.name}' - please change this value in TrenchBroom instead of Unity!", removedComponent.containingInstanceGameObject);
							removedComponent.Revert(InteractionMode.AutomatedAction);
						}
					}

#if UNITY_2022_1_OR_NEWER
					foreach (RemovedGameObject removedGO in removedGOs)
					{
						if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.WarnOnOverridesFound)
						{
							Debug.LogWarning($"Removed GameObject found: '{removedGO.assetGameObject.name}' - please change this value in TrenchBroom instead of Unity!", removedGO.parentOfRemovedGameObjectInInstance);
						}
						else if (syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.AutomaticallyRevert)
						{
							Debug.LogWarning($"Auto-reverted removed GameObject '{removedGO.assetGameObject.name}' - please change this value in TrenchBroom instead of Unity!", removedGO.parentOfRemovedGameObjectInInstance);
							removedGO.Revert(InteractionMode.AutomatedAction);
						}
					}
#endif

					if (needsRevert && syncSettings.PrefabOverrideHandling == PrefabOverrideHandling.AutomaticallyRevert)
					{
						PrefabUtility.RevertPrefabInstance(gameObject, InteractionMode.AutomatedAction);
					}
				}
			}
#endif
		}

		internal void INTERNAL_SetResult(Q3Map2Result result, string reason)
		{
			m_Result = result;
			m_ResultReason = reason;
		}

		internal void INTERNAL_SetData(string bspName, IDMapping[] ids, string originalAssetGuid, GameObject worldspawnObject, ulong mapImportedTime)
		{
			m_BspName = bspName;
			m_EntityIDs = ids;
			m_OriginalAssetGuid = originalAssetGuid;
			m_WorldspawnObject = worldspawnObject;
			m_MapImportedTime = mapImportedTime;
		}

		// Find a list of entities with a given id
		public bool Q(string id, out GameObject[] foundObjects)
		{
			if (m_EntityIDs == null || m_EntityIDs.Length == 0)
			{
				foundObjects = null;
				return false;
			}

			foreach (IDMapping mapping in m_EntityIDs)
			{
				if (!mapping.ID.EqualsInvariant(id))
					continue;

				foundObjects = mapping.Objects;
				return true;
			}

			foundObjects = Array.Empty<GameObject>();
			return false;
		}

		// Find a list of components from entities with a given id
		public bool Q<TComponent>(string id, out List<TComponent> foundComponents)
			where TComponent : Component
		{
			foundComponents = new();
			
			if (!Q(id, out GameObject[] foundObjects))
			{
				return false;
			}

			foreach (GameObject foundObject in foundObjects)
			{
				foundComponents.AddRange(foundObject.GetComponentsInChildren<TComponent>());
			}
			
			return foundComponents.Count > 0;
		}
		// Find a single entity with a id
		public bool Q(string id, out GameObject foundObject)
		{
			bool wasFound = Q(id, out GameObject[] foundObjects);

			foundObject = wasFound ? foundObjects[0] : null;
			return wasFound;
		}
		// Find a component on a single entity with a id
		public bool Q<TComponent>(string id, out TComponent foundComponent)
			where TComponent : Component
		{
			bool wasFound = Q(id, out List<TComponent> foundComponents);

			foundComponent = wasFound ? foundComponents[0] : null;
			return wasFound;
		}
		
		// Find a list of components from entities with a given id
		public bool Q(string id, Type componentType, out List<Component> foundComponents)
		{
			foundComponents = new();
			
			if (!Q(id, out GameObject[] foundObjects))
			{
				return false;
			}

			foreach (GameObject foundObject in foundObjects)
			{
				foundComponents.AddRange(foundObject.GetComponentsInChildren(componentType));
			}
			
			return foundComponents.Count > 0;
		}
		
		// Find a component on a single entity with a id
		public bool Q(string id, Type componentType, out Component foundComponent)
		{
			bool wasFound = Q(id, componentType, out List<Component> foundComponents);

			foundComponent = wasFound ? foundComponents[0] : null;
			return wasFound;
		}
	}
}