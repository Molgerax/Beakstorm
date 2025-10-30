using System.Reflection;
using UnityEngine;

namespace TinyGoose.Tremble.Sample.Editor
{
	[TrembleFieldConverter(typeof(IntInRange))]
	public class IntInRangeFieldConverter : TrembleFieldConverter<IntInRange>
	{
		// Tells Tremble how to get an IntInRange value from a map.
		// In most cases, you just read from `entity` using the `key` provided.
		// 
		// For more advanced cases you might read other keys from the entity for context, or look at the sync settings.
		// - `entity` is the entity that the value is coming from
		// - `key` is the name of the entity key to read the value from
		// - `target` is the field this value is being set to (you can read the name, what class, etc.)
		//
		// - you should write the resulting value to `value`
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out IntInRange value)
		{
			// Try to read string value for our field
			if (!entity.TryGetString(key, out string rangeIntValue))
			{
				value = default;
				return false;
			}

			// Pass to IntInRange parser
			return IntInRange.TryParse(rangeIntValue, out value);
		}

		// Tells Tremble how to expose an IntInRange field to your map editor, in the FGD file.
		// Basically you must simply use entityClass.AddField() with the type of field you wish to add
		// to the FGD (e.g., string, int, etc). Use FgdStringField if there is not a more appropriate type.
		//
		// Params:
		// - entityClass is the FGD class we are adding this field to
		// - fieldName is the name of the field - pass this as-is to an FgdFieldXXX constructor unless you have good reason not to!
		// - defaultValue is the default value of the field in C# that you are dealing with
		// - target is the C# field that you are reading from - you can check for Attributes etc here.
		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, IntInRange defaultValue, MemberInfo target)
		{
			// Add a hint to the end of the description to show the format
			string extraDescription = " (format: \"2-4\")";

			// Get the [Tooltip] attribute for this field, if it exists
			target.GetCustomAttributes(out TooltipAttribute existingTooltip);
			
			// Add a "string" field to the FGD class (it's not a float or boolean, for example)
			entityClass.AddField(new FgdStringField
			{
				Name = fieldName,
				DefaultValue = defaultValue.ToString(),
				Description = (existingTooltip?.tooltip ?? $"IntInRange {target.Name}") + extraDescription
			});
		}
	}
}