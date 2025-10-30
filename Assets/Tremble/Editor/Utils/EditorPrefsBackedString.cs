//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	public class EditorPrefsBackedString
	{
		private readonly string m_Key;
		private string m_Value;

		public EditorPrefsBackedString(string key)
		{
			m_Key = key;
		}

		public string Value
		{
			get
			{
				if (m_Value == null)
				{
					if (!EditorPrefs.HasKey(m_Key))
						return null;

					m_Value = EditorPrefs.GetString(m_Key);
				}

				return m_Value;
			}

			set
			{
				m_Value = value;

				if (value == null)
				{
					EditorPrefs.DeleteKey(m_Key);
					return;
				}

				EditorPrefs.SetString(m_Key, value);
			}
		}

		public string[] Values
		{
			get
			{
				if (m_Value == null)
				{
					if (!EditorPrefs.HasKey(m_Key))
						return Array.Empty<string>();

					m_Value = EditorPrefs.GetString(m_Key);
				}

				return m_Value.IsNullOrEmpty() ? Array.Empty<string>() : m_Value.Split(';');
			}

			set
			{
				if (value == null)
				{
					m_Value = null;
					EditorPrefs.DeleteKey(m_Key);
					return;
				}

				string joinedValue = String.Join(';', value);
				m_Value = joinedValue;

				EditorPrefs.SetString(m_Key, joinedValue);
			}
		}
	}
}