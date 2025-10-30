//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Overview", isHomePage: true)]
	public class OverviewPage : ManualPageBase
	{
		private bool m_GotLatestVersions = false;

		protected override void OnInit()
		{
			VersionCheck.FetchLatestVersionsInBackground(() =>
			{
				m_GotLatestVersions = true;
				Repaint();
			});
		}

		protected override void OnGUI()
		{
			GUILayout.BeginHorizontal();
			{
				Image("T_TrembleLogo", width: 200f, centred: false);

				GUILayout.BeginVertical();
				{
					H1("Welcome to the Tremble manual!");
					Text(
						"Please click an item on the left to navigate. There are also buttons in the top",
						"toolbar for navigating back/forwards, and returning here (Home)."
					);


					PropertyDescription("Your version of Tremble", labelWidth: 200f, $"Version {TrembleConsts.VERSION_STRING}");

					if (!m_GotLatestVersions)
					{
						GUI.color = Color.yellow;
						PropertyDescription("Latest version of Tremble", "... checking if there's a newer version ...");
						GUI.color = Color.white;
					}
					else
					{
						if (VersionCheck.IsNewerVersionAvailable)
						{
							GUILayout.BeginVertical(EditorStyles.helpBox);
							{
								PropertyDescription("Latest version of Tremble", labelWidth: 200f, $"There's a newer version available, v{VersionCheck.NewestAvailableVersion.Version}",
										$"({VersionCheck.NumVersionsBehind} versions behind)!");

								ActionBar_SingleAction("Upgrade in Package Manager!", () => EditorApplication.ExecuteMenuItem("Window/Package Manager"));
							}
							GUILayout.EndVertical();
						}
						else
						{
							PropertyDescription("Latest version of Tremble", labelWidth: 200f, $"Version {VersionCheck.CurrentInstalledVersion.Version} - you're already on the latest and greatest!");
						}
					}

					if (TrenchBroomUtil.TrenchBroomPath == null)
					{
						PropertyDescription("TrenchBroom", labelWidth: 200f, "(unknown location!)");

						GUI.color = Color.yellow;
						Text(
							"Is TrenchBroom installed? Tremble is unsure where to find it. Try",
							"opening TrenchBroom now - I'll keep checking for it :)"
						);
						GUI.color = Color.white;
					}
					else
					{
						string path = TrenchBroomUtil.TrenchBroomPath;

#if UNITY_EDITOR_OSX
						// Under macOS, remove /Contents/MacOS/TrenchBroom
						int suffix = path.IndexOfInvariant("/Contents", caseSensitive: true);
						if (suffix != -1)
						{
							path = path.Substring(0, suffix);
						}
#endif

						string running = TrenchBroomUtil.IsTrenchBroomRunning ? " (running)" : "";
						PropertyDescription("TrenchBroom", labelWidth: 200f, $"TrenchBroom was discovered at: {path}{running}");
					}
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();

			LargeSpace();

         if (TrenchBroomUtil.TrenchBroomPath == null)
			{
				ActionBar_SingleAction("Download TrenchBroom", () => { Application.OpenURL("https://github.com/TrenchBroom/TrenchBroom/releases");});
			}

         H2("Getting Started");
         ActionBar(() =>
         {
	         Action("Create your first map", () => GoToPage(typeof(CreateMapPage)));
	         Action("Set Tremble up for User Generated Content", () => GoToPage(typeof(UgcSetupPage)));
	         Action("Get support!", () => GoToPage(typeof(TroubleshootingGetHelpPage)));
         });

         H2("Quick Create Entity");
         ActionBar(() =>
         {
	         Action("Create a POINT entity", () => GoToPage(typeof(PointEntitiesPage)));
	         Action("Create a BRUSH entity", () => GoToPage(typeof(BrushEntitiesPage)));
	         Action("Create a PREFAB entity", () => GoToPage(typeof(PrefabEntitiesPage)));
         });
		}
	}
}