using Beakstorm.Core.Variables;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace Beakstorm.VFX.PropertyBinders
{
    [VFXBinder("Beakstorm/ScriptableVariable")]
// The class need to extend VFXBinderBase
    public class ScriptableVariablePropertyBinder : VFXBinderBase
    {
        // VFXPropertyBinding attributes enables the use of a specific
        // property drawer that populates the VisualEffect properties of a
        // certain type.
        [VFXPropertyBinding("System.Single")]
        public ExposedProperty property;

        public FloatVariable variable;

        // The IsValid method need to perform the checks and return if the binding
        // can be achieved.
        public override bool IsValid(VisualEffect component)
        {
            return variable != null && component.HasFloat(property);
        }

        // The UpdateBinding method is the place where you perform the binding,
        // by assuming that it is valid. This method will be called only if
        // IsValid returned true.
        public override void UpdateBinding(VisualEffect component)
        {
            component.SetFloat(property, variable.GetValue);
        }
    }
}
