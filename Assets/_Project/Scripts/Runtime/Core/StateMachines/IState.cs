using UnityEngine;

namespace Beakstorm.Core.StateMachines
{
    public interface IState<T>
    {
        public void OnEnter(T entity);
        public void OnExit(T entity);

        public void OnUpdate(T entity, float deltaTime);
        public void OnFixedUpdate(T entity, float deltaTime);
    }
}
