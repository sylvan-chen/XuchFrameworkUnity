using System;
using System.Collections.Generic;
using XuchFramework.Core.Utils;

namespace XuchFramework.Core
{
    public abstract class FsmBase
    {
        internal abstract void Update(float deltaTime, float unscaleDeltaTime);
        internal abstract void Shutdown();
    }

    /// <summary>
    /// 状态机
    /// </summary>
    /// <typeparam name="T">状态机的所有者类型</typeparam>
    public sealed class Fsm<T> : FsmBase, ICache where T : class
    {
        /// <summary>
        /// 状态机名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 状态机所有者
        /// </summary>
        public T Owner { get; private set; }

        /// <summary>
        /// 状态机的状态数量
        /// </summary>
        public int StateCount => _stateDict.Count;

        /// <summary>
        /// 当前状态
        /// </summary>
        public StateBase<T> CurrentState { get; private set; }

        /// <summary>
        /// 当前状态已持续时间
        /// </summary>
        /// <remarks>
        /// 单位：秒，切换时重置为 0。
        /// </remarks>
        public float CurrentStateTime { get; private set; } = 0f;

        /// <summary>
        /// 状态机是否已销毁
        /// </summary>
        public bool IsDestroyed { get; private set; } = false;

        /// <summary>
        /// 状态机是否已启动
        /// </summary>
        private bool IsStartup => CurrentState != null;

        private readonly Dictionary<Type, StateBase<T>> _stateDict = new();

        internal static Fsm<T> Create(string name, T owner, params StateBase<T>[] states)
        {
            var fsm = CachePool.Spawn<Fsm<T>>();
            fsm.Name = name;
            fsm.Owner = owner;
            foreach (var state in states)
            {
                if (fsm._stateDict.ContainsKey(state.GetType()))
                {
                    Log.Warning($"[StateMachine<{typeof(T).Name}>] Duplicated state type {state.GetType().FullName}.");
                    continue;
                }

                fsm._stateDict.Add(state.GetType(), state);
                state.OnInit(fsm);
            }

            fsm.IsDestroyed = false;
            return fsm;
        }

        internal override void Update(float deltaTime, float unscaleDeltaTime)
        {
            if (!IsStartup || IsDestroyed)
                return;

            CurrentStateTime += unscaleDeltaTime;
            CurrentState.OnUpdate(this, deltaTime, unscaleDeltaTime);
        }

        internal override void Shutdown()
        {
            CurrentState?.OnExit(this);
            foreach (var state in _stateDict.Values)
            {
                state.OnShutdown(this);
            }

            IsDestroyed = true;
            Clear();
            CachePool.Unspawn(this);
        }

        private void Clear()
        {
            _stateDict.Clear();
            Name = null;
            Owner = null;
            CurrentState = null;
            CurrentStateTime = 0f;
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <typeparam name="TState">启动时的状态类型</typeparam>
        public void Startup<TState>() where TState : StateBase<T>
        {
            if (IsDestroyed)
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Start StateMachine {Name} failed. It has already been destroyed.");
                return;
            }

            if (IsStartup)
            {
                Log.Warning($"[StateMachine<{typeof(T).Name}>] Start StateMachine {Name} has already been started, don't start it again.");
                return;
            }

            if (_stateDict.TryGetValue(typeof(TState), out StateBase<T> state))
            {
                CurrentState = state;
                CurrentStateTime = 0;
                CurrentState.OnEnter(this);
            }
            else
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Start StateMachine {Name} failed. State of type {typeof(TState).FullName} not found.");
            }
        }

        /// <summary>
        /// 启动状态机
        /// </summary>
        /// <param name="startStateType">启动时的状态类型</param>
        public void Startup(Type startStateType)
        {
            if (IsDestroyed)
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Start StateMachine {Name} failed. It has already been destroyed.");
                return;
            }

            if (IsStartup)
            {
                Log.Warning($"[StateMachine<{typeof(T).Name}>] Start StateMachine {Name} has already been started, don't start it again.");
                return;
            }

            if (!CheckTypeCompliance(startStateType))
                return;

            if (_stateDict.TryGetValue(startStateType, out var state))
            {
                CurrentState = state;
                CurrentStateTime = 0;
                CurrentState.OnEnter(this);
            }
            else
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Start StateMachine {Name} failed. State of type {startStateType.FullName} not found.");
            }
        }

        public TState GetState<TState>() where TState : StateBase<T>
        {
            if (_stateDict.TryGetValue(typeof(TState), out var state))
            {
                return state as TState;
            }

            return null;
        }

        public StateBase<T> GetState(Type stateType)
        {
            if (!CheckTypeCompliance(stateType))
                return null;

            return _stateDict.GetValueOrDefault(stateType);
        }

        public bool HasState<TState>() where TState : StateBase<T>
        {
            return _stateDict.ContainsKey(typeof(TState));
        }

        public bool HasState(Type stateType)
        {
            if (!CheckTypeCompliance(stateType))
                return false;

            return _stateDict.ContainsKey(stateType);
        }

        public void ChangeState<TState>() where TState : StateBase<T>
        {
            if (IsDestroyed)
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Change state to {typeof(TState).Name} failed with FSM {Name}, fsm already destroyed.");
                return;
            }

            if (!IsStartup)
            {
                Log.Warning($"[StateMachine<{typeof(T).Name}>] Change state to {typeof(TState).Name} failed with FSM {Name}, fsm not started up.");
                return;
            }

            if (_stateDict.TryGetValue(typeof(TState), out StateBase<T> state))
            {
                CurrentState.OnExit(this);
                CurrentState = state;
                CurrentStateTime = 0;
                CurrentState.OnEnter(this);
            }
            else
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Change state to {typeof(TState).Name} failed on FSM {Name}. State not found.");
            }
        }

        public void ChangeState(Type stateType)
        {
            if (IsDestroyed)
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Change state to {stateType.Name} failed with FSM {Name}, fsm already destroyed.");
                return;
            }

            if (!IsStartup)
            {
                Log.Warning($"[StateMachine<{typeof(T).Name}>] Change state to {stateType.Name} failed with FSM {Name}, fsm not started up.");
                return;
            }

            CheckTypeCompliance(stateType);

            if (_stateDict.TryGetValue(stateType, out StateBase<T> state))
            {
                CurrentState.OnExit(this);
                CurrentState = state;
                CurrentStateTime = 0;
                CurrentState.OnEnter(this);
            }
            else
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Change state to {stateType.Name} failed on FSM {Name}. State not found.");
            }
        }

        public StateBase<T>[] GetAllStates()
        {
            if (IsDestroyed)
                return null;
            if (_stateDict.Count == 0)
                return null;

            var result = new StateBase<T>[_stateDict.Count];
            _stateDict.Values.CopyTo(result, 0);
            return result;
        }

        private bool CheckTypeCompliance(Type type)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (type == null)
            {
                Log.Error($"[StateMachine<{typeof(T).Name}>] Check type complience of StateMachine {Name} failed. State type cannot be null.");
                return false;
            }

            if (!type.IsClass || type.IsAbstract)
            {
                Log.Error(
                    $"[StateMachine<{typeof(T).Name}>] Compliance check failed for StateMachine {Name}, state type {type.FullName} is not a non-abstract class.");
                return false;
            }

            if (!typeof(StateBase<T>).IsAssignableFrom(type))
            {
                Log.Error(
                    $"[StateMachine<{typeof(T).Name}>] Compliance check failed for StateMachine {Name}, state type {type.FullName} is not a subclass of {typeof(StateBase<T>).Name}.");
                return false;
            }
#endif
            return true;
        }
    }
}