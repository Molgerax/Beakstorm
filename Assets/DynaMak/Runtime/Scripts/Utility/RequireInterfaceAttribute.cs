using UnityEngine;

namespace DynaMak.Utility
{
    public class RequireInterfaceAttribute : PropertyAttribute
    {
        public System.Type requiredType { get; private set; }

        public RequireInterfaceAttribute(System.Type type)
        {
            this.requiredType = type;
        }
    }
}
