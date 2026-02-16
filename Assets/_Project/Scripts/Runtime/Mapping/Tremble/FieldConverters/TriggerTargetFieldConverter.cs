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
			if (entity.TryGetString(key, out string allIds))
			{
				List<ITriggerTarget> targets = new();
				string[] ids = allIds.Split(',');

				foreach (string id in ids)
				{
					if (TryGetValuesFromId(id, gameObject, target, out List<ITriggerTarget> ts))
					{
						targets.AddRange(ts);
					}
				}
				
				values = new ITriggerTarget[targets.Count];
				for (int objIdx = 0; objIdx < targets.Count; objIdx++)
				{
					ITriggerTarget t = targets[objIdx];
					if (t == null)
					{
						continue;
					}

					values[objIdx] = t;
				}

				return true;
			}

			values = default;
			return false;
		}

		private bool TryGetValuesFromId(string id, GameObject gameObject, MemberInfo target,
			out List<ITriggerTarget> values)
		{
			if (TrembleMapImportSettings.Current.TryGetGameObjectsForID(id, out List<GameObject> objs))
			{
				values = new();
				for (int objIdx = 0; objIdx < objs.Count; objIdx++)
				{
					if (!objs[objIdx].TryGetComponent(out ITriggerTarget t))
					{
						Debug.LogWarning($"Entity '{objs[objIdx].name}' reference '{id}' is of unexpected type. (expected: {target.GetFieldOrPropertyTypeOrElementType().Name}). Check the targeted entity in the map is of the correct type.");

						continue;
					}

					values.Add(t);
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