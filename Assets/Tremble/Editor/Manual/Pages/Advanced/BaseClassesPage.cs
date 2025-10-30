//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Advanced/Base Classes")]
	public class BaseClassesPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text(
				"Sometimes, you want to use a field (or a set of fields) across multiple",
				"prefabs, point entities, and brush entities. For example, imagine you have a 'team' system,",
				"whereby different entities might be on a player 'team' or the enemy 'team'. Entities that",
				"you may wish to give a 'team' in this case could be the player prefab, some enemy prefabs,",
				"and some doors which can only be interacted with by things on the same 'team'."
			);

			Text(
				$"In this case, you can create a C# interface derived from {nameof(ITrembleBaseClass)}, which",
				"those entities or prefabs implement, in order to attach fields to all of",
				"them at once."
			);

			Text("Here's how this might look for our 'teams' example:");

			Code(
				$"using {nameof(TinyGoose)}.{nameof(Tremble)};",
				"",
				"// Simple enum to denote which team an entity is on (or for).",
            "public enum TeamType",
            "{",
            "    Neutral,",
            "    Players,",
            "    Enemies,",
            "}",
            "",
            "// Inherit your interface from ITrembleBaseClass in order to expose it to",
            "// Tremble.",
            $"public interface ITeam : {nameof(ITrembleBaseClass)}",
            "{",
            "    public TeamType Team => TeamType.Neutral; //you must provide a default!",
            "}"
			);

			Text("Now, attach the interface to your entities, like this:");

			Code(
				$"using {nameof(TinyGoose)}.{nameof(Tremble)};",
				"",
				$"[{FormatAttributeName(typeof(PrefabEntityAttribute))}]",
				"public class Door : MonoBehaviour, ITeam",
				"{",
				"",
				"}"
			);

			Text(
				"In order to read the data set from this base class, you will need to implement",
				"OnImportFromMapEntity. You can then store this data in a variable, or do whatever",
				"else you need with it."
			);

			Code(
				$"using {nameof(TinyGoose)}.{nameof(Tremble)};",
				"",
				$"[{FormatAttributeName(typeof(PrefabEntityAttribute))}]",
				$"public class Door : MonoBehaviour, ITeam, {nameof(IOnImportFromMapEntity)}",
				"{",
				"    // [SerializeField] so that Unity stores the result",
				"    [SerializeField] private TeamType m_Team; ",
				"    ",
				"    // We must implement this method and pull the data out manually",
				$"    public void OnImportFromMapEntity({nameof(MapBsp)} mapBsp, {nameof(BspEntity)} entity)",
				"    {",
				"        // Interestingly, because this is named `m_Team`, and our",
				"        // interface has `Team`, this would have been set",
				"        // automagically anyway. But it's better to be explicit!",
				"        m_Team = entity.GetBaseClassData<TeamType>(this, nameof(ITeam.Team));",
				"        Debug.Log($\"This door is usable by the {m_Team} team!\");",
				"    }",
				"}"
			);
		}
	}
}