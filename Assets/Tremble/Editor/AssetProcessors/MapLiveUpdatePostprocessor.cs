//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace TinyGoose.Tremble.Editor
{
	public class MapLiveUpdatePostprocessor : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if (!Application.isPlaying || !TrembleSyncSettings.Get().UseLiveUpdate)
				return;

			// Get list of imported GUIDs as strings
			string[] importedGUIDs = importedAssets
				.Select(path => AssetDatabase.GUIDFromAssetPath(path).ToString())
				.ToArray();

			// If we reimport during gameplay, disable and re-enable the map to refresh the collision
			Scene scene = SceneManager.GetActiveScene();
			MapDocument[] updatedMaps = scene.GetRootGameObjects()
				.SelectMany(go => go.GetComponentsInChildren<MapDocument>())
				.Where(doc => importedGUIDs.Contains(doc.OriginalAssetGuid))
				.ToArray();

			foreach (MapDocument map in updatedMaps)
			{
				Stopwatch timer = new();
				timer.Start();

				string assetPath = AssetDatabase.GUIDToAssetPath(map.OriginalAssetGuid);
				GameObject prefabMap = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				MapDocument prefabMapDocument = prefabMap.GetComponent<MapDocument>();

				// Re-instance worldspawn
				GameObject newWorldspawn = InstanceNewWorldspawn(map, prefabMapDocument);

				// Spawn any new objects, and transfer vars to existing
				int numNewlySpawned = 0;
				int numUpdated = 0;
				foreach (Transform prefabChild in prefabMap.transform)
				{
					if (prefabChild.gameObject == prefabMapDocument.WorldspawnObject)
						continue;

					bool isNew = true;
					foreach (Transform mapChild in map.transform)
					{
						if (mapChild.name != prefabChild.name)
							continue;

						numUpdated++;

						// Object is static :( grab it from the map instead
						if (mapChild.gameObject.isStatic && HasEntityMoved(mapChild, prefabChild))
						{
							Debug.LogWarning($"Tremble LiveUpdate: Replacing static object '{mapChild.name}' with new copy. This could cause issues!");
							GameObject.Destroy(mapChild.gameObject);
							break;
						}

						if (TryFindTrembleComponent(prefabChild.gameObject, out Component prefabComp) &&
						    TryFindTrembleComponent(mapChild.gameObject, out Component mapComp))
						{
							// Disable, copy across data, re-enable
							mapComp.gameObject.SetActive(false);
							CopySafeSerialisedData(from: prefabComp, to: mapComp);
							mapComp.gameObject.SetActive(true);

							// Handle live update callbacks
							switch (mapComp)
							{
								case IOnLiveUpdate_Simple:
									mapComp.SendMessage("OnDestroy", SendMessageOptions.DontRequireReceiver);
									mapComp.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
									mapComp.SendMessage("Start", SendMessageOptions.DontRequireReceiver);
									break;

								case IOnLiveUpdate liveUpdateable:
									liveUpdateable.OnLiveUpdated();
									break;
							}
						}

						isNew = false;
						break;
					}

					if (isNew)
					{
						numNewlySpawned++;
						GameObject.Instantiate(prefabChild.gameObject, newWorldspawn.transform);
					}
				}

				timer.Stop();

				TryShowGameViewNotification($"Tremble LiveUpdate:\n{numNewlySpawned} new ents\n{numUpdated} updates\nin {timer.ElapsedMilliseconds}ms");
			}
		}

		private static bool HasEntityMoved(Transform mapChild, Transform prefabChild)
		{
			if (Vector3.SqrMagnitude(mapChild.position - prefabChild.position) > 0f)
				return true;

			if (Vector3.SqrMagnitude(mapChild.rotation.eulerAngles - prefabChild.rotation.eulerAngles) > 0f)
				return true;

			return false;
		}

		private static GameObject InstanceNewWorldspawn(MapDocument map, MapDocument prefabMapDocument)
		{
			// Remove worldspawn
			GameObject.Destroy(map.WorldspawnObject);

			// Reinstance worldspawn
			GameObject newWorldspawn = GameObject.Instantiate(prefabMapDocument.WorldspawnObject, map.transform, true);
			newWorldspawn.transform.localPosition = Vector3.zero;
			newWorldspawn.transform.localRotation = Quaternion.identity;
			map.Modify(so => so.FindProperty("m_WorldspawnObject").objectReferenceValue = newWorldspawn);
			newWorldspawn.transform.SetAsFirstSibling();

			// Disable and re-enable - if you don't do this, Unity does not update the colliders
			map.gameObject.SetActive(false);
			map.gameObject.SetActive(true);
			return newWorldspawn;
		}

		private static bool TryShowGameViewNotification(string note)
		{
			Type gameViewT = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");

			EditorWindow[] windows = Resources.FindObjectsOfTypeAll(gameViewT).Cast<EditorWindow>().ToArray();
			foreach (EditorWindow win in windows)
			{
				win.ShowNotification(new(note), 1f);
			}

			return windows.Length > 0;
		}

		private static bool TryFindTrembleComponent(GameObject go, out Component component)
		{
			foreach (Component c in go.GetComponents<Component>())
			{
				if (!c.GetType().HasCustomAttribute<EntityAttributeBase>())
					continue;
				
				component = c;
				return true;
			}

			component = default;
			return false;
		}

		private static void CopySafeSerialisedData(Component from, Component to)
		{
			// For now, we just copy simple data - floats, colours, vectors etc.
			// References and assets are a bit spoopier and we can't do too much there sadly!

			SerializedObject fromObject = new(from);
			SerializedObject toObject = new(to);

			SerializedProperty iterator = fromObject.GetIterator();
			iterator.NextVisible(true);

			do
			{
				if (iterator.propertyType
				    is SerializedPropertyType.ObjectReference
				    or SerializedPropertyType.ManagedReference)
				{
					if (iterator.objectReferenceValue is Component)
						continue;
				}

				toObject.CopyFromSerializedProperty(iterator);
			} while (iterator.NextVisible(false));

			toObject.ApplyModifiedPropertiesWithoutUndo();

			// Also copy basic positioning information
			Transform fromTransform = from.transform;
			Transform toTransform = to.transform;

			toTransform.localPosition = fromTransform.localPosition;
			toTransform.localRotation = fromTransform.localRotation;
		}
	}
}