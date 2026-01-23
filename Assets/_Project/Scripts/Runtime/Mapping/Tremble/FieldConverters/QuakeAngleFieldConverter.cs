using System.Linq;
using System.Reflection;
using Beakstorm.Mapping.Tremble.Properties;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble.FieldConverters
{
    [TrembleFieldConverter(typeof(QuakeAngle))]
    public class QuakeAngleFieldConverter : TrembleFieldConverter<QuakeAngle>
    {
        protected override bool TryGetValueFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out QuakeAngle value)
        {
            bool success = entity.TryGetInt(key, out int i);
            value = i;
            return success;
        }

        protected override bool TryGetValuesFromMap(BspEntity entity, string key, GameObject gameObject, MemberInfo target, out QuakeAngle[] values)
        {
            if (!entity.TryGetString(key, out string stringValues))
            {
                values = default;
                return false;
            }

            values = stringValues
                .Split(',')
                .Select(s => int.TryParse(s, out int result) ? (QuakeAngle)result : default)
                .ToArray();
            return true;
        }

        protected override void AddFieldToFgd(FgdClass entityClass, string fieldName, QuakeAngle defaultValue, MemberInfo target)
        {
            target.GetCustomAttributes(
                out TooltipAttribute tooltip
            );

            string rangeHint = $" ( -1 = Up, -2 = Down)";

            entityClass.AddField(new FgdIntegerField
            {
                Name = fieldName,
                Description = (tooltip?.tooltip ?? $"{target.GetFieldOrPropertyType().Name} {target.Name}") + rangeHint,
                DefaultValue = defaultValue
            });
        }
    }
}