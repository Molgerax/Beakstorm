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
	public abstract class ManualPageBase
	{
		private readonly Dictionary<string, Texture2D> m_Images = new();
		private int m_NextNumber;
		private ManualWindow m_ManualWindow;

		private readonly Dictionary<string, bool> m_FoldoutState = new();

		protected float PageWidth => m_ManualWindow.PageWidth;

		protected void Repaint() => m_ManualWindow.Repaint();

		internal void Init(ManualWindow window)
		{
			m_ManualWindow = window;
			OnInit();
		}

		internal void DeInit() => OnDeInit();

		internal void Render()
		{
			ResetNumbering();
			OnGUI();
		}

		protected virtual void OnInit() {}
		protected virtual void OnDeInit() {}
		protected abstract void OnGUI();

		protected void GoToPage(Type type)
		{
			m_ManualWindow.GoToPage(type);
		}

		protected void NYI()
		{
			GUI.color = Color.red;
			Text("This page is not yet implemented!!!");
			GUI.color = Color.grey;
			Text(GetType().Name.ToNamingConvention(NamingConvention.HumanFriendly));
			GUI.color = Color.white;
		}

		protected int Tabs(ref int selectedIdx, string[] tabNames)
		{
			selectedIdx = GUILayout.Toolbar(selectedIdx, tabNames, "LargeButton");
			return selectedIdx;
		}
		protected int Tabs(ref int selectedIdx, string[] tabNames, float width)
		{
			selectedIdx = GUILayout.Toolbar(selectedIdx, tabNames, "LargeButton", GUILayout.Width(width * ManualStyles.Scale));
			return selectedIdx;
		}


		protected void Image(string name, float width = 400f, bool centred = true)
		{
			if (!m_Images.TryGetValue(name, out Texture2D texture))
			{
				texture = TrembleAssetLoader.LoadAssetByName<Texture2D>(name);
				m_Images[name] = texture;
			}

			width *= ManualStyles.Scale;
			float height = texture ? texture.height * (width / texture.width) : width;

            var style = new GUIStyle("IN ThumbnailShadow");
            style.margin.bottom = 16;
            style.margin.top = 16;

			void RawImage()
			{
				if (texture)
				{
					GUILayout.Box(texture, style, GUILayout.Width(width), GUILayout.Height(height));
				}
				else
				{
					GUI.color = Color.red;
					GUILayout.Box($"<missing manual image '{name}'>", style, GUILayout.Width(width), GUILayout.Height(height));
					GUI.color = Color.white;
				}
			}

			if (centred)
			{
				Centre(RawImage);
			}
			else
			{
				RawImage();
			}
		}

		protected void Title(string text) => TextAndSpace(text, ManualStyles.Styles.Title);

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Text
		// -----------------------------------------------------------------------------------------------------------------------------
		protected void Text(string text) => TextAndSpace(text, ManualStyles.Styles.Text);
		protected void Text(params string[] text) => Text(String.Join(' ', text));
		protected void TextMultiline(params string[] text) => Text(String.Join('\n', text));
		protected void Text(string text, float width) => TextAndSpace(text, ManualStyles.Styles.Text, width);

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Bold
		// -----------------------------------------------------------------------------------------------------------------------------
		protected void Bold(string text) => TextAndSpace(text, ManualStyles.Styles.B);
		protected void Bold(params string[] text) => Bold(String.Join(' ', text));
		protected void BoldMultiline(params string[] text) => Bold(String.Join('\n', text));
		protected void Bold(string text, float width) => TextAndSpace(text, ManualStyles.Styles.B, width);

		protected void CheckListItem(string text, bool isChecked, string action = null, Action doIt = null)
		{
			Color oldColour = GUI.color;
			GUI.color = isChecked ? Color.green : Color.red;

			GUILayout.BeginHorizontal(EditorStyles.helpBox);
			{
				bool wasTicked = GUILayout.Toggle(isChecked, text, ManualStyles.Styles.Toggle, GUILayout.ExpandWidth(true));

				if (!isChecked && !action.IsNullOrEmpty() && doIt != null)
				{
					GUI.color = Color.green;
					if (GUILayout.Button(action, ManualStyles.Styles.Button, GUILayout.Width(200f)))
					{
						doIt();
					}
				}

				if (!isChecked && wasTicked && doIt != null)
				{
					doIt();
				}
			}
			GUILayout.EndHorizontal();

			GUI.color = oldColour;
		}

		protected void Foldout(string title, Action inside)
		{
			bool foldout = m_FoldoutState.GetValueOrDefault(title, false);
			foldout = EditorGUILayout.Foldout(foldout, title, true, ManualStyles.Styles.Foldout);

			SmallSpace();

			if (foldout)
			{
				Indent(inside);
			}

			SmallSpace();

			m_FoldoutState[title] = foldout;
		}


		protected void Indent(Action inside) => Indent(1, inside);
		protected void Indent(int amount, Action inside)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Space(20f * amount);

				GUILayout.BeginVertical();
				{
					inside();
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndHorizontal();
		}

		protected void Code(params string[] text) => CodeInternal(null, false, text);
		protected void CodeNoCopy(params string[] text) => CodeInternal(null, true, text);
		protected void CodeWithHeader(string header, params string[] text) => CodeInternal(header, false, text);
		protected void CodeWithHeaderNoCopy(string header, params string[] text) => CodeInternal(header, true, text);

		private void CodeInternal(string header, bool noCopy, params string[] text)
		{
			GUILayout.BeginVertical(EditorStyles.helpBox);
			{
				string textJoined = String.Join('\n', text);
				int numLines = textJoined.Count(c => c == '\n') + 1;

				GUILayout.BeginHorizontal();
				{
					header ??= "C# Source Code";

					GUI.color = Color.grey;
					GUILayout.Label($"{header}, {numLines} line(s)");
					GUI.color = Color.white;
					GUILayout.FlexibleSpace();

					if (!noCopy && GUILayout.Button("Copy to clipboard"))
					{
						GUIUtility.systemCopyBuffer = textJoined;
					}
				}
				GUILayout.EndHorizontal();

				GUILayout.Label(textJoined, ManualStyles.Styles.Code);
			}
			GUILayout.EndVertical();
		}

		protected void Button(string label, Action action, float width = 0f)
		{
			bool pressed = width > 0f
				? GUILayout.Button(label, GUILayout.Width(width))
				: GUILayout.Button(label);

			if (pressed)
				action();
		}

		protected void LargeButton(string label, Action action) => SizedButton(label, ManualStyles.Styles.VeryLargeButton, action);

		private void SizedButton(string label, GUIStyle style, Action action)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(label, style, GUILayout.ExpandWidth(false), GUILayout.Height(80f)))
					action();

				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}

		protected void Bullet(params string[] text) => BulletInternal("•", text);

		protected void ResetNumbering() => m_NextNumber = 1;
		protected void Number(params string[] text) => BulletInternal($"{m_NextNumber++})", text);

		private void BulletInternal(string bullet, params string[] text)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label(bullet, ManualStyles.Styles.B, GUILayout.Width(20f));
				GUILayout.Label(String.Join(' ', text), ManualStyles.Styles.Text);
			}
			GUILayout.EndHorizontal();
		}

		protected void PropertyDescription(string property, params string[] text)
			=> PropertyDescription(property, EditorGUIUtility.labelWidth, text);
		protected void PropertyDescription(string property, float labelWidth, params string[] text)
			=> CustomPropertyDescription(property, labelWidth, () => GUILayout.Label(String.Join(' ', text), ManualStyles.Styles.Text));

		protected void CustomPropertyDescription(string property, Action inside)
			=> CustomPropertyDescription(property, EditorGUIUtility.labelWidth, inside);
		protected void CustomPropertyDescription(string property, float labelWidth, Action inside)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.Label(property, ManualStyles.Styles.B, GUILayout.Width(labelWidth));
				inside();
			}
			GUILayout.EndHorizontal();
		}

		protected void Experimental() => Callout("This feature is experimental and may not work in all cases!");

		protected void Callout(params string[] text)
		{
			SmallSpace();

			GUIContent helpContent = new(String.Join('\n', text), EditorGUIUtility.IconContent("console.infoicon").image);
			GUILayout.Label(helpContent, ManualStyles.Styles.HelpBox);
		}


		protected void H1(string text) => TextAndSpace(text, ManualStyles.Styles.H1);

		protected void H2(string text) => TextAndSpace(text, ManualStyles.Styles.H2);

		protected void H3(string text) => TextAndSpace(text, ManualStyles.Styles.H3);

		//protected void H4(string text) => TextAndSpace(text, ManualStyles.Styles.H4);

		protected void B(string text) => GUILayout.Label(text, ManualStyles.Styles.B);

		private void TextAndSpace(string text, GUIStyle style)
		{
			GUILayout.Space(style.fontSize * ManualStyles.Scale);
			GUILayout.Label(text, style);
			SmallSpace();
		}

		private void TextAndSpace(string text, GUIStyle style, float width)
		{
			GUILayout.BeginVertical();
			{
				GUILayout.Space(style.fontSize * ManualStyles.Scale);
				GUILayout.Label(text, style, GUILayout.Width(width * ManualStyles.Scale));
				SmallSpace();
			}
			GUILayout.EndVertical();
		}

		protected void SmallSpace() => Space(0.25f);
		protected void LargeSpace() => Space(2.5f);
		protected void Space(float scale = 1f) => GUILayout.Space(ManualStyles.SPACE * scale * ManualStyles.Scale);

		protected void HorizontalLine(float height = 2f)
		{
			GUILayout.Space(5f);
			GUILayout.Box(Texture2D.whiteTexture, GUILayout.ExpandWidth(true), GUILayout.Height(height));
		}

		protected void Centre(Action inside)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				{
					inside();
				}
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}
		protected void RAlign(Action inside)
		{
			GUILayout.BeginHorizontal();
			{
				GUILayout.FlexibleSpace();
				inside();
			}
			GUILayout.EndHorizontal();
		}

		protected void ActionBar(Action inside)
		{
			Color oldColour = GUI.color;
			GUI.color = Color.yellow;

			GUILayout.BeginHorizontal(EditorStyles.helpBox);
			{
				GUILayout.Label("Try it:", ManualStyles.Styles.B, GUILayout.ExpandWidth(false));
				GUILayout.FlexibleSpace();
				inside();
			}
			GUILayout.EndHorizontal();

			SmallSpace();

			GUI.color = oldColour;
		}

		protected void Action(string action, Action inside)
		{
			if (GUILayout.Button(action, ManualStyles.Styles.Button, GUILayout.ExpandWidth(false)))
			{
				inside();
			}
		}

		protected void ActionBar_SingleAction(string action, Action inside)
			=> ActionBar(() => Action(action, inside));

		protected static string FormatAttributeName(Type attributeType)
		{
			string name = attributeType.Name;
			int attrIdx = name.LastIndexOfInvariant("Attribute", caseSensitive: true);

			return attrIdx == -1 ? name : name.Substring(0, attrIdx);
		}
	}
}