//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.IO;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public class ManualStyles
	{
		private ManualStyles()
		{
			string manualPath = Path.Combine(TrembleConsts.EDITOR_GetTrembleInstallFolder(), "Editor", "Manual");
			string fontPath = Path.Combine(manualPath, "FNT_UbuntuMono.ttf");
			Font ubuntuMonoFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);

			string bookmarkOnPath = Path.Combine(manualPath, "T_Bookmark_On.png");
			m_BookmarkIconOn = AssetDatabase.LoadAssetAtPath<Texture2D>(bookmarkOnPath);
			string bookmarkOffPath = Path.Combine(manualPath, "T_Bookmark_Off.png");
			m_BookmarkIconOff = AssetDatabase.LoadAssetAtPath<Texture2D>(bookmarkOffPath);

			m_Text = new(EditorStyles.wordWrappedLabel) { fontSize = Mathf.FloorToInt(13 * s_Scale) };
			m_Code = new(EditorStyles.wordWrappedLabel) { font = ubuntuMonoFont, fontSize = Mathf.FloorToInt(13 * s_Scale)};
			m_CenteredGreyMiniLabel = new(EditorStyles.centeredGreyMiniLabel) { fontSize = Mathf.FloorToInt(9 * s_Scale)};

			m_Toggle = new(EditorStyles.toggle) { fontSize = Mathf.FloorToInt(13 * s_Scale) };
			m_Foldout = new(EditorStyles.foldout) { fontSize = Mathf.FloorToInt(13 * s_Scale) };

			m_Button = new("Button") { fontSize = Mathf.FloorToInt(13 * s_Scale) };
			m_VeryLargeButton = new(m_Button) { padding = new(30, 30, 30, 30), fontSize = Mathf.FloorToInt(20 * s_Scale) };
			m_B = new(m_Text) { fontStyle = FontStyle.Bold, fontSize = Mathf.FloorToInt(13 * s_Scale) };

			m_HelpBox = new(EditorStyles.helpBox) { fontSize = Mathf.FloorToInt(13 * s_Scale) };

			m_Title = new(m_B) { fontStyle = FontStyle.Bold, fontSize = Mathf.FloorToInt(24 * s_Scale) };

			m_H1 = new(m_B) { fontSize = Mathf.FloorToInt(20 * s_Scale) };
			m_H2 = new(m_B) { fontSize = Mathf.FloorToInt(18 * s_Scale) };
			m_H3 = new(m_B) { fontSize = Mathf.FloorToInt(16 * s_Scale) };
		}

		private static ManualStyles s_ManualStyles;
		private static float s_Scale = 1f;

		public static ManualStyles Styles
		{
			get
			{
				s_ManualStyles ??= new();
				return s_ManualStyles;
			}
		}

		public static float Scale
		{
			get => s_Scale;
			set
			{
				s_Scale = value;
				s_ManualStyles = new();
			}
		}

		private readonly Texture2D m_BookmarkIconOn;
		private readonly Texture2D m_BookmarkIconOff;

		private readonly GUIStyle m_Text;
		private readonly GUIStyle m_Code;
		private readonly GUIStyle m_CenteredGreyMiniLabel;

		private readonly GUIStyle m_Button;
		private readonly GUIStyle m_VeryLargeButton;
		private readonly GUIStyle m_Toggle;
		private readonly GUIStyle m_Foldout;
		private readonly GUIStyle m_B;

		private readonly GUIStyle m_HelpBox;

		private readonly GUIStyle m_Title;
		private readonly GUIStyle m_H1;
		private readonly GUIStyle m_H2;
		private readonly GUIStyle m_H3;

		public const float SPACE = 20f;


		public Texture2D BookmarkIconOn => m_BookmarkIconOn;
		public Texture2D BookmarkIconOff => m_BookmarkIconOff;

		public GUIStyle Text => m_Text;
		public GUIStyle Code => m_Code;
		public GUIStyle CenteredGreyMiniLabel => m_CenteredGreyMiniLabel;


		public GUIStyle Button => m_Button;
		public GUIStyle VeryLargeButton => m_VeryLargeButton;
		public GUIStyle Toggle => m_Toggle;
		public GUIStyle Foldout => m_Foldout;
		public GUIStyle B => m_B;

		public GUIStyle HelpBox => m_HelpBox;

		public GUIStyle Title => m_Title;

		public GUIStyle H1 => m_H1;
		public GUIStyle H2 => m_H2;
		public GUIStyle H3 => m_H3;
	}
}