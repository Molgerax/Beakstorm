using UnityEditor;
using UnityEngine;

namespace Beakstorm.Core.Attributes
{
    [CustomPropertyDrawer(typeof(PowerOfTwoAttribute))]
    public class PowerOfTwoDrawer : PropertyDrawer
    {
        public int[] Options;
        public GUIContent[] DisplayOptions;

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            PowerOfTwoAttribute power = attribute as PowerOfTwoAttribute;
            
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, label.text, "Use PowerOfTwo with int.");
                return;
            }

            PopulateOptions(power);
            ValidateValue(power, property);
            EditorGUI.IntPopup(position, property, DisplayOptions, Options);
        }

        private void ValidateValue(PowerOfTwoAttribute power, SerializedProperty property)
        {
            int value = property.intValue;
            int logged = Mathf.RoundToInt(Mathf.Log(value, 2));

            logged = Mathf.Clamp(logged, power.MinExp, power.MaxExp);

            property.intValue = 1 << logged;
        }
        
        private void PopulateOptions(PowerOfTwoAttribute power)
        {
            if (Options != null)
            {
                if (Options[0] == power.Min && Options[^1] == power.Max)
                    return;
            }
            
            int count = power.MaxExp - power.MinExp + 1;
            Options = new int[count];
            DisplayOptions = new GUIContent[count];
            int index = 0;
            
            for (int i = power.MinExp; i <= power.MaxExp; i++)
            {
                int value = 1 << i;
                Options[index] = value;
                DisplayOptions[index] = new GUIContent($"{value}");
                index++;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }
    }
}