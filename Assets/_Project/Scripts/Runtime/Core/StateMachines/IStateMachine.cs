namespace Beakstorm.Core.StateMachines
{
    public interface IStateMachine<T>
    {
        public IState<T> CurrentState { get; }
    }
    
    public class StateMachine<T> : IStateMachine<T>
    {
        protected T _owner;
        protected IState<T> _currentState;

        public IState<T> CurrentState => _currentState;

        public StateMachine(T owner)
        {
            _owner = owner;
        }
        
        public void SetState(IState<T> state)
        {
            _currentState = state;
            _currentState?.OnEnter(_owner);
        }

        public void ChangeState(IState<T> state)
        {
            if (state == _currentState) 
                return;

            var previousState = _currentState;
            
            previousState.OnExit(_owner);
            state.OnEnter(_owner);
            
            _currentState = state;
        }

        public void Update(float deltaTime)
        {
            _currentState?.OnUpdate(_owner, deltaTime);
        }
        
        public void FixedUpdate(float deltaTime)
        {
            _currentState?.OnFixedUpdate(_owner, deltaTime);
        }
    }
}