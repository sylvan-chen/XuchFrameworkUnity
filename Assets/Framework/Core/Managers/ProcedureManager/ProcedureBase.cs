namespace XuchFramework.Core
{
    public abstract class ProcedureBase : StateBase
    {
        protected sealed override void OnInit(Fsm fsm)
        {
            OnProcedureInit();
        }

        protected sealed override void OnEnter(Fsm fsm)
        {
            Log.Debug($"[GameProcedure] Enter {GetType().Name}...");
            OnProcedureEnter();
        }

        protected sealed override void OnExit(Fsm fsm)
        {
            Log.Debug($"[GameProcedure] Exit {GetType().Name}...");
            OnProcedureExit();
        }

        protected sealed override void OnUpdate(Fsm fsm, float deltaTime, float unscaledDeltaTime)
        {
            OnProcedureUpdate(deltaTime, unscaledDeltaTime);
        }

        protected virtual void OnProcedureInit() { }

        protected virtual void OnProcedureEnter() { }

        protected virtual void OnProcedureExit() { }

        protected virtual void OnProcedureUpdate(float deltaTime, float unscaledDeltaTime) { }
    }
}