using XuchFramework.Core.Utils;

namespace XuchFramework.Core
{
    public abstract class ProcedureBase : StateBase<ProcedureManager>
    {
        public sealed override void OnInit(Fsm<ProcedureManager> fsm)
        {
            base.OnInit(fsm);
            OnProcedureInit();
        }

        public sealed override void OnEnter(Fsm<ProcedureManager> fsm)
        {
            base.OnEnter(fsm);
            Log.Debug($"[Procedure] Enter {GetType().Name}...");
            OnProcedureEnter();
        }

        public sealed override void OnExit(Fsm<ProcedureManager> fsm)
        {
            base.OnExit(fsm);
            Log.Debug($"[Procedure] Exit {GetType().Name}...");
            OnProcedureExit();
        }

        public sealed override void OnUpdate(Fsm<ProcedureManager> fsm, float deltaTime, float unscaledDeltaTime)
        {
            base.OnUpdate(fsm, deltaTime, unscaledDeltaTime);
            OnProcedureUpdate(deltaTime, unscaledDeltaTime);
        }

        public virtual void OnProcedureInit() { }

        public virtual void OnProcedureEnter() { }

        public virtual void OnProcedureExit() { }

        public virtual void OnProcedureUpdate(float deltaTime, float unscaledDeltaTime) { }
    }
}