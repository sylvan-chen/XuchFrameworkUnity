using System;
using System.Collections.Generic;

namespace Framework.Core
{
    public sealed class Fsm
    {
        private const float STATE_SECONDS_LIMIT = float.MaxValue - 5f;

        public string Name { get; private set; }
        public FsmBlackboard Blackboard { get; private set; }

        public int StateCount => _stateDict.Count;
        public StateBase CurrentState { get; private set; }
        public float CurrentStateSeconds { get; private set; } = 0f;

        public bool IsShutdown { get; private set; } = false;
        private bool IsStartup => CurrentState != null;

        private readonly Dictionary<Type, StateBase> _stateDict = new();

        internal Fsm(string name, FsmBlackboard blackboard, params StateBase[] states)
        {
            Name = name;
            Blackboard = blackboard;

            foreach (var state in states)
            {
                if (_stateDict.ContainsKey(state.GetType()))
                {
                    Log.Warning($"[FSM ({Name})] Duplicated state type {state.GetType().FullName}");
                    continue;
                }

                _stateDict.Add(state.GetType(), state);
                state.Init(this);
            }

            IsShutdown = false;
        }

        internal void Update(float deltaTime, float unscaleDeltaTime)
        {
            if (!IsStartup || IsShutdown)
                return;

            CurrentStateSeconds = CurrentStateSeconds >= STATE_SECONDS_LIMIT ? STATE_SECONDS_LIMIT : CurrentStateSeconds + unscaleDeltaTime;
            CurrentState.Update(this, deltaTime, unscaleDeltaTime);
        }

        public void Startup<TState>() where TState : StateBase
        {
            if (IsShutdown)
            {
                Log.Error($"[FSM ({Name})] Startup failed. Fsm has already been destroyed");
                return;
            }

            if (IsStartup)
            {
                Log.Warning($"[FSM ({Name})] Start StateMachine {Name} has already been started, don't start it again");
                return;
            }

            if (_stateDict.TryGetValue(typeof(TState), out StateBase state))
            {
                CurrentState = state;
                CurrentStateSeconds = 0;
                CurrentState.Enter(this);
            }
            else
            {
                Log.Error($"[FSM ({Name})] Start StateMachine {Name} failed. State of type {typeof(TState).FullName} not found");
            }
        }

        public void Startup(Type startStateType)
        {
            if (IsShutdown)
            {
                Log.Error($"[FSM ({Name})] Start StateMachine {Name} failed. It has already been destroyed");
                return;
            }

            if (IsStartup)
            {
                Log.Warning($"[FSM ({Name})] Start StateMachine {Name} has already been started, don't start it again");
                return;
            }

            if (!CheckTypeCompliance(startStateType))
                return;

            if (_stateDict.TryGetValue(startStateType, out var state))
            {
                CurrentState = state;
                CurrentStateSeconds = 0;
                CurrentState.Enter(this);
            }
            else
            {
                Log.Error($"[FSM ({Name})] Start StateMachine {Name} failed. State of type {startStateType.FullName} not found");
            }
        }

        public void Shutdown()
        {
            CurrentState?.Exit(this);
            _stateDict.Clear();
            Blackboard.Clear();

            Name = null;
            Blackboard = null;
            CurrentState = null;
            CurrentStateSeconds = 0f;

            IsShutdown = true;
        }

        public TState GetState<TState>() where TState : StateBase
        {
            if (_stateDict.TryGetValue(typeof(TState), out var state))
            {
                return state as TState;
            }

            return null;
        }

        public StateBase GetState(Type stateType)
        {
            if (!CheckTypeCompliance(stateType))
                return null;

            return _stateDict.GetValueOrDefault(stateType);
        }

        public bool HasState<TState>() where TState : StateBase
        {
            return _stateDict.ContainsKey(typeof(TState));
        }

        public bool HasState(Type stateType)
        {
            if (!CheckTypeCompliance(stateType))
                return false;

            return _stateDict.ContainsKey(stateType);
        }

        public void ChangeState<TState>() where TState : StateBase
        {
            if (IsShutdown)
            {
                Log.Error($"[FSM ({Name})] Change state to {typeof(TState).Name} failed, fsm already destroyed");
                return;
            }

            if (!IsStartup)
            {
                Log.Warning($"[FSM ({Name})] Change state to {typeof(TState).Name} failed, fsm not started up");
                return;
            }

            if (_stateDict.TryGetValue(typeof(TState), out StateBase state))
            {
                CurrentState.Exit(this);
                CurrentState = state;
                CurrentStateSeconds = 0;
                CurrentState.Enter(this);
            }
            else
            {
                Log.Error($"[FSM ({Name})] Change state to {typeof(TState).Name} failed, state not found");
            }
        }

        public void ChangeState(Type stateType)
        {
            if (IsShutdown)
            {
                Log.Error($"[FSM ({Name})] Change state to {stateType.Name} failed, fsm already destroyed");
                return;
            }

            if (!IsStartup)
            {
                Log.Warning($"[FSM ({Name})] Change state to {stateType.Name} failed, fsm not started up");
                return;
            }

            CheckTypeCompliance(stateType);

            if (_stateDict.TryGetValue(stateType, out StateBase state))
            {
                CurrentState.Exit(this);
                CurrentState = state;
                CurrentStateSeconds = 0;
                CurrentState.Enter(this);
            }
            else
            {
                Log.Error($"[FSM ({Name})] Change state to {stateType.Name} failed. State not found");
            }
        }

        public StateBase[] GetAllStates()
        {
            if (IsShutdown)
                return null;
            if (_stateDict.Count == 0)
                return null;

            var result = new StateBase[_stateDict.Count];
            _stateDict.Values.CopyTo(result, 0);
            return result;
        }

        private bool CheckTypeCompliance(Type type)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (type == null)
            {
                Log.Error($"[FSM ({Name})] Check type compliance failed for FSM {Name}, state type cannot be null");
                return false;
            }

            if (!type.IsClass || type.IsAbstract)
            {
                Log.Error($"[FSM ({Name})] Compliance check failed for FSM {Name}, state type {type.FullName} is not a non-abstract class");
                return false;
            }

            if (!typeof(StateBase).IsAssignableFrom(type))
            {
                Log.Error(
                    $"[FSM ({Name})] Compliance check failed for FSM {Name}, state type {type.FullName} is not a subclass of {nameof(StateBase)}");
                return false;
            }
#endif
            return true;
        }
    }
}