using Xuch.Framework;
using Xuch.Framework.Utils;

public class ProcedureSplash : ProcedureBase
{
    public override void OnEnter(Fsm<ProcedureManager> fsm)
    {
        base.OnEnter(fsm);

        Log.Debug("Enter ProcedureSplash.");

        ChangeState<ProcedureEnterGame>(fsm);
    }
}