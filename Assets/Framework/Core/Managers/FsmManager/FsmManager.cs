using System.Collections.Generic;
using UnityEngine;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Fsm Manager")]
    public sealed class FsmManager : ManagerBase
    {
        private readonly Dictionary<string, Fsm> _fsms = new();

        private const string DEFAULT_FSM_NAME = "Default";

        protected override void OnDispose()
        {
            foreach (var fsm in _fsms.Values)
            {
                fsm.Shutdown();
            }

            _fsms.Clear();
        }

        protected override void OnUpdate(float deltaTime, float unscaledDeltaTime)
        {
            foreach (var stateMachine in _fsms.Values)
            {
                stateMachine.Update(deltaTime, unscaledDeltaTime);
            }
        }

        public Fsm CreateFsm(List<StateBase> states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, new Blackboard(), states.ToArray());
        }

        public Fsm CreateFsm(params StateBase[] states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, new Blackboard(), states);
        }

        public Fsm CreateFsm(Blackboard blackboard, List<StateBase> states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, blackboard, states.ToArray());
        }

        public Fsm CreateFsm(Blackboard blackboard, params StateBase[] states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, blackboard, states);
        }

        public Fsm CreateFsm(string fsmName, List<StateBase> states)
        {
            return CreateFsm(fsmName, new Blackboard(), states.ToArray());
        }

        public Fsm CreateFsm(string fsmName, params StateBase[] states)
        {
            return CreateFsm(fsmName, new Blackboard(), states);
        }

        public Fsm CreateFsm(string fsmName, Blackboard blackboard, List<StateBase> states)
        {
            return CreateFsm(fsmName, blackboard, states.ToArray());
        }

        public Fsm CreateFsm(string fsmName, Blackboard blackboard, params StateBase[] states)
        {
            if (_fsms.ContainsKey(fsmName))
            {
                Log.Error($"[FsmManager] Create FSM failed. FSM '{fsmName}' already exists");
                return null;
            }

            var fsm = new Fsm(fsmName, blackboard, states);
            _fsms.Add(fsmName, fsm);
            return fsm;
        }

        public Fsm GetFsm(string fsmName = DEFAULT_FSM_NAME)
        {
            return _fsms.GetValueOrDefault(fsmName);
        }

        public void ShutdownFsm(string fsmName = DEFAULT_FSM_NAME)
        {
            if (_fsms.TryGetValue(fsmName, out var fsm))
            {
                fsm.Shutdown();
                _fsms.Remove(fsmName);
            }
        }
    }
}