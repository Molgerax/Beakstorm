//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using UnityEditor;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Advanced/Pipeline")]
	public class PipelinePage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text("When Tremble imports a map, it performs a number of processing steps on it.");

			Text(
				"From the Pipeline settings, you can choose which steps to perform, and which",
				"custom map processors (if any) you want to run. You can speed up import",
				"by disabling any steps which your game does not use."
			);

			Text("The settings looks like this:");

			Image("T_Manual_Pipeline");

			Callout(
				"Note that each step shows how long it took to run, the last time",
				"a map was imported."
			);

			ActionBar_SingleAction("Open Pipeline Settings", () =>
			{
				const string TREMBLE_PIPELINE_PATH = "Project/Tremble/4 Pipeline";
				SettingsService.OpenProjectSettings(TREMBLE_PIPELINE_PATH);
			});
		}
	}
}