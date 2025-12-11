using System;
using UnityEngine;
using Xuch.Framework.Utils;

namespace Xuch.Framework
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Xuch/Procedure Manager")]
    public sealed class ProcedureManager : ManagerBase
    {
        [SerializeField]
        private string _startupProcedureTypeName;

        [SerializeField]
        private string[] _availableProcedureTypeNames;

        private Fsm<ProcedureManager> _procedureFsm;
        private ProcedureBase _startupProcedure;

        public ProcedureBase CurrentProcedure => _procedureFsm?.CurrentState as ProcedureBase;
        public float CurrentProcedureTime => _procedureFsm?.CurrentStateTime ?? 0;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            var procedures = new ProcedureBase[_availableProcedureTypeNames.Length];
            // 注册所有流程为状态
            for (int i = 0; i < _availableProcedureTypeNames.Length; i++)
            {
                string typeName = _availableProcedureTypeNames[i];
                procedures[i] = Activator.CreateInstance(TypeHelper.GetType(typeName)) as ProcedureBase;
                if (typeName == _startupProcedureTypeName)
                {
                    _startupProcedure = procedures[i];
                }
            }

            if (_startupProcedure == null)
            {
                Log.Error(
                    $"[ProcedureManager] Initialize failed. Startup procedure '{_startupProcedureTypeName}' not found or failed to initialize.");
                return;
            }

            _procedureFsm = App.FsmManager.CreateFsm(this, procedures);
        }

        protected override void OnStartup()
        {
            base.OnStartup();

            StartProcedure();
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            App.FsmManager.ShutdownFsm<ProcedureManager>();
            _procedureFsm = null;
            _startupProcedure = null;
        }

        public void StartProcedure()
        {
            _procedureFsm.Startup(_startupProcedure.GetType());
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