using UnityEngine;

namespace Beakstorm.Core.Events
{
    public interface IEventListener<T>
    {
        void OnEventRaised(T data);
    }
}