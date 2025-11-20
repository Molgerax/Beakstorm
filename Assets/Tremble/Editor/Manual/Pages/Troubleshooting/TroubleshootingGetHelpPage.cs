//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Troubleshooting/Get Help & Support")]
	public class TroubleshootingGetHelpPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			H1("Discord");
			Text(
				"If you need help, please first join us on our Discord server:",
				"https://discord.gg/aUTmYxbVHZ."
			);

			ActionBar_SingleAction("Open Discord", () => Application.OpenURL("https://discord.gg/aUTmYxbVHZ"));

			H1("Email Support");
			Text(
				"For further assistance, or if you do not have access to Discord, do",
				"not hesitate to contact us at: hello@tinygoose.com."
			);

			ActionBar_SingleAction("Send Email", () => Application.OpenURL("mailto:hello@tinygoose.com"));
		}
	}
}