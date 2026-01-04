using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Beakstorm.Core.Interfaces
{
    [Serializable]
    public class InterfaceReference<TInterface, TObject> where TObject : Object where TInterface : class
    {
        [SerializeField, HideInInspector] private TObject underlyingValue;

        public TInterface Value
        {
            get
            {
                if (underlyingValue is TInterface @interface)
                    return @interface;
                if (!underlyingValue)
                    return null;
                throw new InvalidOperationException(
                    $"{underlyingValue} needs to implement interface {nameof(TInterface)}");
            }
            set
            {
                if (value is TObject newObject)
                    underlyingValue = newObject;
                else if (value == null)
                    underlyingValue = null;
                else
                    throw new ArgumentException($"{value} needs to be of type {typeof(TObject)}.", String.Empty);
            }
        }

        public TObject UnderlyingValue
        {
            get => underlyingValue;
            set => underlyingValue = value;
        }

        public InterfaceReference() { }

        public InterfaceReference(TObject target) => underlyingValue = target;

        public InterfaceReference(TInterface @interface) => underlyingValue = @interface as TObject;
        
        public static implicit operator TInterface(InterfaceReference<TInterface, TObject> i) => i.Value;
    }

    [Serializable]
    public class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> where TInterface : class
    {
        public static implicit operator TInterface(InterfaceReference<TInterface> i) => i.Value;
    }
}