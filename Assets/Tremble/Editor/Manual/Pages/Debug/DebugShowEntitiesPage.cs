//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Debug/Entity Explorer")]
	public class DebugShowEntitiesPage : ManualPageBase
	{
		private FgdWriter m_FgdWriter;

		private string[] m_ClassList;
		private int m_SelectedClassIdx = 0;

		protected override void OnInit()
		{
			m_FgdWriter = TrembleSync.GatherEntities();

			m_ClassList = m_FgdWriter.BaseClasses
				.Select(bc => $"@BaseClass {bc.Name}")
				.Concat(m_FgdWriter.Classes.Select(c => $"@{c.Type}Class {c.Name}"))
				.Prepend("(select a class...)")
				.ToArray();
		}

		protected override void OnGUI()
		{
			m_SelectedClassIdx = EditorGUILayout.Popup(m_SelectedClassIdx, m_ClassList);

			int selectedClassIdx = m_SelectedClassIdx - 1;
			if (selectedClassIdx < 0)
			{
				Text("Select a class above to see its entity definition!");
				return;
			}

			FgdClass entityClass = (selectedClassIdx < m_FgdWriter.BaseClasses.Count)
				? m_FgdWriter.BaseClasses[selectedClassIdx]
				: m_FgdWriter.Classes[selectedClassIdx - m_FgdWriter.BaseClasses.Count];

			MemoryStream memoryStream = new();

			// Write to memory
			using StreamWriter writer = new(memoryStream, Encoding.ASCII);
			entityClass.WriteFgd(writer, TrembleSyncSettings.Get().ImportScale);
			writer.Flush();

			// Rewind and read
			memoryStream.Position = 0;
			using StreamReader reader = new(memoryStream, Encoding.ASCII);
			string snippet = reader.ReadToEnd().Replace("\t", "  ");

			CodeWithHeader($"[{entityClass.Type}] {entityClass.Name}", snippet);

			Text("Above is how the entity is passed to TrenchBroom. If you're familiar with FGD files,",
				"this will look very familiar! If not, you basically are looking to make sure the name",
				"is what you would expect, and any fields that you wish to edit on entities of that",
				"class are present.");
		}
	}
}