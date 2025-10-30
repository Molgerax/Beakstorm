//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public class ManualWindow : EditorWindow
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Statics - for storing state between reloads
		// -----------------------------------------------------------------------------------------------------------------------------
		private const string MANUAL_PREFIX = "Manual_";
		private const int MAX_BOOKMARKS = 5;

		private static readonly EditorPrefsBackedString s_Path = new(MANUAL_PREFIX + nameof(s_Path));
		private static readonly EditorPrefsBackedString s_HomePath = new(MANUAL_PREFIX + nameof(s_HomePath));
		private static readonly EditorPrefsBackedString s_SortOrderFirst = new(MANUAL_PREFIX + nameof(s_SortOrderFirst));
		private static readonly EditorPrefsBackedString s_SortOrderLast = new(MANUAL_PREFIX + nameof(s_SortOrderLast));
		private static readonly EditorPrefsBackedString s_Bookmarks = new(MANUAL_PREFIX + nameof(s_Bookmarks));

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private Vector2 m_ListScrollPos;
		private Vector2 m_PageScrollPos;

		private ManualEntry m_CurrentNode;
		private ManualPageBase m_CurrentPage;

		private ManualEntry m_TreeRoot;
		private readonly Dictionary<string, Type> m_PageClasses = new();

		private GUIStyle m_SelectedItemStyle;

		private float m_TreeViewWidth = 235f;
		private bool m_IsResizing;
		private readonly List<string> m_History = new();
		private int m_HistoryPtr = -1;

		public float PageWidth => position.width - m_TreeViewWidth - 20f;

		public static void OpenManual(string windowTitle, Type startPage = null, string[] sortOrderFirst = null, string[] sortOrderLast = null)
		{
			string startPath = null;

			if (startPage != null)
			{
				startPage.GetCustomAttributes(out ManualPageAttribute pageInfo);
				if (pageInfo == null)
				{
					Debug.LogWarning($"Tried to open manual to page: {startPage.Name} - which is not a valid page - going home...");
				}
				else
				{
					startPath = pageInfo.Path;
				}
			}

			OpenManual(windowTitle, startPath, sortOrderFirst, sortOrderLast);
		}

		public static void OpenManual(string windowTitle, string startPath = null, string[] sortOrderFirst = null, string[] sortOrderLast = null)
		{
			s_Path.Value = startPath;
			s_SortOrderFirst.Values = sortOrderFirst ?? Array.Empty<string>();
			s_SortOrderLast.Values = sortOrderLast ?? Array.Empty<string>();

			ManualWindow window = GetWindow<ManualWindow>(true, windowTitle, true);

			Vector2 size = new(1200f, 640f);
			window.minSize = new(800f, 400f);
			window.position = new(EditorGUIUtility.GetMainWindowPosition().center - size / 2f, size);
			window.Show();
		}

		private void OnEnable()
		{
			s_HomePath.Value = null;
			m_TreeRoot = new() { Title = "" };

			TypeCache.TypeCollection pages = TypeCache.GetTypesWithAttribute<ManualPageAttribute>();
			foreach (Type pageClass in pages)
			{
				pageClass.GetCustomAttributes(out ManualPageAttribute pageInfo);

				if (pageInfo.IsHomePage)
				{
					if (s_HomePath.Value != null)
					{
						Debug.LogError($"Multiple home pages in manual! Was: {s_HomePath.Values}, now using: {pageInfo.Path}!");
					}
					s_HomePath.Value = pageInfo.Path;
				}

				m_PageClasses[pageInfo.Path] = pageClass;

				List<string> pathList = pageInfo.Path.Split("/", StringSplitOptions.RemoveEmptyEntries).ToList();
				CreateHierarchy_Recursive(m_TreeRoot, pathList, pageInfo);
			}

			SortHierarchy_Recursive(m_TreeRoot, s_HomePath.Value);
			GoToPage(s_Path.Value ?? s_HomePath.Value);
		}

		public void GoToPage(Type type, bool noUpdateHistory = false)
		{
			type.GetCustomAttributes(out ManualPageAttribute manualPageAttribute);
			if (manualPageAttribute == null)
			{
				Debug.LogError($"Type {type.Name} is not a [ManualPage]");
				return;
			}

			GoToPage(manualPageAttribute.Path, noUpdateHistory);
		}
		public void GoToPage(string path, bool noUpdateHistory = false)
		{
			if (!noUpdateHistory)
			{
				if (m_History.Count > 0 && m_HistoryPtr > -1 && m_HistoryPtr != m_History.Count - 1)
				{
					m_History.RemoveRange(m_HistoryPtr + 1, m_History.Count - m_HistoryPtr - 1);
				}

				m_History.Add(path);
				m_HistoryPtr = m_History.Count - 1;
			}

			if (!m_PageClasses.TryGetValue(path, out Type pageType))
			{
				EditorUtility.DisplayDialog("Invalid Page", $"Could not go to page with path '{path}' - maybe it was removed or moved?", "Oh no!");
				RemoveBookmark(path);
				return;
			}

			m_CurrentPage?.DeInit();
			{
				m_CurrentNode = m_TreeRoot.FindEntryByPath_Recursive(path);
				m_CurrentPage = (ManualPageBase)Activator.CreateInstance(pageType);
			}
			m_CurrentPage.Init(this);

			s_Path.Value = path;
			m_PageScrollPos = Vector2.zero;

			ExpandHierarchy(path);
		}

		private void OnGUI()
		{
			if (m_SelectedItemStyle == null)
			{
				Color lightBlue = new(0f, 0.6f, 1f);
				m_SelectedItemStyle = new(EditorStyles.wordWrappedLabel)
				{
					fontStyle = FontStyle.Bold,
					normal = { textColor = lightBlue },
					focused = { textColor = lightBlue },
					hover = { textColor = lightBlue }
				};
			}

			DrawToolbar();

			// Horizontal separator
			GUILayout.Box(GUIContent.none, GUILayout.ExpandWidth(true), GUILayout.Height(2f));

			GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
			{
				DrawTreeView(m_TreeViewWidth);
				DrawDraggableSeparator();
				DrawContent();

				DrawBookmarkOverlayButton();
			}
			GUILayout.EndHorizontal();
		}

		private void DrawToolbar()
		{
			GUILayout.BeginHorizontal();
			{
				EditorGUI.BeginDisabledGroup(m_HistoryPtr < 1);
				{
					if (GUILayout.Button("<", GUILayout.Width(20f)))
					{
						m_HistoryPtr--;
						GoToPage(m_History[m_HistoryPtr], noUpdateHistory: true);
					}
				}
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(m_HistoryPtr >= m_History.Count - 1);
				{
					if (GUILayout.Button(">", GUILayout.Width(20f)))
					{
						m_HistoryPtr++;
						GoToPage(m_History[m_HistoryPtr], noUpdateHistory: true);
					}
				}
				EditorGUI.EndDisabledGroup();

				EditorGUI.BeginDisabledGroup(m_CurrentNode?.PagePath.EqualsInvariant(s_HomePath.Value) ?? false);
				{
					if (GUILayout.Button("Home", GUILayout.Width(50f)))
					{
						m_History.Clear();
						m_HistoryPtr = -1;

						GoToPage(s_HomePath.Value);
					}
				}
				EditorGUI.EndDisabledGroup();

				// Bookmarks
				GUILayout.Space(5f);

				string[] bookmarks = s_Bookmarks.Values;
				Array.Sort(bookmarks, String.CompareOrdinal);

				if (bookmarks.Length > MAX_BOOKMARKS)
				{
					if (EditorGUILayout.DropdownButton(new("Bookmarks"), FocusType.Passive))
					{
						GenericMenu menu = new();
						foreach (string bookmark in bookmarks)
						{
							menu.AddItem(new(bookmark), false, () => GoToPage(bookmark));
						}

						menu.ShowAsContext();
					}
				}
				else
				{
					foreach (string bookmark in bookmarks)
					{
						EditorGUI.BeginDisabledGroup(s_Path.Value.EqualsInvariant(bookmark));
						{
							if (GUILayout.Button(new GUIContent(Path.GetFileName(bookmark)), EditorStyles.miniButton))
							{
								GoToPage(bookmark);
							}
						}
						EditorGUI.EndDisabledGroup();
					}
				}

				// GUI.color = Color.gray;
				// GUILayout.Label($"Path: {m_CurrentNode?.PagePath ?? "<none>"}");
				// GUI.color = Color.white;



				GUILayout.FlexibleSpace();

				GUILayout.Label($"Scale: {ManualStyles.Scale*100f:F0}%");

				float rawValue = GUILayout.HorizontalSlider(ManualStyles.Scale, 0.7f, 1.3f, GUILayout.Width(200f));
				ManualStyles.Scale = Mathf.FloorToInt(rawValue * 10f) / 10f;

				EditorGUI.BeginDisabledGroup(ManualStyles.Scale == 1f);
				{
					if (GUILayout.Button(new GUIContent("x", "Reset Zoom")))
					{
						ManualStyles.Scale = 1f;
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			GUILayout.EndHorizontal();
		}

		private void DrawTreeView(float width)
		{
			// Tree view
			m_ListScrollPos = GUILayout.BeginScrollView(m_ListScrollPos, GUILayout.Width(width));
			{
				DrawHierarchy_Recursive(m_TreeRoot, isRoot: true);
			}
			GUILayout.EndScrollView();
		}

		private void DrawDraggableSeparator()
		{
			GUILayout.Box(GUIContent.none, GUILayout.Width(4f), GUILayout.ExpandHeight(true));

			Rect resizeHandleRect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect(resizeHandleRect, MouseCursor.ResizeHorizontal);

			if (resizeHandleRect.Contains(Event.current.mousePosition))
			{
				// Mouse down while over - start resizing
				if (Event.current.type == EventType.MouseDown)
				{
					m_IsResizing = true;
					Event.current.Use();
				}
			}

			// Mouse up - no resizing
			if (Event.current.type == EventType.MouseUp)
			{
				m_IsResizing = false;
			}

			// If currently resizing, update the m_TreeViewWidth based on mouse movement
			if (m_IsResizing && Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
			{
				m_TreeViewWidth += Event.current.delta.x;
				m_TreeViewWidth = Mathf.Clamp(m_TreeViewWidth, 10f, 500f);

				Event.current.Use();
			}
		}

		private void DrawContent()
		{
			if (m_CurrentPage != null)
			{
				EditorGUILayoutUtil.Pad(10f, () =>
				{
					m_PageScrollPos = GUILayout.BeginScrollView(m_PageScrollPos);
					{
						GUILayout.Label(m_CurrentNode.Title, ManualStyles.Styles.Title);
						m_CurrentPage.Render();
					}
					GUILayout.EndScrollView();
				});
			}
			else
			{
				GUILayout.Label("Please select an item on the left!", EditorStyles.centeredGreyMiniLabel, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			}
		}

		private void DrawBookmarkOverlayButton()
		{
			// Bookmark overlay button
			bool wasBookmarked = s_Bookmarks.Values.Contains(s_Path.Value);

			Rect buttonPosition = position;
			buttonPosition.x = position.width - 32f - 30f;
			buttonPosition.y = 26f;
			buttonPosition.width = 32f;
			buttonPosition.height = 32f;

			GUIContent bookmarkButton = wasBookmarked
				? new(ManualStyles.Styles.BookmarkIconOn, "Remove bookmark from this page")
				: new(ManualStyles.Styles.BookmarkIconOff, "Bookmark this page");

			if (GUI.Button(buttonPosition, bookmarkButton, ManualStyles.Styles.Text))
			{
				SetBookmark(s_Path.Value, !wasBookmarked);
			}
		}

		private void ExpandHierarchy(string path)
		{
			int endSlice = path.Length;

			// While the path still has '/' in it...
			while (endSlice != -1)
			{
				// Chop off after last slash (no-op on first run)
				path = path.Substring(0, endSlice);

				// Open the child, find the parent for further chopping
				EditorGUILayoutUtil.SetHeaderOpen(path, true);
				endSlice = path.LastIndexOf('/');
			}
		}

		private void SetBookmark(string path, bool on)
		{
			if (on)
			{
				AddBookmark(path);
			}
			else
			{
				RemoveBookmark(path);
			}
		}
		private void AddBookmark(string path)
		{
			s_Bookmarks.Values = s_Bookmarks.Values.Append(path).ToArray();
			ShowNotification(new("Page bookmarked!"));
		}

		private void RemoveBookmark(string path)
		{
			s_Bookmarks.Values = s_Bookmarks.Values.Where(p => p != path).ToArray();
			ShowNotification(new("Bookmark removed!"));
		}


		private void CreateHierarchy_Recursive(ManualEntry node, List<string> path, ManualPageAttribute pageInfo)
		{
			if (path.Count == 0)
				return;

			string newNodeTitle = path[0];
			path.RemoveAt(0);

			// Find existing parent, and go down that route
			foreach (ManualEntry existingChild in node.Children)
			{
				if (existingChild.Title.EqualsInvariant(newNodeTitle))
				{
					CreateHierarchy_Recursive(existingChild, path, pageInfo);
					return;
				}
			}

			// Or... create a new one, and go down that route!
			bool isLeaf = path.Count == 0;

			string newPagePath = isLeaf
				? pageInfo.Path
				: (node.PagePath.IsNullOrEmpty() ? newNodeTitle : $"{node.PagePath}/{newNodeTitle}");

			ManualEntry entry = new()
			{
				Title = isLeaf ? pageInfo.Title : newNodeTitle,
				PagePath = newPagePath,
				ShowInTree = pageInfo.ShowInTree
			};
			node.Children.Add(entry);

			CreateHierarchy_Recursive(entry, path, pageInfo);
		}

		private void SortHierarchy_Recursive(ManualEntry node, string homePath)
		{
			string[] sortOrderFirst = s_SortOrderFirst.Values;
			string[] sortOrderLast = s_SortOrderLast.Values;

			node.Children.Sort((c1, c2) =>
			{
				// Home path ALWAYS TOP
				if (c1.PagePath != null && c1.PagePath.EqualsInvariant(homePath))
					return -1;
				if (c2.PagePath != null && c2.PagePath.EqualsInvariant(homePath))
					return +1;

				// Specific FIRST sort order first?
				int c1FirstSort = Array.IndexOf(sortOrderFirst, c1.Title);
				int c2FirstSort = Array.IndexOf(sortOrderFirst, c2.Title);

				if (c1FirstSort != -1 || c2FirstSort != -1)
				{
					// One sorted, the other not - put sorted below
					if (c1FirstSort == -1)
						return +1;

					if (c2FirstSort == -1)
						return -1;

					// Both last-sorted!
					return c1FirstSort.CompareTo(c2FirstSort);
				}

				// Specific LAST sort order first?
				int c1LastSort = Array.IndexOf(sortOrderLast, c1.Title);
				int c2LastSort = Array.IndexOf(sortOrderLast, c2.Title);

				if (c1LastSort != -1 || c2LastSort != -1)
				{
					// One sorted, the other not - put sorted below
					if (c1LastSort == -1)
						return -1;

					if (c2LastSort == -1)
						return +1;

					// Both last-sorted!
					return c1LastSort.CompareTo(c2LastSort);
				}

				// Both no specific sort at all - check titles
				return String.Compare(c1.Title, c2.Title, StringComparison.Ordinal);

			});

			foreach (ManualEntry child in node.Children)
			{
				SortHierarchy_Recursive(child, homePath);
			}
		}

		private void DrawHierarchy_Recursive(ManualEntry entry, bool isRoot = false)
		{
			if (entry == null)
				return;

			// It's a leaf!
			if (entry.Children.Count == 0)
			{
				if (!entry.ShowInTree)
					return;

				bool isCurrentPage = entry == m_CurrentNode;

				GUILayout.BeginHorizontal();

				GUILayout.Space(26f);

				bool wasPressed = isCurrentPage
					? GUILayout.Button(entry.Title, m_SelectedItemStyle)
					: GUILayout.Button(entry.Title, EditorStyles.wordWrappedLabel);

				GUILayout.EndHorizontal();

				if (wasPressed)
				{
					GoToPage(entry.PagePath);
				}

				GUILayout.Space(5f);

				return;
			}

			if (isRoot)
			{
				foreach (ManualEntry child in entry.Children)
				{
					DrawHierarchy_Recursive(child);
				}
			}
			else
			{
				EditorGUILayoutUtil.CollapsibleHeader(entry.Title, entry.PagePath, () =>
				{
					GUILayout.Space(5f);

					foreach (ManualEntry child in entry.Children)
					{
						DrawHierarchy_Recursive(child);
					}
				});

				if (!EditorGUILayoutUtil.IsHeaderOpen(entry.PagePath))
				{
					GUILayout.Space(5f);
				}
			}
		}
	}
}