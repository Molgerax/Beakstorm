//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble.Editor
{
	[ManualPage("Advanced/Custom Types")]
	public class CustomTypesPage : ManualPageBase
	{
		protected override void OnGUI()
		{
			Text("Tremble also supports syncing custom data types from your maps.");

			Text("For example, imagine you have an enemy that has a random amount of",
				"health, chosen between two values that you want to set in a map. You could of course add",
				"two fields, like this:"
			);
			CodeWithHeader("Individual fields sample",
				"[SerializeField] private int m_MinHealth = 10;",
				"[SerializeField] private int m_MaxHealth = 50;"
			);

			Text("However, you might prefer a single field of a custom type, something like this:");
			CodeWithHeader("Custom type sample",
				"[SerializeField] private IntInRange m_HealthRange = new(10, 50);");

			Text("To do this, first you need a custom type of course (well, you might already have one!)",
				"- in our example something like this:");
			CodeWithHeader("IntInRange.cs",
				"[Serializable]",
				"public struct IntInRange",
				"{",
				"    [SerializeField] public int Min;",
				"    [SerializeField] public int Max;",
				"",
				"    public override string ToString() => $\"{Min}-{Max}\";",
				"}");

			Text("Now we have our class to store our data, we just need to tell Tremble how to read/write",
				"this data from our map.");

			Text("To do this, we create a new class extending TrembleFieldConverter,",
				"and add the [TrembleFieldConverter] attribute to it.");

			CodeWithHeader("IntInRangeFieldConverter.cs",
				"[TrembleFieldConverter(typeof(IntInRange))]",
				"public class IntInRangeFieldConverter : TrembleFieldConverter<IntInRange>",
				"{",
				"   ...",
				"}");

			Text("Next, you need to implement the AddFieldToFgd method which tells TrenchBroom how to",
				"display your data. This can be as a string, an integer, a list of values, and more.");

			CodeWithHeader("AddFieldToFgd Method",
				$"protected override void AddFieldToFgd({nameof(FgdClass)} entityClass, string fieldName, IntInRange defaultValue, MemberInfo target)",
				"{",
				"    // Add a hint to the end of the description to show the format",
				"    string extraDescription = \" (format: \\\"2-4\\\")\";",
				"",
				"    // Get the [Tooltip] attribute for this field, if it exists",
				"    target.GetCustomAttributes(out TooltipAttribute existingTooltip);",
				"    ",
				"    // Add a \"string\" field to the FGD class (it's not a float or boolean, for example)",
				$"    entityClass.AddField(new {nameof(FgdStringField)}",
				"    {",
				"        Name = fieldName,",
				"        DefaultValue = defaultValue.ToString(),",
				"        Description = (existingTooltip?.tooltip ?? $\"IntInRange {target.Name}\") + extraDescription",
				"    });",
				"}");

			Text("Finally, you need to implement the TryGetValueFromMap method which reads your custom data",
				"from an entity in map. This is used when a map is parsed during import. You can read any data",
				"from the 'entity' passed in, in any format. This example reads a string value, but you can",
				"also read integers, floats, and others.");

			CodeWithHeader("TryGetValueFromMap Method",
				"protected override bool TryGetValueFromMap(BspEntity entity, string key, MapProcessorBase mapProcessor, MemberInfo target, out IntInRange value)",
				"{",
				"    // Try to read string value for our field",
				"    if (!entity.TryGetString(key, out string rangeIntValue))",
				"    {",
				"        value = default;",
				"        return false; // we could not convert the data",
				"    }",
				"",
				"    // Now parse our range from the string value in the map.",
				"    // !! NOTE !!: This sample code performs no bounds checking and is",
				"                   thus very unsafe. See IntInRangeFieldConverter.cs",
				"                   in the Tremble Sample for a much better implementation!",
				"",
				"    value.Min = int.Parse(rangeIntValue.Split(\"-\")[0]);",
				"    value.Max = int.Parse(rangeIntValue.Split(\"-\")[1]);",
				"    return true; // we succesfully converted the data",
				"",
				"}");

			Text("And that's it! Now Tremble will be able to tell TrenchBroom how to display your custom",
				"type, and it will be able to import it from maps without issue.");

			Text("Feel free to check out the real 'IntInRange.cs' and 'IntInRangeFieldConverter.cs' classes",
				"in the Tremble Sample folder, or look in Tremble's FieldConverters folder to see all the",
				"built-in field converters for things like Vector3s, strings, Materials, and more!");
		}

		private void Test(out MinMax mm)
		{
			mm.Min = 2;
			mm.Max = 2;
		}
	}

	struct MinMax
	{
		public int Min;
		public int Max;
	}
}