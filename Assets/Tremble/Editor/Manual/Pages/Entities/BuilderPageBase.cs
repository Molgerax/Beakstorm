//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Diagnostics.Eventing.Reader;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public abstract class BuilderPageBase<TDataClass> : ManualPageBase
		where TDataClass : ScriptableObject
	{
		private TDataClass m_Sample;
		private SerializedObject m_SampleSO;
		private TrembleSyncSettings m_SyncSettings;

		protected virtual bool HasInitialQuestions { get; } = false;

		private bool m_DoneInitialQuestions = false;

		protected TDataClass Sample => m_Sample;
		protected SerializedObject SampleSO => m_SampleSO;
		protected TrembleSyncSettings SyncSettings => m_SyncSettings;

		protected override void OnInit()
		{
			m_Sample = ScriptableObject.CreateInstance<TDataClass>();
			m_SampleSO = new(m_Sample);

			m_SyncSettings = TrembleSyncSettings.Get();
		}

		protected sealed override void OnGUI()
		{
			if (HasInitialQuestions && !m_DoneInitialQuestions)
			{
				OnInitialQuestionsGUI();
				return;
			}

			OnPrePropertiesGUI();
			m_SampleSO.Update();
			{
				OnPropertiesGUI();
			}
			m_SampleSO.ApplyModifiedPropertiesWithoutUndo();
			OnPostPropertiesGUI();
		}

		protected virtual void OnInitialQuestionsGUI() { }
		protected virtual void OnPrePropertiesGUI() { }
		protected virtual void OnPropertiesGUI() { }
		protected virtual void OnPostPropertiesGUI() { }

		protected void MarkInitialQuestionsDone() => m_DoneInitialQuestions = true;

		protected bool RenderProperty(string propName, string title, string description, Type objectReferenceType = null, Action extraContent = null)
		{
			SerializedProperty prop = m_SampleSO.FindProperty(propName);
			uint hash = prop.GetHashOfContent();

			GUILayout.BeginVertical(EditorStyles.helpBox);
			{
				if (prop.propertyType == SerializedPropertyType.ObjectReference)
				{
					GUILayout.BeginHorizontal();
					{
						EditorGUILayout.PrefixLabel(new GUIContent(title));
						prop.objectReferenceValue = EditorGUILayout.ObjectField(prop.objectReferenceValue, objectReferenceType ?? typeof(GameObject), allowSceneObjects: false);
					}
					GUILayout.EndVertical();

					m_SampleSO.ApplyModifiedProperties();
				}
				else
				{
					EditorGUILayout.PropertyField(prop, new GUIContent(title));
				}

				GUILayout.Label(description, ManualStyles.Styles.Text);

				extraContent?.Invoke();
			}
			GUILayout.EndVertical();

			GUILayout.Space(5f);

			return hash != prop.GetHashOfContent();
		}
	}
}