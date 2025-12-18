namespace Beakstorm.Core.StateMachines
{
    public abstract class BaseState<T> : IState<T>
    {
        public virtual void OnEnter(T entity)
        {
            // noop
        }

        public virtual void OnExit(T entity)
        {
            // noop
        }

        public virtual void OnUpdate(T entity, float deltaTime)
        {
            // noop
        }

        public virtual void OnFixedUpdate(T entity, float deltaTime)
        {
            // noop
        }
    }
}