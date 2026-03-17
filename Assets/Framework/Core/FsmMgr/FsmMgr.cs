using System.Collections.Generic;
using UnityEngine;

namespace Framework.Core
{
    public static class FsmMgr
    {
        private static readonly Dictionary<string, Fsm> _fsms = new();

        private const string DEFAULT_FSM_NAME = "Default";

        public static void Dispose()
        {
            foreach (var fsm in _fsms.Values)
            {
                fsm.Shutdown();
            }

            _fsms.Clear();
        }

        public static void Update()
        {
            foreach (var stateMachine in _fsms.Values)
            {
                stateMachine.Update(Time.deltaTime, Time.unscaledDeltaTime);
            }
        }

        public static Fsm CreateFsm(List<StateBase> states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, new FsmBlackboard(), states.ToArray());
        }

        public static Fsm CreateFsm(params StateBase[] states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, new FsmBlackboard(), states);
        }

        public static Fsm CreateFsm(FsmBlackboard blackboard, List<StateBase> states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, blackboard, states.ToArray());
        }

        public static Fsm CreateFsm(FsmBlackboard blackboard, params StateBase[] states)
        {
            return CreateFsm(DEFAULT_FSM_NAME, blackboard, states);
        }

        public static Fsm CreateFsm(string fsmName, List<StateBase> states)
        {
            return CreateFsm(fsmName, new FsmBlackboard(), states.ToArray());
        }

        public static Fsm CreateFsm(string fsmName, params StateBase[] states)
        {
            return CreateFsm(fsmName, new FsmBlackboard(), states);
        }

        public static Fsm CreateFsm(string fsmName, FsmBlackboard blackboard, List<StateBase> states)
        {
            return CreateFsm(fsmName, blackboard, states.ToArray());
        }

        public static Fsm CreateFsm(string fsmName, FsmBlackboard blackboard, params StateBase[] states)
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

        public static Fsm GetFsm(string fsmName = DEFAULT_FSM_NAME)
        {
            return _fsms.GetValueOrDefault(fsmName);
        }

        public static void ShutdownFsm(string fsmName = DEFAULT_FSM_NAME)
        {
            if (_fsms.TryGetValue(fsmName, out var fsm))
            {
                fsm.Shutdown();
                _fsms.Remove(fsmName);
            }
        }
    }
}