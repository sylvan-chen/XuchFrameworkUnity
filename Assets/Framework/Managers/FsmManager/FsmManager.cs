using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xuch.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("DigiEden/Fsm Manager")]
    public sealed class FsmManager : ManagerBase
    {
        private readonly Dictionary<int, FsmBase> _fsms = new();

        private const string DEFAULT_FSM_NAME = "default";

        protected override void OnDispose()
        {
            base.OnDispose();
            foreach (var fsm in _fsms.Values)
            {
                fsm.Shutdown();
            }

            _fsms.Clear();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            foreach (var stateMachine in _fsms.Values)
            {
                stateMachine.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        public Fsm<T> CreateFsm<T>(T owner, List<StateBase<T>> states) where T : class
        {
            return CreateFsm(DEFAULT_FSM_NAME, owner, states.ToArray());
        }

        public Fsm<T> CreateFsm<T>(T owner, params StateBase<T>[] states) where T : class
        {
            return CreateFsm(DEFAULT_FSM_NAME, owner, states);
        }

        public Fsm<T> CreateFsm<T>(string fsmName, T owner, List<StateBase<T>> states) where T : class
        {
            return CreateFsm(fsmName, owner, states.ToArray());
        }

        public Fsm<T> CreateFsm<T>(string fsmName, T owner, params StateBase<T>[] states) where T : class
        {
            if (fsmName == null)
            {
                throw new ArgumentNullException(nameof(fsmName), "Create StateMachine failed. Name cannot be null.");
            }

            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner), "Create StateMachine failed. Owner cannot be null.");
            }

            if (states == null || states.Length == 0)
            {
                throw new ArgumentNullException(nameof(states), "Create StateMachine failed. Initial states cannot be null or empty.");
            }

            int id = GetID(typeof(T), fsmName);
            if (_fsms.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    $"Create StateMachine failed. StateMachine with the same name ({fsmName}) and same owner type ({typeof(T).Name}) already exists.");
            }

            var stateMachine = Fsm<T>.Create(fsmName, owner, states);
            _fsms.Add(id, stateMachine);
            return stateMachine;
        }

        public Fsm<T> GetFsm<T>() where T : class
        {
            return GetFsm<T>(DEFAULT_FSM_NAME);
        }

        public Fsm<T> GetFsm<T>(string fsmName) where T : class
        {
            if (fsmName == null)
            {
                throw new ArgumentNullException(nameof(fsmName), "Get StateMachine failed. Name cannot be null.");
            }

            int id = GetID(typeof(T), fsmName);
            if (_fsms.TryGetValue(id, out var stateMachine))
            {
                return stateMachine as Fsm<T>;
            }

            return null;
        }

        public void ShutdownFsm<T>() where T : class
        {
            ShutdownFsm<T>(DEFAULT_FSM_NAME);
        }

        public void ShutdownFsm<T>(string fsmName) where T : class
        {
            if (fsmName == null)
            {
                throw new ArgumentNullException(nameof(fsmName), "Destroy StateMachine failed. Name cannot be null.");
            }

            int id = GetID(typeof(T), fsmName);
            if (_fsms.TryGetValue(id, out var stateMachine))
            {
                stateMachine.Shutdown();
                _fsms.Remove(id);
            }
        }

        private int GetID(Type type, string fsmName)
        {
            return (type.Name + fsmName).GetHashCode();
        }
    }
}