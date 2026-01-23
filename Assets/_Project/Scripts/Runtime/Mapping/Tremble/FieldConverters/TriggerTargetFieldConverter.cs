using System.Collections.Generic;
using System.Reflection;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble.FieldConverters
{
    [TrembleFieldConverter(typeof(ITriggerTarget))]
	public class TriggerTargetFieldConverter : TrembleFieldConverter<ITriggerTarget>
	{
		protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out ITriggerTarget value)
		{
			if (entity.TryGetString(key, out string id))
			{
				if (TrembleMapImportSettings.Current.TryGetGameObjectsForID(id, out List<GameObject> objs) && objs.Count > 0)
				{
					value = null;
					
					if (!objs[0].TryGetComponent(out value))
					{
						Debug.LogWarning($"Entity '{objs[0].name}' reference '{id}' is of unexpected type. (expected: {typeof(ITriggerTarget)}). Check the targeted entity in the map is of the correct type.");
						return false;
					}

					return true;
				}
			}

			value = null;
			return false;
		}

		protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out ITriggerTarget[] values)
		{
			if (entity.TryGetString(key, out string id) && TrembleMapImportSettings.Current.TryGetGameObjectsForID(id, out List<GameObject> objs))
			{
				values = new ITriggerTarget[objs.Count];
				for (int objIdx = 0; objIdx < objs.Count; objIdx++)
				{
					if (!objs[objIdx].TryGetComponent(out ITriggerTarget t))
					{
						Debug.LogWarning($"Entity '{objs[objIdx].name}' reference '{id}' is of unexpected type. (expected: {typeof(ITriggerTarget)}). Check the targeted entity in the map is of the correct type.");
						continue;
					}

					values[objIdx] = t;
				}

				return true;
			}

			values = default;
			return false;
		}

		protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, ITriggerTarget defaultValue, MemberInfo target)
		{
			target.GetCustomAttributes(
				out TooltipAttribute tooltip
			);

			entityClass.AddField(new FgdTargetDestinationField
			{
				Name = fieldName,
				Description = tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}",
				DefaultValue = default
			});
		}
	}
}