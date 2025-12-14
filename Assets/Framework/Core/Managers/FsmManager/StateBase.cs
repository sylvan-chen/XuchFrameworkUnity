namespace XuchFramework.Core
{
    public abstract class StateBase
    {
        internal void Init(Fsm fsm)
        {
            if (fsm == null)
            {
                Log.Error($"[{GetType().Name}] Init failed. FSM is null");
                return;
            }

            OnInit(fsm);
        }

        internal void Enter(Fsm fsm)
        {
            if (fsm == null)
            {
                Log.Error($"[{GetType().Name}] Enter failed. FSM is null");
                return;
            }

            OnEnter(fsm);
        }

        internal void Exit(Fsm fsm)
        {
            if (fsm == null)
            {
                Log.Error($"[{GetType().Name}] Exit failed. FSM is null");
                return;
            }

            OnExit(fsm);
        }

        internal void Update(Fsm fsm, float deltaTime, float unscaledDeltaTime)
        {
            if (fsm == null)
            {
                Log.Error($"[{GetType().Name}] Update failed. FSM is null");
                return;
            }

            OnUpdate(fsm, deltaTime, unscaledDeltaTime);
        }

        protected virtual void OnInit(Fsm fsm) { }

        protected virtual void OnEnter(Fsm fsm) { }

        protected virtual void OnExit(Fsm fsm) { }

        protected virtual void OnUpdate(Fsm fsm, float deltaTime, float unscaledDeltaTime) { }
    }
}