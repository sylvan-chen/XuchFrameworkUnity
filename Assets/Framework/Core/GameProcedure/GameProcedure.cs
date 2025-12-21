using System;
using UnityEngine;
using XuchFramework.Core.Utils;

namespace XuchFramework.Core
{
    [DisallowMultipleComponent]
    [AddComponentMenu("XuchFramework/Game Procedure")]
    public sealed class GameProcedure : MonoSingletonPersistent<GameProcedure>
    {
        private const string PROCEDURE_FSM_NAME = "GameProcedure";

        [SerializeField]
        private string _startupProcedureTypeName;

        [SerializeField]
        private string[] _availableProcedureTypeNames;

        private Fsm _procedureFsm;
        private ProcedureBase _startupProcedure;

        public bool IsStarted { get; private set; } = false;
        public Blackboard Blackboard { get; private set; }
        public ProcedureBase CurrentProcedure => _procedureFsm?.CurrentState as ProcedureBase;
        public float CurrentProcedureSeconds => _procedureFsm?.CurrentStateSeconds ?? 0;

        public void Startup()
        {
            if (IsStarted)
            {
                Log.Error("[GameProcedure] Startup failed. GameProcedure has already started");
                return;
            }

            var procedures = new ProcedureBase[_availableProcedureTypeNames.Length];
            // Register all available procedures
            for (int i = 0; i < _availableProcedureTypeNames.Length; i++)
            {
                string typeName = _availableProcedureTypeNames[i];
                procedures[i] = Activator.CreateInstance(GameHelper.GetType(typeName)) as ProcedureBase;
                if (typeName == _startupProcedureTypeName)
                {
                    _startupProcedure = procedures[i];
                }
            }

            if (_startupProcedure == null)
            {
                Log.Error($"[GameProcedure] Initialize failed. Startup procedure '{_startupProcedureTypeName}' not found or failed to initialize");
                return;
            }

            Blackboard = new Blackboard();
            _procedureFsm = GameModule<FsmManager>.Instance.CreateFsm(PROCEDURE_FSM_NAME, Blackboard, procedures);

            _procedureFsm.Startup(_startupProcedure.GetType());
            IsStarted = true;
        }

        protected override void OnDispose()
        {
            GameModule<FsmManager>.Instance.ShutdownFsm(PROCEDURE_FSM_NAME);
            Blackboard.Clear();

            IsStarted = false;
            _procedureFsm = null;
            _startupProcedure = null;
            Blackboard = null;
        }

        public void ChangeProcedure<T>() where T : ProcedureBase
        {
            _procedureFsm.ChangeState<T>();
        }

        public T GetProcedure<T>() where T : ProcedureBase
        {
            return _procedureFsm.GetState<T>();
        }

        public bool HasProcedure<T>() where T : ProcedureBase
        {
            return _procedureFsm.HasState<T>();
        }
    }
}