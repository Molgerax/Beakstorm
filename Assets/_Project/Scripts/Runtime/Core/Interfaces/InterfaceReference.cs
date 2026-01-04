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
    }

    [Serializable]
    public class InterfaceReference<TInterface> : InterfaceReference<TInterface, Object> where TInterface : class { }


    public static class InterfaceReferenceExtensions
    {
        public static TInterface AsInterface<TInterface>(this InterfaceReference<TInterface> i) where TInterface : class => i.Value;

        public static TInterface AsInterface<TInterface>(this Object obj) where TInterface : class
        {
            if (!obj)
                return null;
        
            if (obj is TInterface i)
                return i;
            return null;
        }
        
        public static bool TryAsInterface<TInterface>(this Object obj, out TInterface @interface) where TInterface : class
        {
            @interface = null;
            if (!obj)
                return false;

            if (obj is not TInterface i) 
                return false;
            
            @interface = i;
            return true;
        }
    }
}