//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace TinyGoose.Tremble.Editor
{
	public class Q3Map2DllDownloaderWindow : EditorWindow
	{
		private GUIStyle m_TitleStyle;
		private GUIStyle m_BodyStyle;
		private GUIStyle m_ButtonStyle;

		private Vector2 m_ScrollPosition;

		private int m_DownloadSize;

		private bool m_IsUnpacking;
		private bool m_IsInstalling;

		public static bool OpenIfRequired()
		{
			if (!Q3Map2DllInstallEngine.IsNewerQ3Map2Available)
				return false;

			if (Q3Map2DllInstallEngine.CanInstallInBackground)
			{
				// If it's a first install, and not the Tremble asset store project - we can do this in the background
				Q3Map2DllInstallEngine.StartDownloadAndInstallLatestQ3Map2();
				return false;
			}

			Q3Map2DllDownloaderWindow window = GetWindow<Q3Map2DllDownloaderWindow>(true, "Tremble First Install", true);

			Vector2 size = new(520f, 240f);
			window.minSize = size;
			window.maxSize = size;
			window.position = new(EditorGUIUtility.GetMainWindowPosition().center - size / 2f, size);
			window.Show();

			return true;
		}

		private async void OnEnable()
		{
			m_DownloadSize = await Q3Map2DllInstallEngine.GetDownloadSizeBytes();

			if (Q3Map2DllInstallEngine.State != Q3Map2DllDownloadEngineState.Downloading)
			{
				Q3Map2DllInstallEngine.Reset();
			}
		}

		private void OnGUI()
		{
			m_BodyStyle ??= new(EditorStyles.label) { wordWrap = true };
			m_TitleStyle ??= new(m_BodyStyle) { fontSize = 20 };
			m_ButtonStyle ??= new(GUI.skin.button) { fontSize = 14, fixedHeight = 30f };

			EditorGUILayoutUtil.Pad(50f, () =>
			{
				m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);
				{
					if (m_IsInstalling)
					{
						OnReloadingDomainGUI();
					}
					else
					{
						Q3Map2DllInstallState state = Q3Map2DllInstallEngine.InstallState;
						switch (state)
						{
							case Q3Map2DllInstallState.Installed:
								OnEverythingOkayGUI();
								break;
							case Q3Map2DllInstallState.NotInstalled:
								OnMissingQ3Map2GUI();
								break;
							case Q3Map2DllInstallState.InstalledButWrongVersion:
								OnNonMatchingQ3Map2VersionGUI();
								break;
						}
					}

				}
				GUILayout.EndScrollView();
			});
		}

		private void OnMissingQ3Map2GUI()
		{
			GUILayout.Label("Installation Required: Q3Map2 library", m_TitleStyle);
			GUILayout.Label("Before Tremble can import maps, it needs to download and install the Q3Map2 library into your project."
			                + " You only need to do this once, and it's very small.", m_BodyStyle);

			GUILayout.Space(10f);

			switch (Q3Map2DllInstallEngine.State)
			{
				case Q3Map2DllDownloadEngineState.Idle:
				{
					if (GUILayout.Button(m_DownloadSize == 0 ? "Download now" : $"Download now ({m_DownloadSize / 1024f / 1024f:F1}MB)", m_ButtonStyle))
					{
						Q3Map2DllInstallEngine.StartDownloadAndInstallLatestQ3Map2();
					}

					break;
				}

				case Q3Map2DllDownloadEngineState.Downloading:
				{
					GUILayout.Box(String.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(30f));
					Rect box = GUILayoutUtility.GetLastRect();
					EditorGUI.ProgressBar(box, Q3Map2DllInstallEngine.Progress, $"Downloading ({Q3Map2DllInstallEngine.Progress * 100f:F0}%)...");

					break;
				}

				case Q3Map2DllDownloadEngineState.Succeeded:
				{
					Close();
					break;
				}

				case Q3Map2DllDownloadEngineState.Failed:
				{
					EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Failed to download!", "Oh no, the download didn't work. Check your connection and try again.", "Fingers Crossed...");
					break;
				}
			}
		}

		private void OnNonMatchingQ3Map2VersionGUI()
		{
			GUILayout.Label("Update Required: Q3Map2 library", m_TitleStyle);
			GUILayout.Label("An older version of the Q3Map2 DLL is installed in your project and should be upgraded."
			                + " Unity will restart during the upgrade, so save your work first!", m_BodyStyle);

			GUILayout.Space(10f);

			if (GUILayout.Button("I'm Ready, Restart!", m_ButtonStyle))
			{
				if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
				{
					Q3Map2Dll.EDITOR_IsPluginsFolderFlaggedForDeletion = true;
					EditorApplication.OpenProject(Environment.CurrentDirectory);
				}
			}
		}

		private void OnEverythingOkayGUI()
		{
			GUILayout.Label("Q3Map2 Installed Successfully", m_TitleStyle);
			GUILayout.Label("Tremble is now ready to go.", m_BodyStyle);

			GUILayout.Space(10f);

			if (GUILayout.Button("Let's Gooooo!", m_ButtonStyle))
			{
				Close();
			}
		}

		private void OnReloadingDomainGUI()
		{
			GUILayout.Label("Hold on...", m_TitleStyle);
			GUILayout.Label("Tremble is initialising for the first time.", m_BodyStyle);

			GUILayout.Space(10f);

			GUILayout.Box(String.Empty, GUILayout.ExpandWidth(true), GUILayout.Height(30f));
			Rect box = GUILayoutUtility.GetLastRect();
			EditorGUI.ProgressBar(box, -1f, "Initialising...");
		}
	}
}